using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Mvc.Controllers;

// Cambia la empresa con la que trabaja el usuario (selector de la barra superior)
[Authorize]
public class EmpresaActualController : Controller
{
    private readonly ContextoUsuarioHttp _contexto;

    public EmpresaActualController(ContextoUsuarioHttp contexto)
    {
        _contexto = contexto;
    }

    // POST: EmpresaActual/Seleccionar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Seleccionar(int empresaId, string? returnUrl)
    {
        if (!_contexto.EmpresasPermitidas.Contains(empresaId))
            return Forbid();

        Response.Cookies.Append(ContextoUsuarioHttp.CookieEmpresa, empresaId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            MaxAge = TimeSpan.FromDays(180)
        });
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
    }
}
