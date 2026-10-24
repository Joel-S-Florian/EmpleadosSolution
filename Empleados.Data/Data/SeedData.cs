using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Data.Data;

public static class SeedData
{
    public static async Task InitializeAsync(EmpleadosDbContext context)
    {
        // Borrar datos existentes respetando FKs
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Empleados");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Puestos");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Notificaciones");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Departamentos");
        // Reset identities
        try
        {
            await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Departamentos', RESEED, 0)");
            await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Puestos', RESEED, 0)");
            await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Empleados', RESEED, 0)");
            await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Notificaciones', RESEED, 0)");
        }
        catch { }

        // Departamentos
        var departamentos = new List<Departamento>
        {
            new() { Nombre = "Tecnología", Descripcion = "Desarrollo, QA, DevOps" },
            new() { Nombre = "Recursos Humanos", Descripcion = "Reclutamiento y gestión de personal" },
            new() { Nombre = "Marketing", Descripcion = "Publicidad y comunicación" },
            new() { Nombre = "Finanzas", Descripcion = "Contabilidad y finanzas" }
        };
        context.Departamentos.AddRange(departamentos);
        await context.SaveChangesAsync();

        var ti = await context.Departamentos.FirstAsync(d => d.Nombre == "Tecnología");
        var rrhh = await context.Departamentos.FirstAsync(d => d.Nombre == "Recursos Humanos");
        var mkt = await context.Departamentos.FirstAsync(d => d.Nombre == "Marketing");
        var fin = await context.Departamentos.FirstAsync(d => d.Nombre == "Finanzas");

        // Puestos vinculados a departamentos
        var puestos = new List<Puesto>
        {
            new() { Nombre = "Desarrollador Junior", Descripcion = "Junior Dev", DepartamentoId = ti.Id },
            new() { Nombre = "Desarrollador Senior", Descripcion = "Senior Dev", DepartamentoId = ti.Id },
            new() { Nombre = "QA Tester", Descripcion = "Aseguramiento de calidad", DepartamentoId = ti.Id },
            new() { Nombre = "Reclutador", Descripcion = "Reclutamiento", DepartamentoId = rrhh.Id },
            new() { Nombre = "Especialista RRHH", Descripcion = "Gestión de personal", DepartamentoId = rrhh.Id },
            new() { Nombre = "Diseñador Gráfico", Descripcion = "Diseño", DepartamentoId = mkt.Id },
            new() { Nombre = "Community Manager", Descripcion = "Redes sociales", DepartamentoId = mkt.Id },
            new() { Nombre = "Contador", Descripcion = "Contabilidad", DepartamentoId = fin.Id },
            new() { Nombre = "Analista Financiero", Descripcion = "Análisis financiero", DepartamentoId = fin.Id }
        };
        context.Puestos.AddRange(puestos);
        await context.SaveChangesAsync();

        // Resolver puestos para asignar
        var devJr = await context.Puestos.FirstAsync(p => p.Nombre == "Desarrollador Junior");
        var devSr = await context.Puestos.FirstAsync(p => p.Nombre == "Desarrollador Senior");
        var qa = await context.Puestos.FirstAsync(p => p.Nombre == "QA Tester");
        var reclutador = await context.Puestos.FirstAsync(p => p.Nombre == "Reclutador");
        var dis = await context.Puestos.FirstAsync(p => p.Nombre == "Diseñador Gráfico");
        var cm = await context.Puestos.FirstAsync(p => p.Nombre == "Community Manager");
        var cont = await context.Puestos.FirstAsync(p => p.Nombre == "Contador");

        var empleados = new List<Empleado>
        {
            new() { Nombre = "Ana", Apellido = "García", FechaNacimiento = new DateTime(1992,5,15), Direccion = "Calle Principal 123", Telefono = "600111222", CorreoElectronico = "ana.garcia@empresa.com", PuestoId = devJr.Id, Salario = 35000, FechaContratacion = new DateTime(2021,3,1), DepartamentoId = ti.Id, Estado = EstadoEmpleado.Activo },
            new() { Nombre = "Luis", Apellido = "Pérez", FechaNacimiento = new DateTime(1988,8,20), Direccion = "Av. Tecnológica 456", Telefono = "600333444", CorreoElectronico = "luis.perez@empresa.com", PuestoId = devSr.Id, Salario = 55000, FechaContratacion = new DateTime(2019,6,15), DepartamentoId = ti.Id, Estado = EstadoEmpleado.Activo },
            new() { Nombre = "María", Apellido = "López", FechaNacimiento = new DateTime(1995,3,10), Direccion = "Calle QA 789", Telefono = "600555666", CorreoElectronico = "maria.lopez@empresa.com", PuestoId = qa.Id, Salario = 40000, FechaContratacion = new DateTime(2022,1,10), DepartamentoId = ti.Id, Estado = EstadoEmpleado.Activo },
            new() { Nombre = "Carlos", Apellido = "Ruiz", FechaNacimiento = new DateTime(1990,11,5), Direccion = "Calle RRHH 10", Telefono = "600777888", CorreoElectronico = "carlos.ruiz@empresa.com", PuestoId = reclutador.Id, Salario = 38000, FechaContratacion = new DateTime(2020,4,20), DepartamentoId = rrhh.Id, Estado = EstadoEmpleado.Activo },
            new() { Nombre = "Sofía", Apellido = "Martínez", FechaNacimiento = new DateTime(1993,7,22), Direccion = "Av. Marketing 20", Telefono = "600999000", CorreoElectronico = "sofia.martinez@empresa.com", PuestoId = dis.Id, Salario = 37000, FechaContratacion = new DateTime(2021,9,1), DepartamentoId = mkt.Id, Estado = EstadoEmpleado.Activo },
            new() { Nombre = "Javier", Apellido = "Torres", FechaNacimiento = new DateTime(1985,2,14), Direccion = "Calle Social 30", Telefono = "600123456", CorreoElectronico = "javier.torres@empresa.com", PuestoId = cm.Id, Salario = 36000, FechaContratacion = new DateTime(2020,11,11), DepartamentoId = mkt.Id, Estado = EstadoEmpleado.Inactivo },
            new() { Nombre = "Elena", Apellido = "Díaz", FechaNacimiento = new DateTime(1991,12,1), Direccion = "Av. Finanzas 100", Telefono = "600654321", CorreoElectronico = "elena.diaz@empresa.com", PuestoId = cont.Id, Salario = 48000, FechaContratacion = new DateTime(2018,5,5), DepartamentoId = fin.Id, Estado = EstadoEmpleado.Activo },
        };
        context.Empleados.AddRange(empleados);
        await context.SaveChangesAsync();

        // Notificaciones de ejemplo (para probar campana)
        var userIds = await context.Users.Select(u => u.Id).ToListAsync();
        foreach (var uid in userIds)
        {
            context.Notificaciones.Add(new Notificacion
            {
                Mensaje = "¡Bienvenido! Sistema de notificaciones activo",
                Tipo = TipoNotificacion.Info,
                Leida = false,
                FechaCreacion = DateTime.Now,
                UsuarioDestinoId = uid
            });
        }
        // Si no hay usuarios (antes de que Identity cree admin), crear una global
        if (!userIds.Any())
        {
            context.Notificaciones.Add(new Notificacion
            {
                Mensaje = "¡Bienvenido! Sistema de notificaciones activo (global)",
                Tipo = TipoNotificacion.Info,
                Leida = false,
                FechaCreacion = DateTime.Now,
                UsuarioDestinoId = null
            });
        }
        await context.SaveChangesAsync();
    }
}
