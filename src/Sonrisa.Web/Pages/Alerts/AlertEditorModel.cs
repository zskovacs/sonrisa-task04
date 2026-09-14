using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sonrisa.Web.Alerts;

namespace Sonrisa.Web.Pages.Alerts;

public abstract class AlertEditorModel : PageModel
{
    [BindProperty]
    public AlertInput Input { get; set; } = new();

    protected void AddErrors(IReadOnlyList<AlertFieldError> errors)
    {
        foreach (var error in errors)
            ModelState.AddModelError(error.Key == "Settings" ? string.Empty : $"Input.{error.Key}", error.Message);
    }

    protected bool HasBindingErrors()
    {
        if (ModelState.IsValid) return false;

        if (ModelState.TryGetValue("Input.Enabled", out var enabled) && enabled.Errors.Count > 0)
        {
            ModelState.Remove("Input.Enabled");
            ModelState.AddModelError("Input.Enabled", "Select whether the alert is enabled.");
        }

        if (ModelState.TryGetValue("Input.Revision", out var revision) && revision.Errors.Count > 0)
        {
            ModelState.Remove("Input.Revision");
            ModelState.AddModelError(string.Empty, "Reload this alert before saving.");
        }

        return true;
    }
}
