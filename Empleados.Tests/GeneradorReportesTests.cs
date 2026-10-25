using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;

namespace Empleados.Tests;

public class GeneradorReportesTests
{
    // 1 = TI 45,000 desde 2020; 2 = Ventas 30,000 desde marzo de 2026; 3 = salió en junio de 2026
    private static (EmpleadosDbContext Context, GeneradorReportes Reportes, CicloNominaService Ciclo, IncidenciaService Incidencias) Crear()
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
        return (context, new GeneradorReportes(context, incidencias), new CicloNominaService(context, new NominaService(context, parametros), contexto), incidencias);
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

        var tabla = await reportes.CostoPorDepartamentoAsync(new(2026, 10));

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

        Assert.Empty((await reportes.CostoPorDepartamentoAsync(new(2026, 10))).Filas);
    }

    [Fact]
    public async Task Acumulados_RetencionesDelAnio()
    {
        var (_, reportes, ciclo, _) = Crear();
        await OctubreAprobada(ciclo);

        var tabla = await reportes.AcumuladosAnualesAsync(new(2026));
        var fila = tabla.Filas.Single(f => (string)f[0]! == "N1 A");

        Assert.Equal(45000m, fila[2]);
        Assert.Equal(1291.50m, fila[4]); // AFP 2.87 %
    }

    [Fact]
    public async Task Rotacion_CuentaAltasYBajasDelMes()
    {
        var (_, reportes, _, _) = Crear();

        var tabla = await reportes.RotacionAsync(new(2026));

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

        var ausentismo = await reportes.AusentismoAsync(new(2026, 10));
        var vacaciones = await reportes.VacacionesPendientesAsync(new(2026));

        Assert.Equal(2m, ausentismo.Filas.Single()[1]);
        Assert.Equal("N1 A", vacaciones.Filas.Single()[0]); // el de marzo de 2026 aún no cumple un año
    }

    [Fact]
    public async Task Exportar_CsvYPdf()
    {
        var (context, reportes, ciclo, _) = Crear();
        await OctubreAprobada(ciclo);
        var tabla = await reportes.CostoPorDepartamentoAsync(new(2026, 10));

        var csv = Encoding.UTF8.GetString(GeneradorReportes.Csv(tabla)).TrimStart('﻿').Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        var pdf = GeneradorReportes.Pdf(tabla, await context.Empresas.FirstAsync());

        Assert.StartsWith("Departamento,Empleados,Ingresos", csv[0]);
        Assert.StartsWith("Total,2,75000", csv[^1]);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task Plantilla_FiltraPorDepartamentoYEstado()
    {
        var (_, reportes, _, _) = Crear();

        var ventas = await reportes.PlantillaAsync(new(2026, DepartamentoId: 2));
        var despedidos = await reportes.PlantillaAsync(new(2026, Estado: EstadoEmpleado.Despedido));
        var todos = await reportes.PlantillaAsync(new(2026));

        Assert.Single(ventas.Filas); // el de ventas que salió no aparece por defecto
        Assert.Equal("A, N3", Assert.Single(despedidos.Filas)[0]);
        Assert.Equal(2, todos.Filas.Count);
        Assert.Equal(75000m, todos.Totales![7]);
    }

    [Fact]
    public async Task DetalleYComparativo_UsanLaNominaAprobada()
    {
        var (context, reportes, ciclo, _) = Crear();
        await OctubreAprobada(ciclo);
        var nomina = await context.Nominas.FirstAsync();

        var detalle = await reportes.DetalleNominaAsync(new(2026, NominaId: nomina.Id));
        var comparativo = await reportes.ComparativoMensualAsync(new(2026));
        var porDepartamento = await reportes.EmpleadosPorDepartamentoAsync();

        Assert.Equal(2, detalle.Filas.Count);
        Assert.Equal(nomina.TotalNeto, detalle.Totales![7]);
        Assert.Equal(75000m, comparativo.Filas[9][2]); // octubre
        Assert.Equal(0m, comparativo.Filas[8][2]);
        Assert.Equal(new object?[] { "TI", 1m, 1m, 45000m, "45,000.00" }, porDepartamento.Filas[0]);
    }

    [Fact]
    public async Task Excel_EsUnXlsxConEncabezadoTotalesYAutofiltro()
    {
        var (context, reportes, ciclo, _) = Crear();
        await OctubreAprobada(ciclo);
        var tabla = await reportes.CostoPorDepartamentoAsync(new(2026, 10));

        var xlsx = ExcelReporte.Generar(tabla, await context.Empresas.FirstAsync());

        using var zip = new ZipArchive(new MemoryStream(xlsx));
        Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
        Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
        var hoja = new StreamReader(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open()).ReadToEnd();
        Assert.Contains("<t xml:space=\"preserve\">Departamento</t>", hoja);
        Assert.Contains("<v>45000</v>", hoja);
        Assert.Contains("<t xml:space=\"preserve\">Total</t>", hoja);
        Assert.Contains("<autoFilter ref=\"A4:H7\"/>", hoja); // encabezado, 2 departamentos y totales
        Assert.Equal("AA", ExcelReporte.Letra(26));
    }

    [Fact]
    public async Task ResumenDeNomina_GeneraPdf()
    {
        var (context, _, ciclo, _) = Crear();
        await OctubreAprobada(ciclo);
        var nomina = (await ciclo.ObtenerAsync((await context.Nominas.FirstAsync()).Id))!;

        var pdf = ResumenNominaPdf.Generar(await context.Empresas.FirstAsync(), nomina);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void Catalogo_DescribeTodosLosReportes() =>
        Assert.All(Enum.GetValues<TipoReporte>(), t => Assert.True(GeneradorReportes.Catalogo.ContainsKey(t), t.ToString()));
}
