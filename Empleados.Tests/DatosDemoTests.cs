using Empleados.Data.Data;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Tests;

public class DatosDemoTests
{
    [Fact]
    public async Task DatosDeDemostracion_SeGeneranConElMotorReal_YNoSeDuplican()
    {
        var options = new DbContextOptionsBuilder<EmpleadosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new EmpleadosDbContext(options);
        db.Database.EnsureCreated(); // parámetros legales, feriados y bancos

        var resultado = await DatosDemo.CrearAsync(db, new[] { "admin1" });
        var otraVez = await DatosDemo.CrearAsync(db, new[] { "admin1" });

        Assert.StartsWith("Distribuidora del Caribe SRL: 34 empleados, 30 nóminas", resultado);
        Assert.Contains("ya existen", otraVez);
        Assert.Equal(27, await db.Nominas.CountAsync(n => n.Estado == EstadoNomina.Cerrada));
        Assert.Equal(2, await db.Nominas.CountAsync(n => n.Estado == EstadoNomina.Calculada));
        Assert.Equal(2, await db.Liquidaciones.CountAsync(l => l.Estado == EstadoLiquidacion.Pagada));
        Assert.Equal(2, await db.Empleados.CountAsync(e => e.Estado == EstadoEmpleado.Despedido));
        Assert.True(await db.RecibosPago.AnyAsync(r => r.Novedades.Any(n => n.Concepto.StartsWith("Bonificación art. 223"))));
        Assert.True(await db.Prestamos.AnyAsync(p => p.Saldo < p.MontoTotal));
        Assert.Contains(await db.UsuariosEmpresas.ToListAsync(), u => u.UsuarioId == "admin1");
    }
}
