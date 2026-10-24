using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Empleados.Mvc.Controllers;

// Faltas, retardos, permisos, licencias y vacaciones; las aprobadas con descuento entran solas en la nómina
[Authorize(Roles = "Admin,RRHH")]
public class IncidenciasController : Controller
{
    private readonly IncidenciaService _incidencias;
    private readonly EmpleadoService _empleados;
    private readonly DepartamentoService _departamentos;

    public IncidenciasController(IncidenciaService incidencias, EmpleadoService empleados, DepartamentoService departamentos)
    {
        _incidencias = incidencias;
        _empleados = empleados;
        _departamentos = departamentos;
    }

    // GET: Incidencias?anio=2026&mes=10&estado=Pendiente&empleadoId=5
    public async Task<IActionResult> Index(int? anio, int? mes, EstadoIncidencia? estado, int? empleadoId)
    {
        return await Mostrar(new IncidenciaDto { EmpleadoId = empleadoId ?? 0 }, anio, mes, estado, empleadoId);
    }

    // POST: Incidencias/Crear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(IncidenciaDto dto)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _incidencias.CrearAsync(dto);
            if (resultado.Ok)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index), new { anio = dto.FechaInicio.Year, mes = dto.FechaInicio.Month });
            }
            ModelState.AgregarError(resultado);
        }
        return await Mostrar(dto, dto.FechaInicio.Year, dto.FechaInicio.Month, null, null);
    }

    // POST: Incidencias/Colectivas
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Colectivas(VacacionesColectivasDto dto)
    {
        Avisar(await _incidencias.VacacionesColectivasAsync(dto));
        return RedirectToAction(nameof(Index), new { anio = dto.FechaInicio.Year, mes = dto.FechaInicio.Month });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id, string? volver) => Volver(await _incidencias.AprobarAsync(id), volver);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(int id, string? motivo, string? volver) => Volver(await _incidencias.RechazarAsync(id, motivo), volver);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id, string? volver) => Volver(await _incidencias.EliminarAsync(id), volver);

    private IActionResult Volver(Resultado resultado, string? volver)
    {
        if (resultado.Tipo == TipoResultado.NoEncontrado)
            return NotFound();
        Avisar(resultado);
        return Url.IsLocalUrl(volver) ? LocalRedirect(volver) : RedirectToAction(nameof(Index));
    }

    private void Avisar(Resultado resultado) =>
        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;

    private async Task<IActionResult> Mostrar(IncidenciaDto nuevo, int? anio, int? mes, EstadoIncidencia? estado, int? empleadoId)
    {
        var desde = new DateTime(anio ?? DateTime.Today.Year, mes ?? DateTime.Today.Month, 1);
        var empleados = (await _empleados.ListarAsync(null, null, null, pageSize: 100)).Items
            .Where(e => e.Estado != EstadoEmpleado.Despedido)
            .OrderBy(e => e.Nombre).ThenBy(e => e.Apellido)
            .Select(e => new { e.Id, Texto = $"{e.Nombre} {e.Apellido}" })
            .ToList();
        ViewBag.Empleados = new SelectList(empleados, "Id", "Texto", nuevo.EmpleadoId);
        ViewBag.FiltroEmpleados = new SelectList(empleados, "Id", "Texto", empleadoId);
        ViewBag.Departamentos = new SelectList(await _departamentos.ListarAsync(), "Id", "Nombre");
        ViewBag.Nuevo = nuevo;
        ViewBag.Desde = desde;
        ViewBag.Estado = estado;
        ViewBag.EmpleadoId = empleadoId;
        return View("Index", await _incidencias.ListarAsync(empleadoId, estado, desde, desde.AddMonths(1).AddDays(-1)));
    }
}
