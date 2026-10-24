using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Empleados.Api.Controllers;

// Archivo de la TSS, pago al banco y planilla de personal de la empresa actual
[Authorize(Roles = "Admin,RRHH")]
[ApiController]
[Route("api/[controller]")]
public class ArchivosController : ControllerBase
{
    private readonly ArchivosService _archivos;

    public ArchivosController(ArchivosService archivos)
    {
        _archivos = archivos;
    }

    // GET: api/Archivos/tss/2026/10 - resumen con observaciones por trabajador
    [HttpGet("tss/{anio}/{mes}")]
    public async Task<IActionResult> ResumenTss(int anio, int mes)
    {
        var resultado = await _archivos.AutodeterminacionAsync(anio, mes);
        if (!resultado.Ok)
            return this.Error(resultado);
        var r = resultado.Valor!;
        return Ok(new
        {
            r.Anio, r.Mes, r.Totales, r.Avisos, r.NombreArchivo,
            Filas = r.Filas.Select(f => new { f.EmpleadoId, f.Nombre, f.Trabajador, f.Afp, f.Sfs, f.Isr, f.Avisos })
        });
    }

    // GET: api/Archivos/tss/2026/10/txt
    [HttpGet("tss/{anio}/{mes}/txt")]
    public async Task<IActionResult> ArchivoTss(int anio, int mes)
    {
        var resultado = await _archivos.AutodeterminacionAsync(anio, mes);
        if (!resultado.Ok)
            return this.Error(resultado);
        if (resultado.Valor!.Contenido == null)
            return BadRequest(new { mensaje = "Ningún trabajador tiene documento para el archivo" });
        return File(Encoding.ASCII.GetBytes(resultado.Valor.Contenido), "text/plain", resultado.Valor.NombreArchivo);
    }

    // GET: api/Archivos/dispersion/5?bancoId=1
    [HttpGet("dispersion/{nominaId}")]
    public async Task<IActionResult> Dispersion(int nominaId, [FromQuery] int? bancoId)
    {
        var resultado = await _archivos.DispersionAsync(nominaId, bancoId);
        return resultado.Ok ? File(resultado.Valor!.Contenido, "text/csv", resultado.Valor.Nombre) : this.Error(resultado);
    }

    // GET: api/Archivos/planilla
    [HttpGet("planilla")]
    public async Task<IActionResult> Planilla()
    {
        var archivo = await _archivos.PlanillaPersonalAsync();
        return File(archivo.Contenido, "text/csv", archivo.Nombre);
    }
}
