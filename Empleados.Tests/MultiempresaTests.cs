using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Tests;

// Un usuario de la empresa 1 no puede ver ni tocar datos de la empresa 2
public class MultiempresaTests
{
    private const int DeptoEmpresa1 = 10, DeptoEmpresa2 = 20, EmpleadoEmpresa2 = 200;

    private static DbContextOptions<EmpleadosDbContext> CrearBase()
    {
        var options = new DbContextOptionsBuilder<EmpleadosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var sinFiltro = new EmpleadosDbContext(options);
        sinFiltro.Empresas.AddRange(
            new Empresa { Id = 1, Rnc = "101000001", RazonSocial = "Uno SRL" },
            new Empresa { Id = 2, Rnc = "101000002", RazonSocial = "Dos SRL" });
        sinFiltro.Departamentos.AddRange(
            new Departamento { Id = DeptoEmpresa1, EmpresaId = 1, Nombre = "TI" },
            new Departamento { Id = DeptoEmpresa2, EmpresaId = 2, Nombre = "Ventas" });
        sinFiltro.Empleados.AddRange(
            Empleado(100, 1, DeptoEmpresa1, "uno@test.com"),
            Empleado(EmpleadoEmpresa2, 2, DeptoEmpresa2, "dos@test.com"));
        sinFiltro.UsuariosEmpresas.AddRange(
            new UsuarioEmpresa { UsuarioId = "user1", EmpresaId = 1 },
            new UsuarioEmpresa { UsuarioId = "otro", EmpresaId = 2 });
        sinFiltro.SaveChanges();
        return options;
    }

    private static Empleado Empleado(int id, int empresaId, int departamentoId, string correo) => new()
    {
        Id = id, EmpresaId = empresaId, DepartamentoId = departamentoId, Nombre = "N", Apellido = "A",
        CorreoElectronico = correo, Telefono = "1", Direccion = "d", Salario = 40000,
        FechaNacimiento = new DateTime(1990, 1, 1), FechaContratacion = new DateTime(2020, 1, 1)
    };

    private static ActualizarEmpleadoDto Dto(int departamentoId, string correo = "nuevo@test.com") => new()
    {
        Nombre = "Luis", Apellido = "Pérez", CorreoElectronico = correo, Telefono = "1", Direccion = "d",
        Salario = 40000, FechaNacimiento = new DateTime(1991, 1, 1), FechaContratacion = new DateTime(2021, 1, 1),
        DepartamentoId = departamentoId
    };

    [Fact]
    public async Task Listados_SoloMuestranLaEmpresaSeleccionada()
    {
        var options = CrearBase();
        var contexto = new ContextoFijo(empresaId: 1);
        using var context = new EmpleadosDbContext(options, contexto);

        var empleados = await Servicios.Empleados(context, contexto).ListarAsync(null, null, null);
        var departamentos = await Servicios.Departamentos(context, contexto).ListarAsync();
        var reporte = await Servicios.Reportes(context).PorDepartamentoAsync();

        Assert.Equal(100, Assert.Single(empleados.Items).Id);
        Assert.Equal(DeptoEmpresa1, Assert.Single(departamentos).Id);
        Assert.Equal("TI", Assert.Single(reporte).Nombre);
    }

    [Fact]
    public async Task EmpleadoDeOtraEmpresa_NoSeEncuentraNiSeModifica()
    {
        var options = CrearBase();
        var contexto = new ContextoFijo(empresaId: 1);
        using var context = new EmpleadosDbContext(options, contexto);
        var servicio = Servicios.Empleados(context, contexto);

        Assert.Null(await servicio.ObtenerAsync(EmpleadoEmpresa2));
        Assert.Equal(TipoResultado.NoEncontrado, (await servicio.ActualizarAsync(EmpleadoEmpresa2, Dto(DeptoEmpresa1))).Tipo);
        Assert.Equal(TipoResultado.NoEncontrado, (await servicio.DespedirAsync(EmpleadoEmpresa2)).Tipo);
    }

    [Fact]
    public async Task CrearEmpleado_EnDepartamentoDeOtraEmpresa_NoSePermite()
    {
        var options = CrearBase();
        var contexto = new ContextoFijo(empresaId: 1);
        using var context = new EmpleadosDbContext(options, contexto);

        var resultado = await Servicios.Empleados(context, contexto).CrearAsync(Dto(DeptoEmpresa2));

        Assert.Equal(nameof(CrearEmpleadoDto.DepartamentoId), resultado.Campo);
    }

    [Fact]
    public async Task CrearEmpleado_Toma_LaEmpresaDelDepartamento_YAvisaSoloASusUsuarios()
    {
        var options = CrearBase();
        var contexto = new ContextoFijo(empresaId: 1);
        using (var context = new EmpleadosDbContext(options, contexto))
        {
            var resultado = await Servicios.Empleados(context, contexto).CrearAsync(Dto(DeptoEmpresa1, "dos@test.com"));
            // El mismo correo existe en la empresa 2, pero el correo es único solo dentro de cada empresa
            Assert.True(resultado.Ok, resultado.Mensaje);
            Assert.Equal(1, resultado.Valor!.EmpresaId);
        }

        using var sinFiltro = new EmpleadosDbContext(options);
        var destinos = await sinFiltro.Notificaciones.Select(n => n.UsuarioDestinoId).ToListAsync();
        Assert.Equal("user1", Assert.Single(destinos));
    }

    [Fact]
    public async Task CrearDepartamento_UsaLaEmpresaSeleccionada_YElNombreEsUnicoPorEmpresa()
    {
        var options = CrearBase();
        var contexto = new ContextoFijo(empresaId: 2);
        using var context = new EmpleadosDbContext(options, contexto);

        // "TI" existe en la empresa 1, pero no en la 2
        var resultado = await Servicios.Departamentos(context, contexto).CrearAsync(new CrearDepartamentoDto { Nombre = "TI" });

        Assert.True(resultado.Ok, resultado.Mensaje);
        Assert.Equal(2, resultado.Valor!.EmpresaId);
    }

    [Fact]
    public async Task SinEmpresaSeleccionada_NoVeDatos_NiPuedeCrearDepartamentos()
    {
        var options = CrearBase();
        var contexto = new ContextoFijo(empresaId: null);
        using var context = new EmpleadosDbContext(options, contexto);

        Assert.Empty(await context.Empleados.ToListAsync());
        Assert.Empty(await context.Departamentos.ToListAsync());
        var resultado = await Servicios.Departamentos(context, contexto).CrearAsync(new CrearDepartamentoDto { Nombre = "Nuevo" });
        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
    }
}
