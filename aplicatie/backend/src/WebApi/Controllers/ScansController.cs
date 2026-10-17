using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutriScan.Application.Scans;
using NutriScan.Application.Scans.Commands.ScanLabel;
using NutriScan.Application.Scans.Dtos;
using NutriScan.WebApi.Auth;
using NutriScan.WebApi.Infrastructure;

namespace NutriScan.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/scans")]
public class ScansController(ISender sender) : ControllerBase
{
    // Room for the multipart envelope around a 10 MB file; anything larger is cut off by Kestrel before it is buffered.
    private const long RequestLimit = ScanLabelCommandValidator.MaxBytes + 64 * 1024;

    /// <summary>
    /// Reads a nutrition label photo (JPEG, PNG or WebP, at most 10 MB) into a prefilled food form, with the fields
    /// to confirm marked. The photo is processed in memory and not stored. Nothing is saved: the user confirms the
    /// values and creates the food through POST /api/foods.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    [PayloadTooLarge(ScanErrors.TooLarge)]
    [ProducesResponseType<ScanResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Scan(IFormFile? file, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError("file", ScanErrors.Empty);
            return ValidationProblem(ModelState);
        }

        if (file.Length > ScanLabelCommandValidator.MaxBytes)
        {
            return Problem(title: ScanErrors.TooLarge, statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);

        return Ok(await sender.Send(new ScanLabelCommand(userId, buffer.ToArray()), cancellationToken));
    }
}
