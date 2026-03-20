using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tycoon.OperatorDashboard.Services;

namespace Tycoon.OperatorDashboard.Pages;

public sealed class LoginModel(AdminAuthService auth) : PageModel
{
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    public string Error { get; private set; } = string.Empty;

    public IActionResult OnGet() =>
        User.Identity?.IsAuthenticated == true ? LocalRedirect("/") : Page();

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        if (!ModelState.IsValid) return Page();

        var ok = await auth.LoginAsync(Email, Password);
        if (!ok)
        {
            Error = "Invalid credentials.";
            return Page();
        }

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
