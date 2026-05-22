using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Synaptix.OperatorDashboard.Services;

namespace Synaptix.OperatorDashboard.Pages;

public sealed class LogoutModel(AdminAuthService auth) : PageModel
{
    // GET: renders the auto-submitting POST form — never executes the logout directly.
    public IActionResult OnGet() => Page();

    // POST (with antiforgery): performs the actual sign-out.
    public async Task<IActionResult> OnPostAsync()
    {
        await auth.LogoutAsync();
        return RedirectToPage("/Login");
    }
}
