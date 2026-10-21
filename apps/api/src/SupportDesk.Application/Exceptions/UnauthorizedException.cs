namespace SupportDesk.Application.Exceptions;

/// <summary>
/// Thrown when authentication fails or the request lacks required credentials.
/// </summary>
public sealed class UnauthorizedException(string message) : Exception(message);
