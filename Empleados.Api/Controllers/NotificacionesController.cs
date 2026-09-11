using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Empleados.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificacionesController : ControllerBase
{
    private readonly EmpleadosDbContext _context;

    public NotificacionesController(EmpleadosDbContext context)
    {
        _context = context;
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
               ?? User.FindFirstValue(ClaimTypes.Name);
    }

    // GET: api/Notificaciones/mis-notificaciones
    [HttpGet("mis-notificaciones")]
    public async Task<IActionResult> GetMisNotificaciones()
    {
        var userId = GetCurrentUserId();

        var query = _context.Notificaciones.AsQueryable();

        // Si hay UsuarioDestinoId, filtrar por usuario actual o global (null); si no hay, mostrar todas para testing
        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(n => n.UsuarioDestinoId == userId || n.UsuarioDestinoId == null);
        }

        var notificaciones = await query
            .OrderByDescending(n => n.FechaCreacion)
            .Select(n => new NotificacionDto
            {
                Id = n.Id,
                Mensaje = n.Mensaje,
                Tipo = n.Tipo,
                Leida = n.Leida,
                FechaCreacion = n.FechaCreacion,
                UsuarioDestinoId = n.UsuarioDestinoId
            })
            .ToListAsync();

        return Ok(notificaciones);
    }

    // POST: api/Notificaciones/marcar-leida/{id}
    [HttpPost("marcar-leida/{id}")]
    public async Task<IActionResult> MarcarLeida(int id)
    {
        var userId = GetCurrentUserId();
        var notificacion = await _context.Notificaciones.FindAsync(id);
        if (notificacion == null)
            return NotFound(new { mensaje = "Notificación no encontrada" });

        // Validar que la notificación pertenece al usuario (si tiene destino) o es global
        if (!string.IsNullOrEmpty(notificacion.UsuarioDestinoId) && notificacion.UsuarioDestinoId != userId)
            return Forbid();

        notificacion.Leida = true;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Notificación marcada como leída" });
    }

    // GET: api/Notificaciones/no-leidas-count - para badge
    [HttpGet("no-leidas-count")]
    public async Task<IActionResult> GetNoLeidasCount()
    {
        var userId = GetCurrentUserId();
        var query = _context.Notificaciones.Where(n => !n.Leida);
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(n => n.UsuarioDestinoId == userId || n.UsuarioDestinoId == null);

        var count = await query.CountAsync();
        return Ok(new { count });
    }
}
