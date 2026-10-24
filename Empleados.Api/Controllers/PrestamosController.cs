using Empleados.Data.DTOs;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Api.Controllers;

[Authorize(Roles = "Admin,RRHH")]
[ApiController]
[Route("api/[controller]")]
public class PrestamosController : ControllerBase
{
    private readonly PrestamoService _prestamos;

    public PrestamosController(PrestamoService prestamos)
    {
        _prestamos = prestamos;
    }

    // GET: api/Prestamos?empleadoId=5
    [HttpGet]
    public async Task<IActionResult> GetPrestamos([FromQuery] int? empleadoId)
    {
        var prestamos = await _prestamos.ListarAsync(empleadoId);
        return Ok(prestamos.Select(p => new
        {
            p.Id, p.EmpleadoId, Empleado = $"{p.Empleado?.Nombre} {p.Empleado?.Apellido}", p.Concepto,
            p.MontoTotal, p.MontoCuota, p.Saldo, p.Prioridad, p.FechaInicio, p.SoloFinDeMes, p.Activo
        }));
    }

    // POST: api/Prestamos
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] PrestamoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var resultado = await _prestamos.CrearAsync(dto);
        return resultado.Ok ? Ok(new { resultado.Valor!.Id, mensaje = resultado.Mensaje }) : this.Error(resultado);
    }

    // POST: api/Prestamos/5/cancelar
    [HttpPost("{id}/cancelar")]
    public async Task<IActionResult> Cancelar(int id)
    {
        var resultado = await _prestamos.CancelarAsync(id);
        return resultado.Ok ? Ok(new { mensaje = resultado.Mensaje }) : this.Error(resultado);
    }
}
