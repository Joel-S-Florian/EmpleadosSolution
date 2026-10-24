using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Tests;

// Validaciones de los datos de nómina del empleado e historial de movimientos
public class DatosNominaTests
{
    private static EmpleadosDbContext CrearContexto(FrecuenciaPago frecuenciaEmpresa = FrecuenciaPago.Quincenal)
    {
        var context = new EmpleadosDbContext(new DbContextOptionsBuilder<EmpleadosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        context.Empresas.Add(new Empresa { Id = 1, Rnc = "101000001", RazonSocial = "Uno SRL", Tamano = TamanoEmpresa.Grande, FrecuenciaPagoPredeterminada = frecuenciaEmpresa });
        context.Departamentos.AddRange(
            new Departamento { Id = 1, EmpresaId = 1, Nombre = "TI" },
            new Departamento { Id = 2, EmpresaId = 1, Nombre = "Ventas" });
        context.Puestos.Add(new Puesto { Id = 1, EmpresaId = 1, DepartamentoId = 1, Nombre = "Dev" });
        context.Bancos.Add(new Banco { Id = 1, Codigo = "BHD", Nombre = "Banco BHD" });
        context.SalariosMinimos.Add(new SalarioMinimo { VigenteDesde = new DateTime(2026, 2, 1), Tamano = TamanoEmpresa.Grande, Monto = 29988m });
        context.SaveChanges();
        return context;
    }

    private static ActualizarEmpleadoDto Dto() => new()
    {
        Nombre = "Ana", Apellido = "García", CorreoElectronico = "ana@test.com", Telefono = "1", Direccion = "d",
        Salario = 35000, FechaNacimiento = new DateTime(1990, 1, 1), FechaContratacion = new DateTime(2024, 1, 1),
        DepartamentoId = 1, PuestoId = 1
    };

    [Theory]
    [InlineData(29987.99, false)]
    [InlineData(29988.00, true)]
    public async Task Salario_NoPuedeSerMenorQueElMinimoDeLaEmpresa(decimal salario, bool valido)
    {
        using var context = CrearContexto();
        var dto = Dto();
        dto.Salario = salario;

        var resultado = await Servicios.Empleados(context).CrearAsync(dto);

        Assert.Equal(valido, resultado.Ok);
        if (!valido)
            Assert.Equal(nameof(CrearEmpleadoDto.Salario), resultado.Campo);
    }

    [Fact]
    public async Task Cedula_SeGuardaSinGuiones_YNoSeRepiteEnLaEmpresa()
    {
        using var context = CrearContexto();
        var servicio = Servicios.Empleados(context);
        var dto = Dto();
        dto.NumeroDocumento = "001-1234567-8";

        var primero = await servicio.CrearAsync(dto);
        dto.CorreoElectronico = "otra@test.com";
        dto.NumeroDocumento = "00112345678";
        var repetido = await servicio.CrearAsync(dto);

        Assert.Equal("00112345678", primero.Valor!.NumeroDocumento);
        Assert.Equal(nameof(CrearEmpleadoDto.NumeroDocumento), repetido.Campo);
    }

    [Theory]
    [InlineData(TipoDocumento.Cedula, "001-123")]
    [InlineData(TipoDocumento.Pasaporte, "AB1")]
    public async Task Documento_ConLongitudInvalida_SeRechaza(TipoDocumento tipo, string numero)
    {
        using var context = CrearContexto();
        var dto = Dto();
        dto.TipoDocumento = tipo;
        dto.NumeroDocumento = numero;

        var resultado = await Servicios.Empleados(context).CrearAsync(dto);

        Assert.Equal(nameof(CrearEmpleadoDto.NumeroDocumento), resultado.Campo);
    }

    [Fact]
    public async Task ContratoPorTiempoDefinido_NecesitaFechaDeFin()
    {
        using var context = CrearContexto();
        var dto = Dto();
        dto.TipoContrato = TipoContrato.TiempoDefinido;

        var resultado = await Servicios.Empleados(context).CrearAsync(dto);

        Assert.Equal(nameof(CrearEmpleadoDto.FechaFinContrato), resultado.Campo);
    }

    [Fact]
    public async Task BancoYCuenta_VanJuntos()
    {
        using var context = CrearContexto();
        var dto = Dto();
        dto.BancoId = 1;

        var resultado = await Servicios.Empleados(context).CrearAsync(dto);

        Assert.Equal(nameof(CrearEmpleadoDto.NumeroCuenta), resultado.Campo);
    }

    [Fact]
    public async Task FrecuenciaDePago_PorDefectoEsLaDeLaEmpresa()
    {
        using var context = CrearContexto(frecuenciaEmpresa: FrecuenciaPago.Mensual);

        var resultado = await Servicios.Empleados(context).CrearAsync(Dto());

        Assert.Equal(FrecuenciaPago.Mensual, resultado.Valor!.FrecuenciaPago);
    }

    [Fact]
    public async Task Historial_RegistraAlta_CambiosYDespido_ConElUsuario()
    {
        using var context = CrearContexto();
        var servicio = Servicios.Empleados(context, new ContextoFijo(empresaId: 1, usuarioId: "maria"));
        var id = (await servicio.CrearAsync(Dto())).Valor!.Id;

        var cambio = Dto();
        cambio.Salario = 42000;
        cambio.DepartamentoId = 2;
        cambio.PuestoId = null;
        Assert.True((await servicio.ActualizarAsync(id, cambio)).Ok);
        Assert.True((await servicio.DespedirAsync(id)).Ok);

        var historial = await servicio.HistorialAsync(id);
        Assert.Equal(
            new[] { TipoMovimiento.Despido, TipoMovimiento.CambioSalario, TipoMovimiento.CambioPuesto, TipoMovimiento.CambioDepartamento, TipoMovimiento.Alta }.OrderBy(t => t),
            historial.Select(m => m.Tipo).OrderBy(t => t));
        var salario = historial.Single(m => m.Tipo == TipoMovimiento.CambioSalario);
        Assert.Equal(("35,000.00", "42,000.00"), (salario.ValorAnterior, salario.ValorNuevo));
        var departamento = historial.Single(m => m.Tipo == TipoMovimiento.CambioDepartamento);
        Assert.Equal(("TI", "Ventas"), (departamento.ValorAnterior, departamento.ValorNuevo));
        Assert.All(historial, m => Assert.Equal("maria", m.UsuarioId));
        Assert.All(historial, m => Assert.Equal(1, m.EmpresaId));
    }
}
