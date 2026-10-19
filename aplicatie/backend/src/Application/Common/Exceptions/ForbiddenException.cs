namespace NutriScan.Application.Common.Exceptions;

/// <summary>The resource is visible to the user, but the user may not change it (HTTP 403).</summary>
public sealed class ForbiddenException(string message) : Exception(message);
