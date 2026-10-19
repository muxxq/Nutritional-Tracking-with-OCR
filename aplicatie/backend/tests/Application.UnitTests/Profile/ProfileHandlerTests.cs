using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Exceptions;
using NutriScan.Application.Profile.Commands.UpdateProfile;
using NutriScan.Application.Profile.Commands.UpdateTargets;
using NutriScan.Application.Profile.Queries.GetProfile;
using NutriScan.Application.UnitTests.Auth;
using NutriScan.Domain.Enums;

namespace NutriScan.Application.UnitTests.Profile;

public class ProfileHandlerTests : IDisposable
{
    private readonly AuthTestContext _ctx = new();

    private Task<NutriScan.Application.Profile.Dtos.ProfileDto> GetProfile(Guid userId) =>
        new GetProfileQueryHandler(_ctx.Db, _ctx.Clock.Object).Handle(new GetProfileQuery(userId), CancellationToken.None);

    private Task<NutriScan.Application.Profile.Dtos.ProfileDto> UpdateProfile(UpdateProfileCommand command) =>
        new UpdateProfileCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(command, CancellationToken.None);

    private Task<NutriScan.Application.Profile.Dtos.ProfileDto> UpdateTargets(UpdateTargetsCommand command) =>
        new UpdateTargetsCommandHandler(_ctx.Db, _ctx.Clock.Object).Handle(command, CancellationToken.None);

    [Fact]
    public async Task GetProfile_NewUser_ReturnsDefaults()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();

        // Act
        var profile = await GetProfile(user.Id);

        // Assert
        profile.IsProfileComplete.Should().BeFalse();
        profile.Bmr.Should().BeNull();
        profile.Targets.Calories.Value.Should().Be(2000);
        profile.Targets.Calories.Source.Should().Be(TargetSource.Default);
        profile.Email.Should().Be("ana@exemplu.ro");
    }

    [Fact]
    public async Task GetProfile_OverridesInfeasibleAYearLater_DoesNotThrow()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        await UpdateProfile(ProfileTestData.R1(user.Id));
        await UpdateTargets(new UpdateTargetsCommand(user.Id, null, 318, null, null));
        _ctx.Now = _ctx.Now.AddYears(1);

        // Act
        var profile = await GetProfile(user.Id);

        // Assert
        profile.Targets.ProteinsGrams.Source.Should().Be(TargetSource.Calculated);
    }

    [Fact]
    public async Task GetProfile_MissingUser_ThrowsUnauthorized()
    {
        // Act
        var act = () => GetProfile(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task UpdateProfile_R1_SavesAndReturnsCalculatedTargets()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();

        // Act
        var profile = await UpdateProfile(ProfileTestData.R1(user.Id) with { FirstName = "  Ana " });

        // Assert
        profile.Age.Should().Be(28);
        profile.Bmr.Should().Be(1369);
        profile.Targets.Calories.Value.Should().Be(1700);
        profile.IsProfileComplete.Should().BeTrue();
        await using var verify = _ctx.CreateContext();
        var stored = await verify.Users.SingleAsync();
        stored.FirstName.Should().Be("Ana");
        stored.BirthDate.Should().Be(new DateOnly(1998, 5, 14));
    }

    [Fact]
    public async Task UpdateProfile_UserWithManualProteins_KeepsOverride()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        await UpdateProfile(ProfileTestData.R1(user.Id));
        await UpdateTargets(new UpdateTargetsCommand(user.Id, null, 140, null, null));

        // Act
        var profile = await UpdateProfile(ProfileTestData.R1(user.Id) with { WeightKg = 70 });

        // Assert
        profile.Targets.ProteinsGrams.Should().Be(new NutriScan.Application.Profile.Dtos.TargetDto(140, TargetSource.Manual));
        profile.WeightKg.Should().Be(70);
    }

    [Fact]
    public async Task UpdateProfile_OverridesBecomeImpossible_ResetsToCalculated()
    {
        // Arrange: 300 g manual carbs fit R1 (1700 kcal) but not a sedentary R1 (1310 kcal)
        var user = await _ctx.SeedUserAsync();
        await UpdateProfile(ProfileTestData.R1(user.Id));
        await UpdateTargets(new UpdateTargetsCommand(user.Id, null, null, 300, null));

        // Act
        var profile = await UpdateProfile(ProfileTestData.R1(user.Id) with { ActivityLevel = ActivityLevel.Sedentary });

        // Assert
        profile.Targets.CarbsGrams.Source.Should().Be(TargetSource.Calculated);
        profile.Targets.Calories.Value.Should().Be(1310);
        await using var verify = _ctx.CreateContext();
        (await verify.Users.SingleAsync()).CarbsGramsOverride.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProfile_MissingUser_ThrowsUnauthorized()
    {
        // Act
        var act = () => UpdateProfile(ProfileTestData.R1(Guid.NewGuid()));

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task UpdateTargets_ManualProteins_RecalculatesCarbs()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        await UpdateProfile(ProfileTestData.R1(user.Id));

        // Act
        var profile = await UpdateTargets(new UpdateTargetsCommand(user.Id, null, 140, null, null));

        // Assert
        profile.Targets.CarbsGrams.Value.Should().Be(179);
        profile.Targets.ProteinsGrams.Source.Should().Be(TargetSource.Manual);
    }

    [Fact]
    public async Task UpdateTargets_Null_RestoresCalculated()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        await UpdateProfile(ProfileTestData.R1(user.Id));
        await UpdateTargets(new UpdateTargetsCommand(user.Id, null, 140, null, null));

        // Act
        var profile = await UpdateTargets(new UpdateTargetsCommand(user.Id, null, null, null, null));

        // Assert
        profile.Targets.ProteinsGrams.Should().Be(new NutriScan.Application.Profile.Dtos.TargetDto(124, TargetSource.Calculated));
    }

    [Fact]
    public async Task UpdateTargets_Impossible_ThrowsValidationAndDoesNotSave()
    {
        // Arrange
        var user = await _ctx.SeedUserAsync();
        await UpdateProfile(ProfileTestData.R1(user.Id));

        // Act
        var act = () => UpdateTargets(new UpdateTargetsCommand(user.Id, 1000, 300, null, null));

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        await using var verify = _ctx.CreateContext();
        (await verify.Users.SingleAsync()).ProteinsGramsOverride.Should().BeNull();
    }

    public void Dispose() => _ctx.Dispose();
}
