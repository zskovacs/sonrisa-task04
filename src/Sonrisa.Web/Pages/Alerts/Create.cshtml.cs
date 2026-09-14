using Microsoft.AspNetCore.Mvc;
using Sonrisa.Web.Alerts;

namespace Sonrisa.Web.Pages.Alerts;

public sealed class CreateModel(AlertManagementService alerts) : AlertEditorModel
{
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (HasBindingErrors()) return Page();
        var result = await alerts.CreateAsync(Input, cancellationToken);
        if (result.Status == AlertStatus.Success) return RedirectToPage("Index");
        if (result.Status == AlertStatus.Invalid) { AddErrors(result.Errors!); return Page(); }
        if (result.Status == AlertStatus.Unavailable)
        {
            ModelState.AddModelError(string.Empty, "Alert configuration is temporarily unavailable. Please try again later.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return Page();
        }
        return StatusCode(500);
    }
}
