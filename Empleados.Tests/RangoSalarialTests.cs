using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Tests;

// Rango salarial de los puestos y accesos de la empresa
public class RangoSalarialTests
{
    private static EmpleadosDbContext CrearContexto()
    {
        var context = new EmpleadosDbContext(new DbContextOptionsBuilder<EmpleadosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Empresas.Add(new Empresa { Id = 1, Rnc = "101000001", RazonSocial = "Uno SRL", Tamano = TamanoEmpresa.Grande });
        context.Departamentos.Add(new Departamento { Id = 1, EmpresaId = 1, Nombre = "TI" });
        context.Puestos.Add(new Puesto { Id = 1, EmpresaId = 1, DepartamentoId = 1, Nombre = "Analista", SalarioMinimo = 40000, SalarioMaximo = 60000 });
        context.SalariosMinimos.Add(new SalarioMinimo { VigenteDesde = new DateTime(2026, 2, 1), Tamano = TamanoEmpresa.Grande, Monto = 29988m });
        context.SaveChanges();
        return context;
    }

    private static CrearEmpleadoDto Dto(decimal salario, TipoSalario tipo = TipoSalario.FijoMensual) => new()
    {
        Nombre = "Ana", Apellido = "García", CorreoElectronico = $"ana{salario}@test.com", Telefono = "1", Direccion = "d",
        Salario = salario, TipoSalario = tipo, FechaNacimiento = new DateTime(1990, 1, 1), FechaContratacion = new DateTime(2024, 1, 1),
        DepartamentoId = 1, PuestoId = 1
    };

    [Theory]
    [InlineData(39999.99, false)]
    [InlineData(40000, true)]
    [InlineData(60000, true)]
    [InlineData(60000.01, false)]
    public async Task Salario_DebeQuedarDentroDelRangoDelPuesto(decimal salario, bool valido)
    {
        using var context = CrearContexto();

        var resultado = await Servicios.Empleados(context).CrearAsync(Dto(salario));

        Assert.Equal(valido, resultado.Ok);
        if (!valido)
        {
            Assert.Equal(nameof(CrearEmpleadoDto.Salario), resultado.Campo);
            Assert.Contains("rango del puesto Analista", resultado.Mensaje);
        }
    }

    [Fact]
    public async Task SalarioPorHora_NoSeComparaConElRangoMensual()
    {
        using var context = CrearContexto();

        Assert.True((await Servicios.Empleados(context).CrearAsync(Dto(250, TipoSalario.PorHora))).Ok);
    }

    [Fact]
    public async Task Puesto_MinimoNoPuedeSuperarAlMaximo_YAvisaQuienQuedaFuera()
    {
        using var context = CrearContexto();
        await Servicios.Empleados(context).CrearAsync(Dto(45000));
        var servicio = Servicios.Puestos(context);

        var invertido = await servicio.CrearAsync(new CrearPuestoDto { Nombre = "Otro", DepartamentoId = 1, SalarioMinimo = 50000, SalarioMaximo = 40000 });
        var nuevoRango = await servicio.ActualizarAsync(1, new CrearPuestoDto { Nombre = "Analista", DepartamentoId = 1, SalarioMinimo = 50000, SalarioMaximo = 70000 });

        Assert.False(invertido.Ok);
        Assert.True(nuevoRango.Ok);
        Assert.Contains("1 empleado(s)", nuevoRango.Mensaje);
    }

    [Fact]
    public async Task AsignarUsuarios_NoQuitaElAccesoDeLosUsuariosDelPortal()
    {
        using var context = CrearContexto();
        context.Users.AddRange(new IdentityUser { Id = "rrhh1", UserName = "rrhh" }, new IdentityUser { Id = "admin1", UserName = "admin" }, new IdentityUser { Id = "portal1", UserName = "ana" });
        context.Empleados.Add(new Empleado
        {
            Id = 1, EmpresaId = 1, DepartamentoId = 1, Nombre = "Ana", Apellido = "A", CorreoElectronico = "a@test.com", Telefono = "1",
            Direccion = "d", Salario = 45000, FechaNacimiento = new DateTime(1990, 1, 1), FechaContratacion = new DateTime(2024, 1, 1), UsuarioId = "portal1"
        });
        context.UsuariosEmpresas.AddRange(new UsuarioEmpresa { UsuarioId = "rrhh1", EmpresaId = 1 }, new UsuarioEmpresa { UsuarioId = "portal1", EmpresaId = 1 });
        await context.SaveChangesAsync();

        var resultado = await new EmpresaService(context, new ContextoFijo()).AsignarUsuariosAsync(1, new[] { "admin1" });

        Assert.True(resultado.Ok, resultado.Mensaje);
        Assert.Equal(new[] { "admin1", "portal1" }, (await context.UsuariosEmpresas.Where(u => u.EmpresaId == 1).Select(u => u.UsuarioId).ToListAsync()).Order());
    }
}
