using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Api.Controllers;

// Prestaciones laborales: Admin y RRHH calculan; solo el Admin marca el pago
[Authorize(Roles = "Admin,RRHH")]
[ApiController]
[Route("api/[controller]")]
public class LiquidacionesController : ControllerBase
{
    private readonly LiquidacionService _liquidaciones;
    private readonly EmpleadosDbContext _context;

    public LiquidacionesController(LiquidacionService liquidaciones, EmpleadosDbContext context)
    {
        _liquidaciones = liquidaciones;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLiquidaciones() =>
        Ok((await _liquidaciones.ListarAsync()).Select(l => new
        {
            l.Id, l.EmpleadoId, Empleado = $"{l.Empleado?.Nombre} {l.Empleado?.Apellido}", l.Causa, l.FechaSalida, l.TiempoServicio, l.Neto, l.Estado
        }));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLiquidacion(int id)
    {
        var l = await _liquidaciones.ObtenerAsync(id);
        if (l == null)
            return NotFound(new { mensaje = "Liquidación no encontrada" });
        return Ok(new
        {
            l.Id, l.EmpleadoId, Empleado = $"{l.Empleado?.Nombre} {l.Empleado?.Apellido}", l.Causa, l.FechaIngreso, l.FechaSalida,
            l.PreavisoOtorgado, l.SalarioPromedio, l.SalarioDiario, l.TiempoServicio, l.MontoAcordado,
            l.TotalIngresos, l.TotalDeducciones, l.Neto, l.Estado, l.Comentario, l.CreadaPor, l.FechaCreacion, l.PagadaPor, l.FechaPago,
            Lineas = LiquidacionService.Lineas(l.LineasJson),
            Explicacion = CicloNominaService.Lista(l.ExplicacionJson),
            Advertencias = CicloNominaService.Lista(l.AdvertenciasJson)
        });
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetPdf(int id)
    {
        var l = await _liquidaciones.ObtenerAsync(id);
        if (l == null)
            return NotFound(new { mensaje = "Liquidación no encontrada" });
        var empresa = await _context.Empresas.FirstAsync(e => e.Id == l.EmpresaId);
        return File(LiquidacionPdf.Generar(empresa, l), "application/pdf", $"liquidacion-{l.Id}.pdf");
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] LiquidacionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var resultado = await _liquidaciones.CrearAsync(dto);
        return resultado.Ok ? Ok(new { resultado.Valor!.Id, resultado.Valor.Neto, mensaje = resultado.Mensaje }) : this.Error(resultado);
    }

    // POST: api/Liquidaciones/5/pagar?fechaPago=2026-10-30
    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/pagar")]
    public async Task<IActionResult> Pagar(int id, [FromQuery] DateTime? fechaPago) =>
        Respuesta(await _liquidaciones.PagarAsync(id, fechaPago ?? DateTime.Today));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id) => Respuesta(await _liquidaciones.EliminarAsync(id));

    private IActionResult Respuesta(Resultado resultado) =>
        resultado.Ok ? Ok(new { mensaje = resultado.Mensaje }) : this.Error(resultado);
}
