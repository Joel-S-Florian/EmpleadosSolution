using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Empleados.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Empleados.Mvc.Controllers;

[Authorize(Roles = "Admin,RRHH")]
public class NominaController : Controller
{
    private const int FilasAdicionales = 3;

    private readonly NominaService _nomina;
    private readonly EmpleadoService _empleados;

    public NominaController(NominaService nomina, EmpleadoService empleados)
    {
        _nomina = nomina;
        _empleados = empleados;
    }

    // GET: Nomina/Simulador?empleadoId=5
    public async Task<IActionResult> Simulador(int? empleadoId)
    {
        var entrada = new SimulacionNominaDto { EmpleadoId = empleadoId ?? 0 };
        return await Mostrar(entrada, empleadoId.HasValue ? await Simular(entrada) : null);
    }

    // POST: Nomina/Simulador
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Simulador(SimulacionNominaDto entrada)
    {
        return await Mostrar(entrada, ModelState.IsValid ? await Simular(entrada) : null);
    }

    private async Task<SimulacionNomina?> Simular(SimulacionNominaDto entrada)
    {
        var resultado = await _nomina.SimularAsync(entrada);
        if (!resultado.Ok)
            ModelState.AgregarError(resultado);
        return resultado.Valor;
    }

    private async Task<IActionResult> Mostrar(SimulacionNominaDto entrada, SimulacionNomina? resultado)
    {
        // Siempre hay filas vacías para agregar ingresos y deducciones
        while (entrada.OtrosIngresos.Count < FilasAdicionales)
            entrada.OtrosIngresos.Add(new IngresoSimuladoDto());
        while (entrada.Deducciones.Count < FilasAdicionales)
            entrada.Deducciones.Add(new DeduccionSimuladaDto());

        var activos = await _empleados.ParaSeleccionAsync();
        ViewBag.Empleados = new SelectList(
            activos.Select(e => new { e.Id, Texto = $"{e.Nombre} {e.Apellido} · {e.FrecuenciaPago.Texto()} · RD${e.Salario:N2}{(e.TipoSalario == TipoSalario.PorHora ? "/h" : "")}" }),
            "Id", "Texto", entrada.EmpleadoId);
        return View("Simulador", new SimuladorViewModel { Entrada = entrada, Resultado = resultado });
    }
}
