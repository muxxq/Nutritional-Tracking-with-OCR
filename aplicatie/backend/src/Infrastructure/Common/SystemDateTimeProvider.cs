using NutriScan.Application.Common.Interfaces;

namespace NutriScan.Infrastructure.Common;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
