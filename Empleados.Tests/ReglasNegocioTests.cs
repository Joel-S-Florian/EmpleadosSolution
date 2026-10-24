using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Tests;

// Reglas que antes solo cumplía la API o el MVC (o ninguno) y ahora viven en los servicios
public class ReglasNegocioTests
{
    private static EmpleadosDbContext CrearContexto()
    {
        var context = new EmpleadosDbContext(new DbContextOptionsBuilder<EmpleadosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        context.Departamentos.Add(new Departamento { Id = 1, Nombre = "TI" });
        context.Empleados.Add(new Empleado
        {
            Id = 1, Nombre = "Ana", Apellido = "García", CorreoElectronico = "ana@test.com",
            Telefono = "600111222", Direccion = "Calle 1", Puesto = "Dev", Salario = 40000,
            FechaNacimiento = new DateTime(1990, 1, 1), FechaContratacion = new DateTime(2020, 1, 1),
            DepartamentoId = 1, Estado = EstadoEmpleado.Despedido
        });
        context.SaveChanges();
        return context;
    }

    private static ActualizarEmpleadoDto Dto(string correo = "nuevo@test.com", int departamentoId = 1) => new()
    {
        Nombre = "Luis", Apellido = "Pérez", CorreoElectronico = correo, Telefono = "600333444",
        Direccion = "Calle 2", Puesto = "Dev", Salario = 45000,
        FechaNacimiento = new DateTime(1991, 1, 1), FechaContratacion = new DateTime(2021, 1, 1),
        DepartamentoId = departamentoId
    };

    [Fact]
    public async Task Actualizar_EmpleadoDespedido_NoSePermite()
    {
        using var context = CrearContexto();

        var resultado = await Servicios.Empleados(context).ActualizarAsync(1, Dto());

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        Assert.Equal("Ana", (await context.Empleados.FindAsync(1))!.Nombre);
    }

    [Fact]
    public async Task Crear_CorreoDuplicado_ErrorEnCampoCorreo()
    {
        using var context = CrearContexto();

        var resultado = await Servicios.Empleados(context).CrearAsync(Dto(correo: "ana@test.com"), null);

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        Assert.Equal(nameof(CrearEmpleadoDto.CorreoElectronico), resultado.Campo);
    }

    [Fact]
    public async Task Crear_DepartamentoInexistente_ErrorEnCampoDepartamento()
    {
        using var context = CrearContexto();

        var resultado = await Servicios.Empleados(context).CrearAsync(Dto(departamentoId: 99), null);

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        Assert.Equal(nameof(CrearEmpleadoDto.DepartamentoId), resultado.Campo);
    }

    [Fact]
    public async Task CrearDepartamento_NombreDuplicado_NoSePermite()
    {
        using var context = CrearContexto();

        var resultado = await new DepartamentoService(context).CrearAsync(new CrearDepartamentoDto { Nombre = "TI" });

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
        Assert.Equal(1, await context.Departamentos.CountAsync());
    }

    [Fact]
    public async Task SubirFoto_GuardaEnCarpetaCompartida()
    {
        using var context = CrearContexto();
        (await context.Empleados.FindAsync(1))!.Estado = EstadoEmpleado.Activo;
        await context.SaveChangesAsync();
        var carpeta = Path.Combine(Path.GetTempPath(), "empleados-tests-" + Guid.NewGuid());
        var servicio = new EmpleadoService(context, new NotificacionService(context), new AlmacenFotos(carpeta));

        using var contenido = new MemoryStream(new byte[] { 1, 2, 3 });
        var resultado = await servicio.SubirFotoAsync(1, contenido, "foto.PNG", contenido.Length);

        Assert.True(resultado.Ok);
        Assert.StartsWith("/uploads/empleados/1_", resultado.Valor);
        Assert.True(File.Exists(Path.Combine(carpeta, "empleados", Path.GetFileName(resultado.Valor!))));
        Directory.Delete(carpeta, recursive: true);
    }

    [Theory]
    [InlineData("foto.exe", 3, TipoResultado.Invalido)]
    [InlineData("foto.png", 0, TipoResultado.Invalido)]
    [InlineData("foto.png", EmpleadoService.TamanoMaximoFoto + 1, TipoResultado.Invalido)]
    public async Task SubirFoto_ArchivoInvalido_SeRechaza(string nombre, long tamano, TipoResultado esperado)
    {
        using var context = CrearContexto();
        (await context.Empleados.FindAsync(1))!.Estado = EstadoEmpleado.Activo;
        await context.SaveChangesAsync();

        using var contenido = new MemoryStream(new byte[] { 1, 2, 3 });
        var resultado = await Servicios.Empleados(context).SubirFotoAsync(1, contenido, nombre, tamano);

        Assert.Equal(esperado, resultado.Tipo);
    }

    [Fact]
    public async Task SubirFoto_EmpleadoDespedido_NoSePermite()
    {
        using var context = CrearContexto();

        using var contenido = new MemoryStream(new byte[] { 1, 2, 3 });
        var resultado = await Servicios.Empleados(context).SubirFotoAsync(1, contenido, "foto.png", 3);

        Assert.Equal(TipoResultado.Invalido, resultado.Tipo);
    }
}
