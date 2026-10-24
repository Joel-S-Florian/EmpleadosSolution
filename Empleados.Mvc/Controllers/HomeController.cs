using Empleados.Data.Data;
using Empleados.Data.Entities;
using Empleados.Mvc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Empleados.Mvc.Controllers
{
    public class HomeController : Controller
    {
        // Sin sesión: la página pública; con sesión: lo pendiente de la empresa actual
        public async Task<IActionResult> Index([FromServices] EmpleadosDbContext context)
        {
            if (User.Identity?.IsAuthenticated != true)
                return View("Landing");
            if (!User.IsInRole("Admin") && !User.IsInRole("RRHH"))
                return View(new ResumenInicio());

            var hoy = DateTime.Today;
            return View(new ResumenInicio
            {
                EmpleadosActivos = await context.Empleados.CountAsync(e => e.Estado == EstadoEmpleado.Activo),
                NominasPorAprobar = await context.Nominas.Include(n => n.Recibos).Where(n => n.Estado == EstadoNomina.Calculada).OrderBy(n => n.FechaInicio).ToListAsync(),
                IncidenciasPorAprobar = await context.Incidencias.CountAsync(i => i.Estado == EstadoIncidencia.Pendiente),
                LiquidacionesBorrador = await context.Liquidaciones.CountAsync(l => l.Estado == EstadoLiquidacion.Borrador),
                UltimaPagada = await context.Nominas.Where(n => n.FechaPago != null).OrderByDescending(n => n.FechaPago).ThenByDescending(n => n.Id).FirstOrDefaultAsync(),
                ProximosFeriados = await context.Feriados.Where(f => f.Fecha >= hoy).OrderBy(f => f.Fecha).Take(4).ToListAsync()
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
