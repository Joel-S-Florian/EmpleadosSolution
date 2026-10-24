using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Api.Controllers;

// Ciclo de nómina: Admin y RRHH preparan y calculan; solo el Admin aprueba, paga y cierra
[Authorize(Roles = "Admin,RRHH")]
[ApiController]
[Route("api/[controller]")]
public class NominasController : ControllerBase
{
    private readonly CicloNominaService _ciclo;
    private readonly EmpleadosDbContext _context;

    public NominasController(CicloNominaService ciclo, EmpleadosDbContext context)
    {
        _ciclo = ciclo;
        _context = context;
    }

    // GET: api/Nominas
    [HttpGet]
    public async Task<IActionResult> GetNominas()
    {
        var nominas = await _ciclo.ListarAsync();
        return Ok(nominas.Select(n => new
        {
            n.Id, n.Tipo, n.Descripcion, n.Anio, n.Mes, n.Quincena, n.Frecuencia, n.Estado, n.FechaInicio, n.FechaFin,
            Recibos = n.Recibos.Count, n.TotalIngresos, n.TotalDeducciones, n.TotalNeto, n.TotalAportesPatronales
        }));
    }

    // GET: api/Nominas/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNomina(int id)
    {
        var n = await _ciclo.ObtenerAsync(id);
        if (n == null)
            return NotFound(new { mensaje = "Nómina no encontrada" });

        return Ok(new
        {
            n.Id, n.Tipo, n.Descripcion, n.Anio, n.Mes, n.Quincena, n.Frecuencia, n.Estado, n.FechaInicio, n.FechaFin,
            n.TotalIngresos, n.TotalDeducciones, n.TotalNeto, n.TotalAportesPatronales,
            n.CreadaPor, n.FechaCalculo, n.AprobadaPor, n.FechaAprobacion, n.FechaPago, n.FechaCierre,
            Recibos = n.Recibos.OrderBy(r => r.NombreEmpleado).Select(Recibo)
        });
    }

    // GET: api/Nominas/recibos/7
    [HttpGet("recibos/{reciboId}")]
    public async Task<IActionResult> GetRecibo(int reciboId)
    {
        var recibo = await _ciclo.ObtenerReciboAsync(reciboId);
        return recibo == null ? NotFound(new { mensaje = "Recibo no encontrado" }) : Ok(Recibo(recibo));
    }

    // GET: api/Nominas/recibos/7/pdf
    [HttpGet("recibos/{reciboId}/pdf")]
    public async Task<IActionResult> GetReciboPdf(int reciboId)
    {
        var recibo = await _ciclo.ObtenerReciboAsync(reciboId);
        if (recibo == null)
            return NotFound(new { mensaje = "Recibo no encontrado" });
        var empresa = await _context.Empresas.FirstAsync(e => e.Id == recibo.EmpresaId);
        return File(ReciboPdf.Generar(empresa, recibo.Nomina!, new[] { recibo }), "application/pdf", $"recibo-{recibo.Id}.pdf");
    }

    // POST: api/Nominas - crea y calcula la nómina del período en la empresa actual
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearNominaDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var resultado = await _ciclo.CrearAsync(dto);
        if (!resultado.Ok)
            return this.Error(resultado);
        return CreatedAtAction(nameof(GetNomina), new { id = resultado.Valor!.Id }, new { resultado.Valor.Id, mensaje = resultado.Mensaje });
    }

    // PUT: api/Nominas/recibos/7/novedades - reemplaza las novedades del recibo y recalcula
    [HttpPut("recibos/{reciboId}/novedades")]
    public async Task<IActionResult> GuardarNovedades(int reciboId, [FromBody] NovedadesReciboDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        return Respuesta(await _ciclo.GuardarNovedadesAsync(reciboId, dto));
    }

    [HttpPost("{id}/calcular")]
    public async Task<IActionResult> Calcular(int id) => Respuesta(await _ciclo.CalcularAsync(id));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id) => Respuesta(await _ciclo.EliminarAsync(id));

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/aprobar")]
    public async Task<IActionResult> Aprobar(int id) => Respuesta(await _ciclo.AprobarAsync(id));

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/reabrir")]
    public async Task<IActionResult> Reabrir(int id) => Respuesta(await _ciclo.ReabrirAsync(id));

    // POST: api/Nominas/5/pagar?fechaPago=2026-10-15
    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/pagar")]
    public async Task<IActionResult> Pagar(int id, [FromQuery] DateTime? fechaPago, [FromServices] RecibosCorreoService correo)
    {
        var resultado = await _ciclo.PagarAsync(id, fechaPago ?? DateTime.Today);
        if (!resultado.Ok)
            return this.Error(resultado);
        var envio = await correo.EnviarNominaAsync(id);
        return Ok(new { mensaje = resultado.Mensaje, correo = envio.Mensaje });
    }

    // POST: api/Nominas/5/enviar-recibos
    [HttpPost("{id}/enviar-recibos")]
    public async Task<IActionResult> EnviarRecibos(int id, [FromServices] RecibosCorreoService correo) => Respuesta(await correo.EnviarNominaAsync(id));

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/cerrar")]
    public async Task<IActionResult> Cerrar(int id) => Respuesta(await _ciclo.CerrarAsync(id));

    private IActionResult Respuesta(Resultado resultado) =>
        resultado.Ok ? Ok(new { mensaje = resultado.Mensaje }) : this.Error(resultado);

    private static object Recibo(ReciboPago r) => new
    {
        r.Id, r.NominaId, r.EmpleadoId, r.NombreEmpleado, r.Documento, r.Nss, r.Departamento, r.Puesto, r.Salario, r.TipoSalario,
        r.TotalIngresos, r.TotalDeduccionesLey, r.TotalDeduccionesInternas, r.Neto, r.TotalAportesPatronales,
        Errores = CicloNominaService.Lista(r.ErroresJson),
        Advertencias = CicloNominaService.Lista(r.AdvertenciasJson),
        Lineas = r.Lineas.OrderBy(l => l.Orden).Select(l => new { l.Tipo, l.Codigo, l.Concepto, l.Cantidad, l.Monto, l.PrestamoId }),
        Novedades = new
        {
            r.HorasTrabajadas, r.DiasAusenciaSinPago, r.HorasExtra, r.HorasExtraExceso, r.HorasNocturnas, r.HorasFeriado,
            Otros = r.Novedades.Select(n => new { n.Tipo, n.Concepto, n.Monto, n.CotizaTss, n.GravaIsr, n.Prioridad })
        },
        Explicacion = CicloNominaService.Lista(r.ExplicacionJson)
    };
}
