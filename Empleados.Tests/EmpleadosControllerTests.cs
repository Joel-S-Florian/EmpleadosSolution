using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Empleados.Tests;

public class EmpleadosControllerTests
{
    [Fact]
    public async Task GetEmpleados_ReturnsListOfEmpleados()
    {
        // Arrange: Configurar DbContext en memoria con datos de prueba
        var options = new DbContextOptionsBuilder<EmpleadosDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        using var context = new EmpleadosDbContext(options);

        // Agregar datos de prueba
        var departamento = new Departamento
        {
            Id = 1,
            Nombre = "Tecnología",
            Descripcion = "Departamento de sistemas"
        };
        context.Departamentos.Add(departamento);

        context.Empleados.AddRange(new List<Empleado>
        {
            new Empleado
            {
                Id = 1,
                Nombre = "Juan",
                Apellido = "Pérez",
                CorreoElectronico = "juan@test.com",
                Telefono = "600111222",
                Direccion = "Calle Falsa 123",
                Puesto = "Desarrollador",
                Salario = 40000,
                FechaNacimiento = new DateTime(1990, 1, 1),
                FechaContratacion = new DateTime(2020, 1, 1),
                Estado = EstadoEmpleado.Activo,
                DepartamentoId = 1
            },
            new Empleado
            {
                Id = 2,
                Nombre = "Ana",
                Apellido = "García",
                CorreoElectronico = "ana@test.com",
                Telefono = "600333444",
                Direccion = "Calle Verdadera 456",
                Puesto = "Analista",
                Salario = 45000,
                FechaNacimiento = new DateTime(1992, 5, 15),
                FechaContratacion = new DateTime(2019, 6, 1),
                Estado = EstadoEmpleado.Activo,
                DepartamentoId = 1
            }
        });

        await context.SaveChangesAsync();

        // Usar el nombre completo para especificar el controlador de la API
        var controller = new Empleados.Api.Controllers.EmpleadosController(Servicios.Empleados(context));

        // Act: Llamar al método GetEmpleados
        var result = await controller.GetEmpleados();

        // Assert: Verificar que el resultado sea Ok y tenga 2 empleados
        var okResult = Assert.IsType<OkObjectResult>(result);

        // El resultado es un objeto anónimo con items, totalCount, etc.
        var response = okResult.Value;
        Assert.NotNull(response);
        var itemsProperty = response.GetType().GetProperty("items");
        var items = itemsProperty?.GetValue(response) as List<EmpleadoDto>;

        Assert.NotNull(items);
        Assert.Equal(2, items.Count);
    }
}