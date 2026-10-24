using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Empleados.Tests;

public class ReportesNominaTests
{
    // 1 = TI 45,000 desde 2020; 2 = Ventas 30,000 desde marzo de 2026; 3 = salió en junio de 2026
    private static (EmpleadosDbContext Context, ReportesNominaService Reportes, CicloNominaService Ciclo, IncidenciaService Incidencias) Crear()
    {
        var options = new DbContextOptionsBuilder<EmpleadosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using (var semilla = new EmpleadosDbContext(options))
        {
            semilla.Database.EnsureCreated();
            semilla.Empresas.Add(new Empresa { Id = 1, Rnc = "101000001", RazonSocial = "Uno SRL", TasaRiesgoSrl = 1.20m });
            semilla.Departamentos.AddRange(new Departamento { Id = 1, EmpresaId = 1, Nombre = "TI" }, new Departamento { Id = 2, EmpresaId = 1, Nombre = "Ventas" });
            semilla.Empleados.AddRange(
                Empleado(1, 1, 45000, new DateTime(2020, 1, 1)),
                Empleado(2, 2, 30000, new DateTime(2026, 3, 1)),
                Empleado(3, 2, 30000, new DateTime(2025, 1, 1), new DateTime(2026, 6, 15)));
            semilla.SaveChanges();
        }
        var contexto = new ContextoFijo();
        var context = new EmpleadosDbContext(options, contexto);
        var parametros = new ParametrosLegalesService(context);
        var incidencias = new IncidenciaService(context, parametros, contexto);
        return (context, new ReportesNominaService(context, incidencias), new CicloNominaService(context, new NominaService(context, parametros), contexto), incidencias);
    }

    private static Empleado Empleado(int id, int departamento, decimal salario, DateTime ingreso, DateTime? salida = null) => new()
    {
        Id = id, EmpresaId = 1, DepartamentoId = departamento, Nombre = "N" + id, Apellido = "A", CorreoElectronico = $"e{id}@test.com",
        Telefono = "1", Direccion = "d", Salario = salario, FechaNacimiento = new DateTime(1990, 1, 1), FechaContratacion = ingreso,
        FechaTerminacion = salida, Estado = salida == null ? EstadoEmpleado.Activo : EstadoEmpleado.Despedido,
        FrecuenciaPago = FrecuenciaPago.Mensual, NumeroDocumento = $"0010000000{id}", Nss = "N" + id
    };

    private static async Task OctubreAprobada(CicloNominaService ciclo)
    {
        var nomina = await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = 10, Frecuencia = FrecuenciaPago.Mensual });
        Assert.True((await ciclo.AprobarAsync(nomina.Valor!.Id)).Ok);
    }

    [Fact]
    public async Task CostoPorDepartamento_SumaIngresosYAportes()
    {
        var (_, reportes, ciclo, _) = Crear();
        await OctubreAprobada(ciclo);

        var tabla = await reportes.CostoPorDepartamentoAsync(2026, 10);

        Assert.Equal(new[] { "TI", "Ventas" }, tabla.Filas.Select(f => (string)f[0]!));
        Assert.Equal(45000m, tabla.Filas[0][2]);
        Assert.Equal(75000m, tabla.Totales![2]);
        Assert.Equal((decimal)tabla.Totales[2]! + (decimal)tabla.Totales[6]!, tabla.Totales[7]); // costo = ingresos + aportes
    }

    [Fact]
    public async Task NominaSinAprobar_NoEntraEnLosReportes()
    {
        var (_, reportes, ciclo, _) = Crear();
        await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = 10, Frecuencia = FrecuenciaPago.Mensual });

        Assert.Empty((await reportes.CostoPorDepartamentoAsync(2026, 10)).Filas);
    }

    [Fact]
    public async Task Acumulados_RetencionesDelAnio()
    {
        var (_, reportes, ciclo, _) = Crear();
        await OctubreAprobada(ciclo);

        var tabla = await reportes.AcumuladosAnualesAsync(2026);
        var fila = tabla.Filas.Single(f => (string)f[0]! == "N1 A");

        Assert.Equal(45000m, fila[2]);
        Assert.Equal(1291.50m, fila[4]); // AFP 2.87 %
    }

    [Fact]
    public async Task Rotacion_CuentaAltasYBajasDelMes()
    {
        var (_, reportes, _, _) = Crear();

        var tabla = await reportes.RotacionAsync(2026);

        Assert.Equal(1m, tabla.Filas[2][2]); // marzo: un alta
        Assert.Equal(1m, tabla.Filas[5][3]); // junio: una baja
        Assert.Equal(2m, tabla.Filas[5][4]); // al final de junio quedan 2
    }

    [Fact]
    public async Task Ausentismo_YVacacionesPendientes()
    {
        var (_, reportes, _, incidencias) = Crear();
        var falta = await incidencias.CrearAsync(new IncidenciaDto { EmpleadoId = 1, Tipo = TipoIncidencia.Falta, FechaInicio = new DateTime(2026, 10, 5), FechaFin = new DateTime(2026, 10, 6) });
        await incidencias.AprobarAsync(falta.Valor!.Id);

        var ausentismo = await reportes.AusentismoAsync(2026, 10);
        var vacaciones = await reportes.VacacionesPendientesAsync();

        Assert.Equal(2m, ausentismo.Filas.Single()[1]);
        Assert.Equal("N1 A", vacaciones.Filas.Single()[0]); // el de marzo de 2026 aún no cumple un año
    }

    [Fact]
    public async Task Exportar_CsvYPdf()
    {
        var (context, reportes, ciclo, _) = Crear();
        await OctubreAprobada(ciclo);
        var tabla = await reportes.CostoPorDepartamentoAsync(2026, 10);

        var csv = Encoding.UTF8.GetString(ReportesNominaService.Csv(tabla)).TrimStart('﻿').Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        var pdf = ReportesNominaService.Pdf(tabla, await context.Empresas.FirstAsync());

        Assert.StartsWith("Departamento,Empleados,Ingresos", csv[0]);
        Assert.StartsWith("Total,2,75000", csv[^1]);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }
}
