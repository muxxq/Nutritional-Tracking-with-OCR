using MediatR;
using NutriScan.Application.Scans.Dtos;

namespace NutriScan.Application.Scans.Commands.ScanLabel;

/// <summary>Reads a label photo into a prefilled food form; nothing is saved until the user confirms (FR-05).</summary>
public sealed record ScanLabelCommand(Guid UserId, byte[] Image) : IRequest<ScanResultDto>;
