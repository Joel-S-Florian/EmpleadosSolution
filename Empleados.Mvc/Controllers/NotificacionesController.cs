using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Empleados.Mvc.Controllers;

[Authorize]
public class NotificacionesController : Controller
{
    private readonly NotificacionService _notificaciones;

    public NotificacionesController(NotificacionService notificaciones)
    {
        _notificaciones = notificaciones;
    }

    private string? UsuarioActualId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // GET: Notificaciones
    public async Task<IActionResult> Index()
    {
        return View(await _notificaciones.ListarAsync(UsuarioActualId));
    }

    // GET: Notificaciones/GetUnreadCount - para el badge (polling JS)
    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        return Json(new { count = await _notificaciones.ContarNoLeidasAsync(UsuarioActualId) });
    }

    // GET: Notificaciones/Panel - las últimas, para el panel desplegable de la campana
    [HttpGet]
    public async Task<IActionResult> Panel()
    {
        return PartialView("_Panel", await _notificaciones.ListarAsync(UsuarioActualId, 8));
    }

    // POST: Notificaciones/MarcarTodasLeidas
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarTodasLeidas()
    {
        await _notificaciones.MarcarTodasLeidasAsync(UsuarioActualId);
        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            return Json(new { success = true });
        return RedirectToAction(nameof(Index));
    }

    // POST: Notificaciones/MarcarLeida/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLeida(int id)
    {
        var resultado = await _notificaciones.MarcarLeidaAsync(id, UsuarioActualId);
        if (resultado.Tipo == TipoResultado.NoEncontrado) return NotFound();
        if (resultado.Tipo == TipoResultado.Prohibido) return Forbid();

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            return Json(new { success = true });
        return RedirectToAction(nameof(Index));
    }
}
