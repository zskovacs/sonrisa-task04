using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sonrisa.Web.Alerts;

namespace Sonrisa.Web.Pages.Alerts;

public sealed class IndexModel(AlertManagementService alerts) : PageModel
{
    public IReadOnlyList<AlertDetails> Alerts { get; private set; } = [];
    public string? Message { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var result = await alerts.ListAsync(cancellationToken);
        if (result.Status == AlertStatus.Success) { Alerts = result.Value!; return Page(); }
        Message = "Alert configuration is temporarily unavailable. Please try again later.";
        if (result.Status == AlertStatus.Unavailable)
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return Page();
    }
    public async Task<IActionResult> OnPostStatusAsync(Guid id, Guid revision, bool enabled, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest("Invalid alert status request.");
        var result = await alerts.SetEnabledAsync(id, revision, enabled, cancellationToken);
        return result.Status switch { AlertStatus.Success => RedirectToPage(), AlertStatus.NotFound => NotFound(), AlertStatus.Unavailable => StatusCode(503), AlertStatus.Conflict => StatusCode(409, "This alert changed. Reload the page before changing its status."), _ => BadRequest() };
    }
}
