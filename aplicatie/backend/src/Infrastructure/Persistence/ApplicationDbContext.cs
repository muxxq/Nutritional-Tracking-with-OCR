using Microsoft.EntityFrameworkCore;
using NutriScan.Application.Common.Interfaces;
using NutriScan.Domain.Common;
using NutriScan.Domain.Entities;
using NutriScan.Infrastructure.Common;

namespace NutriScan.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IDateTimeProvider _clock;

    /// <param name="clock">Source of audit timestamps; the system clock when omitted (design-time tooling).</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDateTimeProvider? clock = null) : base(options)
    {
        _clock = clock ?? new SystemDateTimeProvider();
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<FoodItem> FoodItems => Set<FoodItem>();
    public DbSet<DiaryEntry> DiaryEntries => Set<DiaryEntry>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<PlannedMeal> PlannedMeals => Set<PlannedMeal>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            // Optimistic concurrency: parallel failed logins must not overwrite each other's increment.
            entity.Property(u => u.FailedLoginAttempts).HasDefaultValue(0).IsConcurrencyToken();
            entity.Ignore(u => u.IsProfileComplete);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(rt => rt.TokenHash).IsUnique();
            entity.HasIndex(rt => rt.FamilyId);

            // Optimistic concurrency: rotating or revoking only succeeds if no one revoked the token first.
            entity.Property(rt => rt.RevokedAtUtc).IsConcurrencyToken();

            entity.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FoodItem
        modelBuilder.Entity<FoodItem>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).HasMaxLength(250).IsRequired();
            entity.Property(f => f.Brand).HasMaxLength(150);
            entity.Property(f => f.Barcode).HasMaxLength(64);
            entity.HasIndex(f => f.Barcode);
            // name (≤ 250) + " " + brand (≤ 150)
            entity.Property(f => f.SearchText).HasMaxLength(401).IsRequired();
            entity.HasIndex(f => f.SearchText);

            // Not SET NULL: a null creator means "global", so deleting a user must never publish their private foods.
            // Account deletion has to archive/anonymise or delete the user's foods explicitly.
            entity.HasOne(f => f.CreatedByUser)
                .WithMany()
                .HasForeignKey(f => f.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DiaryEntry
        modelBuilder.Entity<DiaryEntry>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => new { d.UserId, d.ConsumedDate });

            entity.HasOne(d => d.User)
                .WithMany(u => u.DiaryEntries)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.FoodItem)
                .WithMany(f => f.DiaryEntries)
                .HasForeignKey(d => d.FoodItemId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Recipes are archived, never deleted; RESTRICT only guards against an accidental hard delete.
            entity.HasOne(d => d.Recipe)
                .WithMany()
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.Restrict);

            // A food or a recipe, never both or neither; recipes are logged in servings (ADR-006 D2, D3).
            // Enforced here too, so a write that bypasses the API cannot store an incoherent entry.
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_DiaryEntries_FoodXorRecipe", "(\"FoodItemId\" IS NULL) <> (\"RecipeId\" IS NULL)");
                t.HasCheckConstraint("CK_DiaryEntries_QuantityByKind",
                    "(\"FoodItemId\" IS NOT NULL AND \"QuantityGrams\" IS NOT NULL) " +
                    "OR (\"RecipeId\" IS NOT NULL AND \"QuantityGrams\" IS NULL AND \"Servings\" IS NOT NULL)");
            });
        });

        // Recipe & RecipeIngredient
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(1000);
            entity.Property(r => r.SearchText).HasMaxLength(200).IsRequired();
            entity.HasIndex(r => r.SearchText);

            entity.HasOne(r => r.User)
                .WithMany(u => u.Recipes)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.HasKey(ri => ri.Id);
            // A food appears once per recipe (ADR-006 D6).
            entity.HasIndex(ri => new { ri.RecipeId, ri.FoodItemId }).IsUnique();

            entity.HasOne(ri => ri.Recipe)
                .WithMany(r => r.Ingredients)
                .HasForeignKey(ri => ri.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ri => ri.FoodItem)
                .WithMany(f => f.RecipeIngredients)
                .HasForeignKey(ri => ri.FoodItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PlannedMeal
        modelBuilder.Entity<PlannedMeal>(entity =>
        {
            entity.HasKey(pm => pm.Id);
            entity.HasIndex(pm => new { pm.UserId, pm.PlanDate });

            entity.HasOne(pm => pm.User)
                .WithMany(u => u.PlannedMeals)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foods and recipes are archived, never deleted; SET NULL would break the "exactly one" rule below.
            entity.HasOne(pm => pm.FoodItem)
                .WithMany(f => f.PlannedMeals)
                .HasForeignKey(pm => pm.FoodItemId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(pm => pm.Recipe)
                .WithMany(r => r.PlannedMeals)
                .HasForeignKey(pm => pm.RecipeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Deleting the diary entry frees the planned meal to be logged again (ADR-007 D6).
            entity.HasOne(pm => pm.DiaryEntry)
                .WithMany()
                .HasForeignKey(pm => pm.DiaryEntryId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(pm => pm.DiaryEntryId).IsUnique();

            // Same shape as a diary entry: a food or a recipe, recipes in servings only (ADR-007 D1).
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_PlannedMeals_FoodXorRecipe", "(\"FoodItemId\" IS NULL) <> (\"RecipeId\" IS NULL)");
                t.HasCheckConstraint("CK_PlannedMeals_QuantityByKind",
                    "(\"FoodItemId\" IS NOT NULL AND \"QuantityGrams\" IS NOT NULL) " +
                    "OR (\"RecipeId\" IS NOT NULL AND \"QuantityGrams\" IS NULL AND \"Servings\" IS NOT NULL)");
            });
        });

        // ShoppingListItem
        modelBuilder.Entity<ShoppingListItem>(entity =>
        {
            entity.HasKey(sli => sli.Id);
            entity.Property(sli => sli.ItemName).HasMaxLength(200).IsRequired();

            entity.HasOne(sli => sli.User)
                .WithMany(u => u.ShoppingListItems)
                .HasForeignKey(sli => sli.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = _clock.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = _clock.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
