using Empleados.Data.Data;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Tests;

public class ParametrosLegalesTests
{
    private static EmpleadosDbContext CrearContexto(bool conDatosLegales = false)
    {
        var context = new EmpleadosDbContext(new DbContextOptionsBuilder<EmpleadosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        if (conDatosLegales)
            context.Database.EnsureCreated(); // carga los HasData de DatosLegales
        return context;
    }

    [Fact]
    public async Task Valor_UsaLaVigenciaQueAplicaEnLaFecha()
    {
        using var context = CrearContexto();
        var servicio = new ParametrosLegalesService(context);
        await servicio.AgregarVigenciaAsync(ClavesParametro.AfpEmpleado, 2.87m, new DateTime(2026, 1, 1), null);
        await servicio.AgregarVigenciaAsync(ClavesParametro.AfpEmpleado, 3.00m, new DateTime(2027, 1, 1), null);

        Assert.Null(await servicio.ValorAsync(ClavesParametro.AfpEmpleado, new DateTime(2025, 12, 31)));
        Assert.Equal(2.87m, await servicio.ValorAsync(ClavesParametro.AfpEmpleado, new DateTime(2026, 12, 31)));
        Assert.Equal(3.00m, await servicio.ValorAsync(ClavesParametro.AfpEmpleado, new DateTime(2027, 1, 1)));

        var vigentes = await servicio.VigentesAsync(new DateTime(2026, 6, 1));
        Assert.Equal(2.87m, Assert.Single(vigentes).Valor);
    }

    [Fact]
    public async Task AgregarVigencia_RechazaClaveDesconocida_YFechaRepetida()
    {
        using var context = CrearContexto();
        var servicio = new ParametrosLegalesService(context);
        var fecha = new DateTime(2026, 1, 1);

        Assert.Equal(TipoResultado.Invalido, (await servicio.AgregarVigenciaAsync("No.Existe", 1m, fecha, null)).Tipo);
        Assert.True((await servicio.AgregarVigenciaAsync(ClavesParametro.SfsEmpleado, 3.04m, fecha, null)).Ok);
        Assert.Equal(TipoResultado.Invalido, (await servicio.AgregarVigenciaAsync(ClavesParametro.SfsEmpleado, 3.10m, fecha, null)).Tipo);
    }

    [Fact]
    public async Task DatosLegales2026_EstanCompletos()
    {
        using var context = CrearContexto(conDatosLegales: true);
        var servicio = new ParametrosLegalesService(context);
        var marzo2026 = new DateTime(2026, 3, 1);

        var vigentes = await servicio.VigentesAsync(marzo2026);
        Assert.All(ClavesParametro.Descripciones.Keys.Except(ClavesParametro.Opcionales), clave => Assert.Contains(vigentes, p => p.Clave == clave));
        Assert.Equal(29988.00m, await servicio.SalarioMinimoAsync(TamanoEmpresa.Grande, marzo2026));
        // Antes del 1-feb-2026 no hay salario mínimo cargado (el tramo anterior no está en el sistema)
        Assert.Null(await servicio.SalarioMinimoAsync(TamanoEmpresa.Grande, new DateTime(2026, 1, 15)));

        var feriados = await servicio.FeriadosAsync(2026);
        Assert.Equal(12, feriados.Count);
        Assert.Contains(feriados, f => f.Fecha == new DateTime(2026, 8, 16)); // no se traslada
    }

    [Fact]
    public async Task EscalaIsr2026_TramosContinuos_YCuotasFijasCoherentes()
    {
        using var context = CrearContexto(conDatosLegales: true);

        var tramos = await new ParametrosLegalesService(context).EscalaIsrAsync(new DateTime(2026, 7, 1));

        Assert.Equal(4, tramos.Count);
        Assert.Null(tramos[^1].Hasta);
        for (var i = 1; i < tramos.Count; i++)
        {
            Assert.Equal(tramos[i - 1].Hasta, tramos[i].Desde);
            // La cuota fija de un tramo es el impuesto acumulado de los anteriores (la DGII redondea al peso)
            var esperada = tramos[i - 1].CuotaFija + (tramos[i - 1].Hasta!.Value - tramos[i - 1].Desde) * tramos[i - 1].Tasa / 100;
            Assert.InRange(tramos[i].CuotaFija, esperada - 2, esperada + 2);
        }
    }

    [Fact]
    public async Task Feriados_NoSeRepitenEnLaMismaFecha()
    {
        using var context = CrearContexto();
        var servicio = new ParametrosLegalesService(context);

        Assert.True((await servicio.AgregarFeriadoAsync(new DateTime(2027, 1, 1), "Año Nuevo")).Ok);
        Assert.Equal(TipoResultado.Invalido, (await servicio.AgregarFeriadoAsync(new DateTime(2027, 1, 1), "Otro")).Tipo);
    }
}
