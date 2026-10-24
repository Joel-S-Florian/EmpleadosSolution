using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Api.Controllers;

// Salario de Navidad (art. 219) y Bonificación (art. 223) de la empresa actual
[Authorize(Roles = "Admin,RRHH")]
[ApiController]
[Route("api/[controller]")]
public class PagosEspecialesController : ControllerBase
{
    private readonly CicloNominaService _ciclo;
    private readonly BonificacionService _bonificacion;

    public PagosEspecialesController(CicloNominaService ciclo, BonificacionService bonificacion)
    {
        _ciclo = ciclo;
        _bonificacion = bonificacion;
    }

    // POST: api/PagosEspeciales/salario-navidad?anio=2026 - crea y calcula la nómina de Navidad (se sigue con api/Nominas)
    [HttpPost("salario-navidad")]
    public async Task<IActionResult> CrearSalarioNavidad([FromQuery] int anio)
    {
        var resultado = await _ciclo.CrearSalarioNavidadAsync(anio);
        return resultado.Ok ? Ok(new { resultado.Valor!.Id, mensaje = resultado.Mensaje }) : this.Error(resultado);
    }

    // GET: api/PagosEspeciales/bonificacion?anioFiscal=2025&utilidad=1000000
    [HttpGet("bonificacion")]
    public async Task<IActionResult> Bonificacion([FromQuery] int anioFiscal, [FromQuery] decimal utilidad)
    {
        var resultado = await _bonificacion.CalcularAsync(anioFiscal, utilidad);
        if (!resultado.Ok)
            return this.Error(resultado);
        var r = resultado.Valor!;
        return Ok(new
        {
            r.AnioFiscal, r.Inicio, r.Cierre, r.UtilidadNeta, r.ARepartir, r.TotalBonificacion,
            Partes = r.Partes.Select(p => new
            {
                p.EmpleadoId, p.Nombre, p.Frecuencia, p.Parte.FactorTiempo, p.Parte.Proporcional, p.Parte.DiasTope, p.Parte.Tope, p.Parte.Monto, p.Infotep
            })
        });
    }

    // POST: api/PagosEspeciales/bonificacion?anioFiscal=2025&utilidad=1000000&nominaId=7
    [HttpPost("bonificacion")]
    public async Task<IActionResult> AplicarBonificacion([FromQuery] int anioFiscal, [FromQuery] decimal utilidad, [FromQuery] int nominaId)
    {
        var resultado = await _bonificacion.AplicarAsync(anioFiscal, utilidad, nominaId);
        return resultado.Ok ? Ok(new { mensaje = resultado.Mensaje }) : this.Error(resultado);
    }
}
