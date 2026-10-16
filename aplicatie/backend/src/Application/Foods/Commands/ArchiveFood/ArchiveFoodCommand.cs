using MediatR;

namespace NutriScan.Application.Foods.Commands.ArchiveFood;

public sealed record ArchiveFoodCommand(Guid UserId, Guid FoodId) : IRequest;
