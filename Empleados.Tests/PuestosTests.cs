using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Tests;

public class PuestosTests
{
    private static DbContextOptions<EmpleadosDbContext> CreateOptions(string dbName)
        => new DbContextOptionsBuilder<EmpleadosDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

    [Fact]
    public async Task CrearPuesto_DepartamentoNoExiste_RetornaBadRequest()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        using var context = new EmpleadosDbContext(options);

        var controller = new Empleados.Api.Controllers.PuestosController(Servicios.Puestos(context));
        var dto = new CrearPuestoDto { Nombre = "Tester", Descripcion = "QA", DepartamentoId = 999 };

        var result = await controller.CrearPuesto(dto);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
    }

    [Fact]
    public async Task CrearPuesto_ValidaDepartamentoYDuplicado()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        using var context = new EmpleadosDbContext(options);
        context.Departamentos.Add(new Departamento { Id = 1, Nombre = "TI" });
        await context.SaveChangesAsync();

        var controller = new Empleados.Api.Controllers.PuestosController(Servicios.Puestos(context));

        var dto1 = new CrearPuestoDto { Nombre = "Dev", Descripcion = "Dev desc", DepartamentoId = 1 };
        var result1 = await controller.CrearPuesto(dto1);
        Assert.IsType<CreatedAtActionResult>(result1);

        // duplicado mismo depto debe fallar
        var dtoDup = new CrearPuestoDto { Nombre = "Dev", Descripcion = "Otro", DepartamentoId = 1 };
        var resultDup = await controller.CrearPuesto(dtoDup);
        Assert.IsType<BadRequestObjectResult>(resultDup);
    }

    [Fact]
    public async Task GetPuestos_FiltroPorDepartamentoId_RetornaSoloFiltrados()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        using var context = new EmpleadosDbContext(options);
        context.Departamentos.AddRange(
            new Departamento { Id = 1, Nombre = "TI" },
            new Departamento { Id = 2, Nombre = "RRHH" });
        context.Puestos.AddRange(
            new Puesto { Id = 1, Nombre = "Dev", DepartamentoId = 1 },
            new Puesto { Id = 2, Nombre = "QA", DepartamentoId = 1 },
            new Puesto { Id = 3, Nombre = "Reclutador", DepartamentoId = 2 });
        await context.SaveChangesAsync();

        var controller = new Empleados.Api.Controllers.PuestosController(Servicios.Puestos(context));

        var resultAll = await controller.GetPuestos(null) as OkObjectResult;
        var all = Assert.IsAssignableFrom<List<PuestoDto>>(resultAll!.Value);
        Assert.Equal(3, all.Count);

        var resultFiltrado = await controller.GetPuestos(1) as OkObjectResult;
        var filtrado = Assert.IsAssignableFrom<List<PuestoDto>>(resultFiltrado!.Value);
        Assert.Equal(2, filtrado.Count);
        Assert.All(filtrado, p => Assert.Equal(1, p.DepartamentoId));
    }

    [Fact]
    public async Task CrearEmpleado_PuestoNoPerteneceADepartamento_RetornaBadRequest()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        using var context = new EmpleadosDbContext(options);
        context.Departamentos.AddRange(
            new Departamento { Id = 1, Nombre = "TI" },
            new Departamento { Id = 2, Nombre = "RRHH" });
        context.Puestos.Add(new Puesto { Id = 10, Nombre = "Dev", DepartamentoId = 1 });
        await context.SaveChangesAsync();

        var controller = new Empleados.Api.Controllers.EmpleadosController(Servicios.Empleados(context))
        {
            ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() }
        };
        var dto = new CrearEmpleadoDto
        {
            Nombre = "Juan",
            Apellido = "Perez",
            FechaNacimiento = new DateTime(1990, 1, 1),
            Direccion = "Calle 1",
            Telefono = "600111222",
            CorreoElectronico = "juan2@test.com",
            Puesto = "Dev",
            Salario = 40000,
            FechaContratacion = new DateTime(2020, 1, 1),
            DepartamentoId = 2, // RRHH
            PuestoId = 10 // pertenece a TI
        };

        var result = await controller.CrearEmpleado(dto);
        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(bad.Value);
    }
}
