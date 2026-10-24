using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Tests;

// El servicio de simulación usa los parámetros legales sembrados (2026) y los datos del empleado
public class SimulacionNominaTests
{
    private static EmpleadosDbContext CrearContexto(bool conDatosLegales = true, IContextoUsuario? contexto = null)
    {
        var options = new DbContextOptionsBuilder<EmpleadosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using (var semilla = new EmpleadosDbContext(options))
        {
            if (conDatosLegales)
                semilla.Database.EnsureCreated();
            semilla.Empresas.AddRange(
                new Empresa { Id = 1, Rnc = "101000001", RazonSocial = "Uno SRL", TasaRiesgoSrl = 1.20m },
                new Empresa { Id = 2, Rnc = "101000002", RazonSocial = "Dos SRL" });
            semilla.Departamentos.AddRange(
                new Departamento { Id = 1, EmpresaId = 1, Nombre = "TI" },
                new Departamento { Id = 2, EmpresaId = 2, Nombre = "Ventas" });
            semilla.Empleados.AddRange(
                Empleado(1, 1, 1, 35000, FrecuenciaPago.Mensual),
                Empleado(2, 1, 1, 60000, FrecuenciaPago.Quincenal, documento: "00112345678", nss: "123"),
                Empleado(3, 2, 2, 50000, FrecuenciaPago.Mensual));
            semilla.SaveChanges();
        }
        return new EmpleadosDbContext(options, contexto);
    }

    private static Empleado Empleado(int id, int empresaId, int departamentoId, decimal salario, FrecuenciaPago frecuencia,
        string? documento = null, string? nss = null) => new()
    {
        Id = id, EmpresaId = empresaId, DepartamentoId = departamentoId, Nombre = "N" + id, Apellido = "A",
        CorreoElectronico = $"e{id}@test.com", Telefono = "1", Direccion = "d", Salario = salario,
        FechaNacimiento = new DateTime(1990, 1, 1), FechaContratacion = new DateTime(2020, 1, 1),
        FrecuenciaPago = frecuencia, NumeroDocumento = documento, Nss = nss
    };

    private static NominaService Servicio(EmpleadosDbContext context) => new(context, new ParametrosLegalesService(context));

    [Fact]
    public async Task Mensual_UsaLosParametrosSembrados_YAdvierteDatosFaltantes()
    {
        using var context = CrearContexto();

        var resultado = await Servicio(context).SimularAsync(new SimulacionNominaDto { EmpleadoId = 1, Anio = 2026, Mes = 10 });

        Assert.True(resultado.Ok, resultado.Mensaje);
        Assert.Equal(32931.50m, resultado.Valor!.Recibo.Neto); // caso golden A
        Assert.Contains(resultado.Valor.Advertencias, a => a.Contains("cédula"));
        Assert.Contains(resultado.Valor.Advertencias, a => a.Contains("NSS"));
    }

    [Fact]
    public async Task SegundaQuincena_CalculaAntesLaPrimeraParaElAcumulado()
    {
        using var context = CrearContexto();

        var resultado = await Servicio(context).SimularAsync(new SimulacionNominaDto { EmpleadoId = 2, Anio = 2026, Mes = 10, Quincena = 2 });

        Assert.True(resultado.Ok, resultado.Mensaje);
        Assert.Equal(26483.67m, resultado.Valor!.PrimeraQuincena!.Neto); // caso golden C
        Assert.Equal(1743.32m, resultado.Valor.Recibo.Monto(Codigos.Isr));
        Assert.Empty(resultado.Valor.Advertencias);
    }

    [Fact]
    public async Task IngresosYDeducciones_SeConviertenYLasFilasVaciasSeIgnoran()
    {
        using var context = CrearContexto();
        var dto = new SimulacionNominaDto
        {
            EmpleadoId = 1, Anio = 2026, Mes = 10, HorasExtra = 10,
            OtrosIngresos = { new IngresoSimuladoDto { Concepto = "Comisión", Monto = 5000 }, new IngresoSimuladoDto() },
            Deducciones = { new DeduccionSimuladaDto { Concepto = "Préstamo", Monto = 1000 }, new DeduccionSimuladaDto() }
        };

        var recibo = (await Servicio(context).SimularAsync(dto)).Valor!.Recibo;

        Assert.Equal(5000m, recibo.Monto(Codigos.OtroIngreso));
        Assert.Equal(1000m, recibo.Monto(Codigos.Deduccion));
        Assert.True(recibo.Monto(Codigos.HoraExtra) > 0);
    }

    [Fact]
    public async Task SinParametrosLegales_NoSimula()
    {
        using var context = CrearContexto(conDatosLegales: false);

        var resultado = await Servicio(context).SimularAsync(new SimulacionNominaDto { EmpleadoId = 1, Anio = 2026, Mes = 10 });

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        Assert.Contains("Falta el parámetro legal", resultado.Mensaje);
    }

    [Fact]
    public async Task EmpleadoDeOtraEmpresa_NoSeEncuentra()
    {
        using var context = CrearContexto(contexto: new ContextoFijo(empresaId: 1));

        var resultado = await Servicio(context).SimularAsync(new SimulacionNominaDto { EmpleadoId = 3, Anio = 2026, Mes = 10 });

        Assert.Equal(TipoResultado.NoEncontrado, resultado.Tipo);
    }

    [Fact]
    public async Task IngresoConMontoSinConcepto_SeRechaza()
    {
        using var context = CrearContexto();
        var dto = new SimulacionNominaDto { EmpleadoId = 1, Anio = 2026, Mes = 10, OtrosIngresos = { new IngresoSimuladoDto { Monto = 100 } } };

        var resultado = await Servicio(context).SimularAsync(dto);

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
    }
}
