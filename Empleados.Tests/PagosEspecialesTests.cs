using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Tests;

public class PagosEspecialesTests
{
    private static decimal R(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    [Fact]
    public void SalarioNavidad_AnioCompletoEstimado_EsUnSalario()
    {
        var calculo = PagosEspeciales.SalarioNavidad(2026, new DateTime(2020, 1, 1), 45000, false, new Dictionary<int, decimal>());

        Assert.Equal(45000, calculo.Monto);
        Assert.Equal(13, calculo.Explicacion.Count);
    }

    [Fact]
    public void SalarioNavidad_IngresoEnElAnio_EsProporcional()
    {
        // 24 de octubre: 8 de 31 días de octubre + noviembre + diciembre
        var calculo = PagosEspeciales.SalarioNavidad(2026, new DateTime(2026, 10, 24), 30000, false, new Dictionary<int, decimal>());

        Assert.Equal(R((R(30000m * 8 / 31) + 60000) / 12), calculo.Monto);
    }

    [Fact]
    public void SalarioNavidad_UsaLoRegistradoEnNomina()
    {
        var calculo = PagosEspeciales.SalarioNavidad(2026, new DateTime(2020, 1, 1), 45000, false, new Dictionary<int, decimal> { [10] = 42000 });

        Assert.Equal(R((45000m * 11 + 42000) / 12), calculo.Monto);
        Assert.Contains(calculo.Explicacion, e => e.Contains("42,000.00 (nómina)"));
    }

    [Fact]
    public void SalarioNavidad_PorHoraSinNomina_Advierte()
    {
        var calculo = PagosEspeciales.SalarioNavidad(2026, new DateTime(2020, 1, 1), 180, true, new Dictionary<int, decimal> { [1] = 15840 });

        Assert.Equal(R(15840m / 12), calculo.Monto);
        Assert.Equal(11, calculo.Advertencias.Count);
    }

    private static readonly ParticipanteBonificacion[] Participantes =
    {
        new(1, 60000, new DateTime(2015, 5, 1)),  // 3 años o más: tope de 60 días
        new(2, 30000, new DateTime(2026, 7, 1))   // medio año y menos de 3 años: 45 días proporcionales
    };

    [Fact]
    public void Bonificacion_SinTope_RepartePorSalarioYTiempo()
    {
        var partes = PagosEspeciales.Bonificacion(1_000_000, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), Participantes, 23.83m);

        Assert.All(partes, p => Assert.Equal(p.Proporcional, p.Monto));
        Assert.InRange(partes.Sum(p => p.Monto), 99_999.99m, 100_000.01m);
        Assert.Equal(Math.Round(184m / 365, 6), partes[1].FactorTiempo);
        Assert.True(partes[0].Monto / 60000 > partes[1].Monto / 30000); // la mitad del año pesa la mitad
    }

    [Fact]
    public void Bonificacion_ConUtilidadAlta_AplicaElTopeDe45o60Dias()
    {
        var partes = PagosEspeciales.Bonificacion(10_000_000, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), Participantes, 23.83m);

        Assert.Equal(60, partes[0].DiasTope);
        Assert.Equal(R(60000m / 23.83m * 60), partes[0].Monto);
        Assert.Equal(45, partes[1].DiasTope);
        Assert.Equal(R(30000m / 23.83m * 45 * partes[1].FactorTiempo), partes[1].Monto);
        Assert.All(partes, p => Assert.True(p.Proporcional > p.Tope));
    }

    // Servicios sobre la base en memoria: 1 = 45,000 mensual desde 2020; 2 = 30,000 mensual desde marzo de 2026
    private static (EmpleadosDbContext Context, CicloNominaService Ciclo, IncidenciaService Incidencias, BonificacionService Bonificacion) Crear()
    {
        var options = new DbContextOptionsBuilder<EmpleadosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using (var semilla = new EmpleadosDbContext(options))
        {
            semilla.Database.EnsureCreated();
            semilla.Empresas.Add(new Empresa { Id = 1, Rnc = "101000001", RazonSocial = "Uno SRL", TasaRiesgoSrl = 1.20m });
            semilla.Departamentos.Add(new Departamento { Id = 1, EmpresaId = 1, Nombre = "TI" });
            semilla.Empleados.AddRange(Empleado(1, 45000, new DateTime(2020, 1, 1)), Empleado(2, 30000, new DateTime(2026, 3, 1)));
            semilla.SaveChanges();
        }
        var contexto = new ContextoFijo();
        var context = new EmpleadosDbContext(options, contexto);
        var parametros = new ParametrosLegalesService(context);
        var ciclo = new CicloNominaService(context, new NominaService(context, parametros), contexto);
        return (context, ciclo, new IncidenciaService(context, parametros, contexto), new BonificacionService(context, parametros, ciclo, contexto));
    }

    private static Empleado Empleado(int id, decimal salario, DateTime ingreso) => new()
    {
        Id = id, EmpresaId = 1, DepartamentoId = 1, Nombre = "N" + id, Apellido = "A", CorreoElectronico = $"e{id}@test.com",
        Telefono = "1", Direccion = "d", Salario = salario, FechaNacimiento = new DateTime(1990, 1, 1),
        FechaContratacion = ingreso, FrecuenciaPago = FrecuenciaPago.Mensual, NumeroDocumento = $"0010000000{id}", Nss = "N" + id
    };

    private static async Task<NominaPeriodo> Mensual(CicloNominaService ciclo, int mes)
    {
        var resultado = await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = mes, Frecuencia = FrecuenciaPago.Mensual });
        Assert.True(resultado.Ok, resultado.Mensaje);
        return (await ciclo.ObtenerAsync(resultado.Valor!.Id))!;
    }

    [Fact]
    public async Task NominaDeNavidad_TomaLasAusenciasDeLasNominasDelAnio()
    {
        var (_, ciclo, incidencias, _) = Crear();
        var falta = await incidencias.CrearAsync(new IncidenciaDto { EmpleadoId = 1, Tipo = TipoIncidencia.Falta, FechaInicio = new DateTime(2026, 10, 5), FechaFin = new DateTime(2026, 10, 6) });
        await incidencias.AprobarAsync(falta.Valor!.Id);
        await Mensual(ciclo, 10);

        var resultado = await ciclo.CrearSalarioNavidadAsync(2026);
        var navidad = (await ciclo.ObtenerAsync(resultado.Valor!.Id))!;

        Assert.True(resultado.Ok, resultado.Mensaje);
        Assert.Equal("Salario de Navidad 2026", navidad.Descripcion);
        var antiguo = navidad.Recibos.Single(r => r.EmpleadoId == 1);
        var ausencia = R(45000m / 23.83m * 2);
        Assert.Equal(R((45000m * 11 + 45000 - ausencia) / 12), antiguo.Neto);
        Assert.Single(antiguo.Lineas); // sin TSS ni ISR
        Assert.Equal(25000, navidad.Recibos.Single(r => r.EmpleadoId == 2).Neto); // marzo a diciembre
        Assert.Equal(0, navidad.TotalAportesPatronales);
    }

    [Fact]
    public async Task NominaDeNavidad_NoChocaConLaMensualDeDiciembreNiAceptaNovedades()
    {
        var (_, ciclo, _, _) = Crear();
        var navidad = await ciclo.CrearSalarioNavidadAsync(2026);

        var diciembre = await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = 12, Frecuencia = FrecuenciaPago.Mensual });
        var repetida = await ciclo.CrearSalarioNavidadAsync(2026);
        var recibo = (await ciclo.ObtenerAsync(navidad.Valor!.Id))!.Recibos.First();
        var novedades = await ciclo.GuardarNovedadesAsync(recibo.Id, new NovedadesReciboDto { HorasExtra = 2 });

        Assert.True(diciembre.Ok, diciembre.Mensaje);
        Assert.False(repetida.Ok);
        Assert.False(novedades.Ok);
    }

    [Fact]
    public async Task Bonificacion_SeAplicaComoNovedadQueGravaIsrConInfotep()
    {
        var (_, ciclo, _, bonificacion) = Crear();
        var abril = await Mensual(ciclo, 4);
        var antes = abril.Recibos.Single(r => r.EmpleadoId == 1);
        var (isrAntes, baseTssAntes) = (antes.Lineas.Where(l => l.Codigo == Codigos.Isr).Sum(l => l.Monto), antes.BaseTss);

        var reparto = await bonificacion.CalcularAsync(2026, 1_000_000);
        var aplicar = await bonificacion.AplicarAsync(2026, 1_000_000, abril.Id);
        var otraVez = await bonificacion.AplicarAsync(2026, 1_000_000, abril.Id);

        Assert.True(aplicar.Ok, aplicar.Mensaje);
        Assert.True(otraVez.Ok, otraVez.Mensaje);
        var recibo = (await ciclo.ObtenerAsync(abril.Id))!.Recibos.Single(r => r.EmpleadoId == 1);
        var parte = reparto.Valor!.Partes.Single(p => p.EmpleadoId == 1);
        Assert.Equal(2, recibo.Novedades.Count); // reemplaza, no duplica
        Assert.Equal(parte.Parte.Monto, recibo.Lineas.Single(l => l.Concepto == "Bonificación art. 223 (2026)").Monto);
        Assert.Equal(R(parte.Parte.Monto * 0.005m), parte.Infotep);
        Assert.Contains(recibo.Lineas, l => l.Concepto == "INFOTEP sobre la bonificación (2026)" && Math.Abs(l.Monto) == parte.Infotep);
        Assert.True(recibo.Lineas.Where(l => l.Codigo == Codigos.Isr).Sum(l => l.Monto) > isrAntes);
        Assert.Equal(baseTssAntes, recibo.BaseTss); // no cotiza a la TSS
    }

    [Fact]
    public async Task Bonificacion_EjercicioAnteriorUsaLosParametrosDeLaFechaDePago()
    {
        var (_, _, _, bonificacion) = Crear();

        var reparto = await bonificacion.CalcularAsync(2025, 1_000_000);

        Assert.True(reparto.Ok, reparto.Mensaje); // los parámetros rigen desde 2026; se paga en 2026
        Assert.Single(reparto.Valor!.Partes); // quien entró en 2026 no participa del ejercicio 2025
    }

    [Fact]
    public async Task Bonificacion_NoSeAplicaEnNominaAprobada()
    {
        var (_, ciclo, _, bonificacion) = Crear();
        var abril = await Mensual(ciclo, 4);
        Assert.True((await ciclo.AprobarAsync(abril.Id)).Ok);

        var aplicar = await bonificacion.AplicarAsync(2026, 1_000_000, abril.Id);

        Assert.False(aplicar.Ok);
    }
}
