using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Sonrisa.Web.Alerts;

namespace Sonrisa.Web.Pages.Alerts;

public sealed class EditModel(AlertManagementService alerts) : AlertEditorModel
{
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await alerts.GetAsync(id, cancellationToken);
        if (result.Status == AlertStatus.NotFound) return NotFound();
        if (result.Status == AlertStatus.Unavailable) return StatusCode(503);
        if (result.Status != AlertStatus.Success) return StatusCode(500);
        var alert = result.Value!;
        Input = new AlertInput { Name = alert.Name, Threshold = alert.Threshold.ToString("R", CultureInfo.InvariantCulture), Enabled = alert.Enabled, Revision = alert.Revision };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (HasBindingErrors()) return Page();
        var result = await alerts.UpdateAsync(id, Input, cancellationToken);
        if (result.Status == AlertStatus.Success) return RedirectToPage("Index");
        if (result.Status == AlertStatus.NotFound) return NotFound();
        if (result.Status == AlertStatus.Conflict) { ModelState.AddModelError(string.Empty, "This alert changed. Reload it before saving."); return Page(); }
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
