using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Mvc.Controllers;

// Ciclo de nómina: RRHH prepara y calcula; el Admin aprueba, paga y cierra
[Authorize(Roles = "Admin,RRHH")]
public class NominasController : Controller
{
    private const int FilasAdicionales = 3;

    private readonly CicloNominaService _ciclo;
    private readonly EmpleadosDbContext _context;

    public NominasController(CicloNominaService ciclo, EmpleadosDbContext context)
    {
        _ciclo = ciclo;
        _context = context;
    }

    // GET: Nominas
    public async Task<IActionResult> Index()
    {
        ViewBag.Nueva = new CrearNominaDto();
        return View(await _ciclo.ListarAsync());
    }

    // POST: Nominas/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearNominaDto dto)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }
        var resultado = await _ciclo.CrearAsync(dto);
        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return resultado.Ok ? RedirectToAction(nameof(Detalle), new { id = resultado.Valor!.Id }) : RedirectToAction(nameof(Index));
    }

    // GET: Nominas/Detalle/5
    public async Task<IActionResult> Detalle(int id)
    {
        var nomina = await _ciclo.ObtenerAsync(id);
        if (nomina == null)
            return NotFound();
        ViewBag.NetosAnteriores = await _ciclo.NetosAnterioresAsync(nomina);
        return View(nomina);
    }

    // GET: Nominas/Recibo/5
    public async Task<IActionResult> Recibo(int id)
    {
        var recibo = await _ciclo.ObtenerReciboAsync(id);
        return recibo == null ? NotFound() : View(recibo);
    }

    // GET: Nominas/Novedades/5 (id del recibo)
    public async Task<IActionResult> Novedades(int id)
    {
        var recibo = await _ciclo.ObtenerReciboAsync(id);
        if (recibo == null)
            return NotFound();

        var dto = new NovedadesReciboDto
        {
            HorasTrabajadas = recibo.HorasTrabajadas,
            DiasAusenciaSinPago = recibo.DiasAusenciaSinPago,
            HorasExtra = recibo.HorasExtra,
            HorasExtraExceso = recibo.HorasExtraExceso,
            HorasNocturnas = recibo.HorasNocturnas,
            HorasFeriado = recibo.HorasFeriado,
            OtrosIngresos = recibo.Novedades.Where(n => n.Tipo == TipoNovedad.Ingreso)
                .Select(n => new IngresoSimuladoDto { Concepto = n.Concepto, Monto = n.Monto, CotizaTss = n.CotizaTss, GravaIsr = n.GravaIsr }).ToList(),
            Deducciones = recibo.Novedades.Where(n => n.Tipo == TipoNovedad.Deduccion)
                .Select(n => new DeduccionSimuladaDto { Concepto = n.Concepto, Monto = n.Monto, Prioridad = n.Prioridad }).ToList()
        };
        return MostrarNovedades(recibo, dto);
    }

    // POST: Nominas/Novedades/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Novedades(int id, NovedadesReciboDto dto)
    {
        var recibo = await _ciclo.ObtenerReciboAsync(id);
        if (recibo == null)
            return NotFound();

        if (ModelState.IsValid)
        {
            var resultado = await _ciclo.GuardarNovedadesAsync(id, dto);
            if (resultado.Ok)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Detalle), new { id = recibo.NominaId });
            }
            ModelState.AgregarError(resultado);
        }
        return MostrarNovedades(recibo, dto);
    }

    private IActionResult MostrarNovedades(ReciboPago recibo, NovedadesReciboDto dto)
    {
        while (dto.OtrosIngresos.Count < dto.OtrosIngresos.Count(i => !string.IsNullOrWhiteSpace(i.Concepto)) + FilasAdicionales)
            dto.OtrosIngresos.Add(new IngresoSimuladoDto());
        while (dto.Deducciones.Count < dto.Deducciones.Count(d => !string.IsNullOrWhiteSpace(d.Concepto)) + FilasAdicionales)
            dto.Deducciones.Add(new DeduccionSimuladaDto());
        ViewBag.Recibo = recibo;
        return View("Novedades", dto);
    }

    // GET: Nominas/ReciboPdf/5
    public async Task<IActionResult> ReciboPdf(int id)
    {
        var recibo = await _ciclo.ObtenerReciboAsync(id);
        if (recibo == null)
            return NotFound();
        var empresa = await _context.Empresas.FirstAsync(e => e.Id == recibo.EmpresaId);
        var pdf = Data.Services.ReciboPdf.Generar(empresa, recibo.Nomina!, new[] { recibo });
        return File(pdf, "application/pdf", $"recibo-{recibo.Nomina!.Anio}-{recibo.Nomina.Mes:00}-{recibo.NombreEmpleado.Replace(' ', '-')}.pdf");
    }

    // GET: Nominas/RecibosPdf/5 (todos los recibos de la nómina)
    public async Task<IActionResult> RecibosPdf(int id)
    {
        var nomina = await _ciclo.ObtenerAsync(id);
        if (nomina == null)
            return NotFound();
        var empresa = await _context.Empresas.FirstAsync(e => e.Id == nomina.EmpresaId);
        var pdf = Data.Services.ReciboPdf.Generar(empresa, nomina, nomina.Recibos.OrderBy(r => r.NombreEmpleado));
        return File(pdf, "application/pdf", $"recibos-{nomina.Anio}-{nomina.Mes:00}{(nomina.Quincena > 0 ? $"-q{nomina.Quincena}" : "")}.pdf");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calcular(int id) => Avisar(await _ciclo.CalcularAsync(id), id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await _ciclo.EliminarAsync(id);
        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return resultado.Ok ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Detalle), new { id });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id) => Avisar(await _ciclo.AprobarAsync(id), id);

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reabrir(int id) => Avisar(await _ciclo.ReabrirAsync(id), id);

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pagar(int id, DateTime fechaPago) => Avisar(await _ciclo.PagarAsync(id, fechaPago), id);

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id) => Avisar(await _ciclo.CerrarAsync(id), id);

    private IActionResult Avisar(Resultado resultado, int id)
    {
        if (resultado.Tipo == TipoResultado.NoEncontrado)
            return NotFound();
        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Detalle), new { id });
    }
}
