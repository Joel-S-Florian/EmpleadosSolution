using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Empleados.Mvc.Areas.Identity.Pages.Account;

// Acceso en español; reemplaza la página de Identity por defecto
[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public LoginModel(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public DatosAcceso Entrada { get; set; } = new();

    public class DatosAcceso
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Escriba un correo válido")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; } = string.Empty;

        public bool Recordarme { get; set; }
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect("~/");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return Page();

        var resultado = await _signInManager.PasswordSignInAsync(Entrada.Correo, Entrada.Contrasena, Entrada.Recordarme, lockoutOnFailure: true);
        if (resultado.Succeeded)
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "~/");
        ModelState.AddModelError(string.Empty, resultado.IsLockedOut
            ? "La cuenta quedó bloqueada por varios intentos fallidos. Intente de nuevo en unos minutos."
            : "Correo o contraseña incorrectos");
        return Page();
    }
}
