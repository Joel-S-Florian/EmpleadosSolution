using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Empleados.Tests;

public class ArchivoTssTests
{
    private static TrabajadorTss Trabajador(decimal regalia = 0) => new(
        "1", 'C', "00112345678", "José Ángel", "Pérez", "Núñez", 'M', new DateTime(1990, 5, 15),
        45000, 45000, 5000, ArchivoTss.TipoNormal, regalia);

    [Fact]
    public void Detalle_TieneElAnchoFijoDelInstructivo()
    {
        var linea = ArchivoTss.Detalle(Trabajador());

        Assert.Equal(ArchivoTss.LongitudDetalle, linea.Length);
        Assert.StartsWith("D001C00112345678", linea);
        Assert.Contains("JOSE ANGEL", linea);
        Assert.Contains("PEREZ", linea);
        Assert.Contains("NUNEZ", linea);
        Assert.Contains("M15051990" + "0000000045000.00" + "0000000000000.00" + "0000000000000.00" + "0000000005000.00", linea); // salario ISR igual al de la TSS: va en cero
        Assert.EndsWith(ArchivoTss.TipoNormal, linea);
    }

    [Fact]
    public void Detalle_ConRegalia_AgregaLosExentosAlFinal()
    {
        var linea = ArchivoTss.Detalle(Trabajador(regalia: 45000));

        Assert.Equal(ArchivoTss.LongitudConExentos, linea.Length);
        Assert.EndsWith("01" + "0000000045000.00" + "02" + "0000000000000.00" + "03" + "0000000000000.00", linea);
    }

    [Fact]
    public void Archivo_TieneEncabezadoYSumario()
    {
        var (nombre, contenido) = ArchivoTss.Generar("1-01-00001-1", 2026, 10, new[] { Trabajador(), Trabajador() });
        var lineas = contenido.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("AM_101000011_102026.txt", nombre);
        Assert.Equal("EAM  101000011102026", lineas[0]);
        Assert.Equal("S000004", lineas[^1]);
        Assert.Equal(4, lineas.Length);
    }

    [Fact]
    public void Monto_DieciseisPosiciones() => Assert.Equal("0000000001234.50", ArchivoTss.Monto(1234.5m));
}

public class ArchivosTests
{
    private static (EmpleadosDbContext Context, ArchivosService Archivos, CicloNominaService Ciclo) Crear()
    {
        var options = new DbContextOptionsBuilder<EmpleadosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using (var semilla = new EmpleadosDbContext(options))
        {
            semilla.Database.EnsureCreated(); // bancos y parámetros
            semilla.Empresas.Add(new Empresa { Id = 1, Rnc = "101000001", RazonSocial = "Uno SRL", TasaRiesgoSrl = 1.20m });
            semilla.Departamentos.Add(new Departamento { Id = 1, EmpresaId = 1, Nombre = "TI" });
            semilla.Empleados.AddRange(
                Empleado(1, "00100000001", bancoId: 1, cuenta: "1234567890"),
                Empleado(2, "00100000002", bancoId: null, cuenta: null));
            semilla.SaveChanges();
        }
        var contexto = new ContextoFijo();
        var context = new EmpleadosDbContext(options, contexto);
        var parametros = new ParametrosLegalesService(context);
        return (context, new ArchivosService(context, contexto), new CicloNominaService(context, new NominaService(context, parametros), contexto));
    }

    private static Empleado Empleado(int id, string documento, int? bancoId, string? cuenta) => new()
    {
        Id = id, EmpresaId = 1, DepartamentoId = 1, Nombre = "Nombre" + id, Apellido = "Pérez Gómez", CorreoElectronico = $"e{id}@test.com",
        Telefono = "1", Direccion = "d", Salario = 45000, FechaNacimiento = new DateTime(1990, 1, 1), Sexo = Sexo.Femenino,
        FechaContratacion = new DateTime(2020, 1, 1), FrecuenciaPago = FrecuenciaPago.Mensual, NumeroDocumento = documento, Nss = "N" + id,
        BancoId = bancoId, NumeroCuenta = cuenta, TipoCuenta = cuenta == null ? null : TipoCuenta.Ahorro
    };

    private static async Task<int> OctubreAprobada(CicloNominaService ciclo)
    {
        var nomina = await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = 10, Frecuencia = FrecuenciaPago.Mensual });
        Assert.True((await ciclo.AprobarAsync(nomina.Valor!.Id)).Ok);
        return nomina.Valor.Id;
    }

    [Fact]
    public async Task Autodeterminacion_SaleDeLasNominasAprobadas()
    {
        var (_, archivos, ciclo) = Crear();
        await OctubreAprobada(ciclo);

        var reporte = await archivos.AutodeterminacionAsync(2026, 10);

        Assert.True(reporte.Ok, reporte.Mensaje);
        var r = reporte.Valor!;
        Assert.Equal(2, r.Filas.Count);
        Assert.Equal(90000, r.Totales["Salario cotizable"]);
        Assert.Equal(2 * 1291.50m, r.Totales["AFP empleado"]); // 2.87 % de 45,000
        Assert.Equal("AM_101000001_102026.txt", r.NombreArchivo);
        var detalles = r.Contenido!.Split("\r\n").Where(l => l.StartsWith('D')).ToList();
        Assert.Equal(2, detalles.Count);
        Assert.All(detalles, d => Assert.Equal(ArchivoTss.LongitudDetalle, d.Length));
        Assert.Contains("PEREZ" + new string(' ', 35) + "GOMEZ", detalles[0]);
    }

    [Fact]
    public async Task Autodeterminacion_SinNominaAprobada_NoGeneraArchivo()
    {
        var (_, archivos, ciclo) = Crear();
        await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = 10, Frecuencia = FrecuenciaPago.Mensual });

        var reporte = await archivos.AutodeterminacionAsync(2026, 10);

        Assert.False(reporte.Ok);
    }

    [Fact]
    public async Task Dispersion_IncluyeSoloQuienTieneCuenta()
    {
        var (_, archivos, ciclo) = Crear();
        var nominaId = await OctubreAprobada(ciclo);

        var archivo = await archivos.DispersionAsync(nominaId, bancoId: 1);

        Assert.True(archivo.Ok, archivo.Mensaje);
        var csv = Encoding.UTF8.GetString(archivo.Valor!.Contenido);
        var filas = csv.TrimStart('﻿').Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, filas.Length); // encabezado + un pago
        Assert.Contains("1234567890", filas[1]);
        Assert.Contains(archivo.Valor.Avisos, a => a.Contains("sin cuenta bancaria"));
    }

    [Fact]
    public async Task Dispersion_NominaSinAprobar_NoSeGenera()
    {
        var (_, archivos, ciclo) = Crear();
        var nomina = await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = 10, Frecuencia = FrecuenciaPago.Mensual });

        Assert.False((await archivos.DispersionAsync(nomina.Valor!.Id, null)).Ok);
    }
}
