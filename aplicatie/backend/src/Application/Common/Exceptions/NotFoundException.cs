namespace NutriScan.Application.Common.Exceptions;

/// <summary>The resource does not exist or is not visible to the user (HTTP 404, existence not revealed).</summary>
public sealed class NotFoundException(string message) : Exception(message);
