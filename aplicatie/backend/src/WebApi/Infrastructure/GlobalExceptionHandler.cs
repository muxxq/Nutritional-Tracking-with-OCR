using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NutriScan.Application.Common.Exceptions;

namespace NutriScan.WebApi.Infrastructure;

/// <summary>
/// Translates exceptions into RFC 7807 ProblemDetails so every module returns errors in one format.
/// Unexpected exceptions are logged and returned without details.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException validation => new HttpValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Date invalide"
            },
            ConflictException => new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = exception.Message },
            UnauthorizedException => new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = exception.Message },
            _ => new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "A apărut o eroare neașteptată" }
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
