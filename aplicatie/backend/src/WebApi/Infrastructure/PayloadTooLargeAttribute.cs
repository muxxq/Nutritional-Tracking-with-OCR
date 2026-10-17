using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NutriScan.WebApi.Infrastructure;

/// <summary>
/// Reads the multipart form before model binding, so a body over the form or request limits is answered with 413
/// instead of the generic 400 model binding would produce ("Failed to read the request form").
/// Runs after <see cref="RequestFormLimitsAttribute"/> and <see cref="RequestSizeLimitAttribute"/>, which set the limits.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PayloadTooLargeAttribute(string title) : Attribute, IAsyncResourceFilter, IOrderedFilter
{
    public int Order => 1000;

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (request.HasFormContentType)
        {
            try
            {
                await request.ReadFormAsync(context.HttpContext.RequestAborted);
            }
            catch (Exception e) when (e is InvalidDataException or BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge })
            {
                context.Result = new ObjectResult(new ProblemDetails { Status = StatusCodes.Status413PayloadTooLarge, Title = title })
                {
                    StatusCode = StatusCodes.Status413PayloadTooLarge
                };
                return;
            }
        }

        await next();
    }
}
