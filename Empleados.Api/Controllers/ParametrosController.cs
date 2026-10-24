using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Api.Controllers;

// Consulta de parámetros legales (las nuevas vigencias se registran desde el MVC)
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ParametrosController : ControllerBase
{
    private readonly ParametrosLegalesService _parametros;

    public ParametrosController(ParametrosLegalesService parametros)
    {
        _parametros = parametros;
    }

    // GET: api/Parametros?fecha=2026-03-01
    [HttpGet]
    public async Task<IActionResult> GetVigentes([FromQuery] DateTime? fecha)
    {
        var parametros = await _parametros.VigentesAsync(fecha ?? DateTime.Today);
        return Ok(parametros.Select(p => new
        {
            p.Clave,
            Descripcion = ClavesParametro.Descripciones.GetValueOrDefault(p.Clave),
            p.Valor,
            p.VigenteDesde,
            p.Fuente
        }));
    }

    // GET: api/Parametros/isr?fecha=2026-03-01
    [HttpGet("isr")]
    public async Task<IActionResult> GetEscalaIsr([FromQuery] DateTime? fecha)
    {
        var tramos = await _parametros.EscalaIsrAsync(fecha ?? DateTime.Today);
        return Ok(tramos.Select(t => new { t.VigenteDesde, t.Desde, t.Hasta, t.Tasa, t.CuotaFija }));
    }

    // GET: api/Parametros/salarios-minimos
    [HttpGet("salarios-minimos")]
    public async Task<IActionResult> GetSalariosMinimos()
    {
        var salarios = await _parametros.SalariosMinimosAsync();
        return Ok(salarios.Select(s => new { s.Tamano, s.Monto, s.VigenteDesde }));
    }

    // GET: api/Parametros/feriados/2026
    [HttpGet("feriados/{anio:int}")]
    public async Task<IActionResult> GetFeriados(int anio)
    {
        var feriados = await _parametros.FeriadosAsync(anio);
        return Ok(feriados.Select(f => new { f.Fecha, f.Nombre }));
    }
}
