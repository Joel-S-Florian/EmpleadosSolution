using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Empleados.Tests;

public class NotificacionesTests
{
    private static DbContextOptions<EmpleadosDbContext> CreateOptions(string dbName)
        => new DbContextOptionsBuilder<EmpleadosDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

    private static void SetUser(ControllerBase controller, string userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) }
        };
    }

    [Fact]
    public async Task GetMisNotificaciones_FiltraPorUsuario()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        using var context = new EmpleadosDbContext(options);
        context.Notificaciones.AddRange(
            new Notificacion { Id = 1, Mensaje = "Hola User1", Tipo = TipoNotificacion.Info, UsuarioDestinoId = "user1", FechaCreacion = DateTime.Now },
            new Notificacion { Id = 2, Mensaje = "Global", Tipo = TipoNotificacion.Success, UsuarioDestinoId = null, FechaCreacion = DateTime.Now },
            new Notificacion { Id = 3, Mensaje = "Hola User2", Tipo = TipoNotificacion.Warning, UsuarioDestinoId = "user2", FechaCreacion = DateTime.Now }
        );
        await context.SaveChangesAsync();

        var controller = new Empleados.Api.Controllers.NotificacionesController(Servicios.Notificaciones(context));
        SetUser(controller, "user1");

        var result = await controller.GetMisNotificaciones() as OkObjectResult;
        var list = Assert.IsAssignableFrom<List<NotificacionDto>>(result!.Value);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, n => n.Id == 1);
        Assert.Contains(list, n => n.Id == 2);
        Assert.DoesNotContain(list, n => n.Id == 3);
    }

    [Fact]
    public async Task MarcarLeida_CambiaEstado()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        using var context = new EmpleadosDbContext(options);
        context.Notificaciones.Add(new Notificacion { Id = 10, Mensaje = "Test", Tipo = TipoNotificacion.Info, UsuarioDestinoId = "user1", Leida = false, FechaCreacion = DateTime.Now });
        await context.SaveChangesAsync();

        var controller = new Empleados.Api.Controllers.NotificacionesController(Servicios.Notificaciones(context));
        SetUser(controller, "user1");

        var result = await controller.MarcarLeida(10) as OkObjectResult;
        Assert.NotNull(result);

        var notif = await context.Notificaciones.FindAsync(10);
        Assert.True(notif!.Leida);
    }

    [Fact]
    public async Task CrearEmpleado_GeneraNotificacion()
    {
        var options = CreateOptions(Guid.NewGuid().ToString());
        using var context = new EmpleadosDbContext(options);
        context.Departamentos.Add(new Departamento { Id = 1, Nombre = "TI" });
        // Crear usuario destino para notificaciones
        var user = new Microsoft.AspNetCore.Identity.IdentityUser { Id = "user1", UserName = "test@test.com", Email = "test@test.com" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var empController = new Empleados.Api.Controllers.EmpleadosController(Servicios.Empleados(context));
        SetUser(empController, "user1");

        var dto = new CrearEmpleadoDto
        {
            Nombre = "Carlos",
            Apellido = "Ruiz",
            FechaNacimiento = new DateTime(1990, 1, 1),
            Direccion = "Calle 1",
            Telefono = "600111222",
            CorreoElectronico = "carlos@test.com",
            Salario = 40000,
            FechaContratacion = DateTime.Now,
            DepartamentoId = 1
        };

        var result = await empController.CrearEmpleado(dto);
        Assert.IsType<CreatedAtActionResult>(result);

        var notifs = await context.Notificaciones.Where(n => n.UsuarioDestinoId == "user1").ToListAsync();
        Assert.Single(notifs);
        Assert.Contains("Nuevo empleado registrado: Carlos Ruiz", notifs[0].Mensaje);
        Assert.Equal(TipoNotificacion.Success, notifs[0].Tipo);
        Assert.False(notifs[0].Leida);
    }
}
