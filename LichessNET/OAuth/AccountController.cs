using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class AccountController : Controller
{
    [HttpGet("~/login")]
    public IActionResult Login()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/" };
        return Challenge(properties, "Lichess");
    }

    [HttpGet("~/logout")]
    public IActionResult Logout()
    {
        return SignOut(new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet("~/signin-lichess")]
    public async Task<IActionResult> Callback()
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
            return BadRequest(); // Handle error

        var token = authenticateResult.Properties.GetTokenValue("access_token");

        // Use the token as needed
        // For example, you can store it in the session or pass it to a view
        ViewBag.Token = token;

        return RedirectToAction("Index", "Home");
    }
}