using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tycoon.OperatorDashboard.Services;

namespace Tycoon.OperatorDashboard.Pages;

public sealed class LogoutModel(AdminAuthService auth) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await auth.LogoutAsync();
        return RedirectToPage("/Login");
    }
}
