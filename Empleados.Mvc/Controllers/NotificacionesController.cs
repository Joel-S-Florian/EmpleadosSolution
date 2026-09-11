using Empleados.Data.Data;
using Empleados.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Empleados.Mvc.Controllers;

[Authorize]
public class NotificacionesController : Controller
{
    private readonly EmpleadosDbContext _context;

    public NotificacionesController(EmpleadosDbContext context)
    {
        _context = context;
    }

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

    // GET: Notificaciones
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var query = _context.Notificaciones.AsQueryable();
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(n => n.UsuarioDestinoId == userId || n.UsuarioDestinoId == null);

        var notificaciones = await query.OrderByDescending(n => n.FechaCreacion).ToListAsync();
        return View(notificaciones);
    }

    // GET: Notificaciones/GetUnreadCount - para polling JS
    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var query = _context.Notificaciones.Where(n => !n.Leida);
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(n => n.UsuarioDestinoId == userId || n.UsuarioDestinoId == null);
        var count = await query.CountAsync();
        return Json(new { count });
    }

    // GET: Notificaciones/GetMisNotificaciones - lista JSON para polling
    [HttpGet]
    public async Task<IActionResult> GetMisNotificaciones()
    {
        var userId = GetCurrentUserId();
        var query = _context.Notificaciones.AsQueryable();
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(n => n.UsuarioDestinoId == userId || n.UsuarioDestinoId == null);
        var list = await query.OrderByDescending(n => n.FechaCreacion).Take(10).ToListAsync();
        return Json(list);
    }

    // POST: Notificaciones/MarcarLeida/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarLeida(int id)
    {
        var notificacion = await _context.Notificaciones.FindAsync(id);
        if (notificacion == null) return NotFound();
        var userId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(notificacion.UsuarioDestinoId) && notificacion.UsuarioDestinoId != userId)
            return Forbid();
        notificacion.Leida = true;
        await _context.SaveChangesAsync();
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true });
        return RedirectToAction(nameof(Index));
    }

    // POST: Notificaciones/MarcarLeidaAjax/5 - para JS sin antiforgery
    [HttpPost]
    public async Task<IActionResult> MarcarLeidaAjax(int id)
    {
        var notificacion = await _context.Notificaciones.FindAsync(id);
        if (notificacion == null) return NotFound(new { mensaje = "No encontrada" });
        var userId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(notificacion.UsuarioDestinoId) && notificacion.UsuarioDestinoId != userId)
            return Forbid();
        notificacion.Leida = true;
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }
}
