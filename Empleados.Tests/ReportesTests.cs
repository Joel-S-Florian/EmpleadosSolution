using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Tests;

public class ReportesTests
{
    private static DbContextOptions<EmpleadosDbContext> CreateOptions(string dbName)
        => new DbContextOptionsBuilder<EmpleadosDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

    [Fact]
    public async Task GetReporteDepartamentos_ReturnsCorrectAggregations()
    {
        // Arrange
        var options = CreateOptions(Guid.NewGuid().ToString());
        using var context = new EmpleadosDbContext(options);

        var dep1 = new Departamento { Id = 1, Nombre = "Tecnologia", Descripcion = "IT" };
        var dep2 = new Departamento { Id = 2, Nombre = "RRHH", Descripcion = "Recursos" };
        context.Departamentos.AddRange(dep1, dep2);

        context.Empleados.AddRange(
            new Empleado
            {
                Id = 1, Nombre = "Juan", Apellido = "Perez", CorreoElectronico = "juan@rpt.com",
                Telefono = "600111222", Direccion = "Calle 1", Puesto = "Dev", Salario = 40000,
                FechaNacimiento = new DateTime(1990, 1, 1), FechaContratacion = new DateTime(2020, 1, 1),
                Estado = EstadoEmpleado.Activo, DepartamentoId = 1
            },
            new Empleado
            {
                Id = 2, Nombre = "Ana", Apellido = "Garcia", CorreoElectronico = "ana@rpt.com",
                Telefono = "600333444", Direccion = "Calle 2", Puesto = "Analista", Salario = 50000,
                FechaNacimiento = new DateTime(1992, 5, 15), FechaContratacion = new DateTime(2019, 6, 1),
                Estado = EstadoEmpleado.Activo, DepartamentoId = 1
            },
            new Empleado
            {
                Id = 3, Nombre = "Luis", Apellido = "Gomez", CorreoElectronico = "luis@rpt.com",
                Telefono = "600555666", Direccion = "Calle 3", Puesto = "Soporte", Salario = 30000,
                FechaNacimiento = new DateTime(1995, 3, 10), FechaContratacion = new DateTime(2021, 1, 1),
                Estado = EstadoEmpleado.Inactivo, DepartamentoId = 1
            },
            new Empleado
            {
                Id = 4, Nombre = "Maria", Apellido = "Lopez", CorreoElectronico = "maria@rpt.com",
                Telefono = "600777888", Direccion = "Calle 4", Puesto = "Reclutadora", Salario = 35000,
                FechaNacimiento = new DateTime(1993, 7, 20), FechaContratacion = new DateTime(2022, 2, 1),
                Estado = EstadoEmpleado.Despedido, DepartamentoId = 2
            }
        );
        await context.SaveChangesAsync();

        var controller = new Empleados.Api.Controllers.ReportesController(context);

        // Act
        var result = await controller.GetReporteDepartamentos();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var reporte = Assert.IsAssignableFrom<List<ReporteDepartamentoDto>>(okResult.Value);

        var tech = reporte.First(r => r.Nombre == "Tecnologia");
        Assert.Equal(3, tech.TotalEmpleados);
        Assert.Equal(2, tech.Activos); // solo Activos
        Assert.Equal(90000, tech.NominaMensual); // 40000+50000, inactivo no cuenta

        var rrhh = reporte.First(r => r.Nombre == "RRHH");
        Assert.Equal(1, rrhh.TotalEmpleados);
        Assert.Equal(0, rrhh.Activos);
        Assert.Equal(0, rrhh.NominaMensual); // despedido no cuenta
    }

    [Fact]
    public async Task GetReporteDepartamentos_IncludesEmptyDepartment()
    {
        // Arrange: departamento sin empleados debe aparecer con 0
        var options = CreateOptions(Guid.NewGuid().ToString());
        using var context = new EmpleadosDbContext(options);

        context.Departamentos.Add(new Departamento { Id = 10, Nombre = "Vacio", Descripcion = "Sin gente" });
        await context.SaveChangesAsync();

        var controller = new Empleados.Api.Controllers.ReportesController(context);

        // Act
        var result = await controller.GetReporteDepartamentos();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var reporte = Assert.IsAssignableFrom<List<ReporteDepartamentoDto>>(okResult.Value);

        Assert.Single(reporte);
        var vacio = reporte[0];
        Assert.Equal("Vacio", vacio.Nombre);
        Assert.Equal(0, vacio.TotalEmpleados);
        Assert.Equal(0, vacio.Activos);
        Assert.Equal(0, vacio.NominaMensual);
    }
}
