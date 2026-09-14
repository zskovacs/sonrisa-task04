using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sonrisa.Web.Alerts;
using Sonrisa.Web.Users;

namespace Sonrisa.Web.Pages.Settings;

public sealed class NotificationsModel(UserNotificationSettingsService settings) : PageModel
{
    [BindProperty]
    public UserNotificationSettingsInput Input { get; set; } = new();
    public bool Saved { get; private set; }
    public string? Message { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Saved = TempData["NotificationSettingsSaved"] is true;
        var result = await settings.GetAsync(cancellationToken);
        if (result.Status == AlertStatus.Success)
        {
            Input = new UserNotificationSettingsInput
            {
                EmailDestination = result.Value!.EmailDestination,
                SlackDestination = result.Value.SlackDestination,
                Revision = result.Value.Revision
            };
        }
        else if (result.Status == AlertStatus.Unavailable)
        {
            Message = "Notification settings are temporarily unavailable. Please try again later.";
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            if (ModelState.TryGetValue("Input.Revision", out var revision) && revision.Errors.Count > 0)
            {
                ModelState.Remove("Input.Revision");
                ModelState.AddModelError(string.Empty, "Reload notification settings before saving.");
            }
            return Page();
        }
        var result = await settings.SaveAsync(Input, cancellationToken);
        if (result.Status == AlertStatus.Success)
        {
            TempData["NotificationSettingsSaved"] = true;
            return RedirectToPage();
        }
        if (result.Status == AlertStatus.Invalid)
        {
            foreach (var error in result.Errors!)
                ModelState.AddModelError(string.IsNullOrEmpty(error.Key) ? string.Empty : $"Input.{error.Key}", error.Message);
            return Page();
        }
        if (result.Status == AlertStatus.Conflict)
        {
            ModelState.AddModelError(string.Empty, "These notification settings changed. Reload before saving.");
            return Page();
        }
        Message = "Notification settings are temporarily unavailable. Please try again later.";
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return Page();
    }
}
