using Empleados.Data.Data;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Data.Services;

public class ParametrosLegalesService
{
    private readonly EmpleadosDbContext _context;

    public ParametrosLegalesService(EmpleadosDbContext context)
    {
        _context = context;
    }

    // Valor vigente en la fecha: la última vigencia que empezó en o antes de ella
    public Task<decimal?> ValorAsync(string clave, DateTime fecha) =>
        _context.ParametrosLegales
            .Where(p => p.Clave == clave && p.VigenteDesde <= fecha.Date)
            .OrderByDescending(p => p.VigenteDesde)
            .Select(p => (decimal?)p.Valor)
            .FirstOrDefaultAsync();

    // Un parámetro por clave con el valor vigente en la fecha
    public async Task<List<ParametroLegal>> VigentesAsync(DateTime fecha)
    {
        var todos = await _context.ParametrosLegales
            .Where(p => p.VigenteDesde <= fecha.Date)
            .ToListAsync();
        return todos
            .GroupBy(p => p.Clave)
            .Select(g => g.OrderByDescending(p => p.VigenteDesde).First())
            .OrderBy(p => p.Clave)
            .ToList();
    }

    public Task<List<ParametroLegal>> HistorialAsync() =>
        _context.ParametrosLegales.OrderBy(p => p.Clave).ThenByDescending(p => p.VigenteDesde).ToListAsync();

    public async Task<List<TramoIsr>> EscalaIsrAsync(DateTime fecha)
    {
        var vigencia = await _context.TramosIsr
            .Where(t => t.VigenteDesde <= fecha.Date)
            .MaxAsync(t => (DateTime?)t.VigenteDesde);
        if (vigencia == null)
            return new List<TramoIsr>();

        return await _context.TramosIsr
            .Where(t => t.VigenteDesde == vigencia)
            .OrderBy(t => t.Desde)
            .ToListAsync();
    }

    public Task<decimal?> SalarioMinimoAsync(TamanoEmpresa tamano, DateTime fecha) =>
        _context.SalariosMinimos
            .Where(s => s.Tamano == tamano && s.VigenteDesde <= fecha.Date)
            .OrderByDescending(s => s.VigenteDesde)
            .Select(s => (decimal?)s.Monto)
            .FirstOrDefaultAsync();

    public Task<List<SalarioMinimo>> SalariosMinimosAsync() =>
        _context.SalariosMinimos.OrderByDescending(s => s.VigenteDesde).ThenByDescending(s => s.Monto).ToListAsync();

    public Task<List<Feriado>> FeriadosAsync(int anio) =>
        _context.Feriados.Where(f => f.Fecha.Year == anio).OrderBy(f => f.Fecha).ToListAsync();

    public async Task<Resultado> AgregarVigenciaAsync(string clave, decimal valor, DateTime vigenteDesde, string? fuente)
    {
        if (!ClavesParametro.Descripciones.ContainsKey(clave))
            return Resultado.Invalido("Parámetro desconocido", nameof(ParametroLegal.Clave));
        if (valor < 0)
            return Resultado.Invalido("El valor no puede ser negativo", nameof(ParametroLegal.Valor));
        if (await _context.ParametrosLegales.AnyAsync(p => p.Clave == clave && p.VigenteDesde == vigenteDesde.Date))
            return Resultado.Invalido("Ya existe una vigencia de este parámetro en esa fecha", nameof(ParametroLegal.VigenteDesde));

        _context.ParametrosLegales.Add(new ParametroLegal
        {
            Clave = clave,
            Valor = valor,
            VigenteDesde = vigenteDesde.Date,
            Fuente = string.IsNullOrWhiteSpace(fuente) ? null : fuente.Trim()
        });
        await _context.SaveChangesAsync();
        return Resultado.Exito("Nueva vigencia registrada");
    }

    public async Task<Resultado> AgregarSalarioMinimoAsync(TamanoEmpresa tamano, decimal monto, DateTime vigenteDesde)
    {
        if (monto <= 0)
            return Resultado.Invalido("El monto debe ser mayor que cero", nameof(SalarioMinimo.Monto));
        if (await _context.SalariosMinimos.AnyAsync(s => s.Tamano == tamano && s.VigenteDesde == vigenteDesde.Date))
            return Resultado.Invalido("Ya existe un salario mínimo para ese tamaño en esa fecha", nameof(SalarioMinimo.VigenteDesde));

        _context.SalariosMinimos.Add(new SalarioMinimo { Tamano = tamano, Monto = monto, VigenteDesde = vigenteDesde.Date });
        await _context.SaveChangesAsync();
        return Resultado.Exito("Salario mínimo registrado");
    }

    public async Task<Resultado> AgregarFeriadoAsync(DateTime fecha, string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return Resultado.Invalido("El nombre es obligatorio", nameof(Feriado.Nombre));
        if (await _context.Feriados.AnyAsync(f => f.Fecha == fecha.Date))
            return Resultado.Invalido("Ya existe un feriado en esa fecha", nameof(Feriado.Fecha));

        _context.Feriados.Add(new Feriado { Fecha = fecha.Date, Nombre = nombre.Trim() });
        await _context.SaveChangesAsync();
        return Resultado.Exito("Feriado registrado");
    }

    // Agrega los feriados del año según la Ley 139-97 que aún no estén registrados en su fecha
    public async Task<Resultado> GenerarFeriadosAsync(int anio)
    {
        var existentes = await _context.Feriados.Where(f => f.Fecha.Year == anio).Select(f => f.Fecha).ToListAsync();
        var nuevos = Empleados.Nomina.CalendarioLaboral.Feriados(anio).Where(f => !existentes.Contains(f.Fecha)).ToList();
        _context.Feriados.AddRange(nuevos.Select(f => new Feriado { Fecha = f.Fecha, Nombre = f.Nombre }));
        await _context.SaveChangesAsync();
        return Resultado.Exito(nuevos.Count == 0
            ? $"Los feriados de {anio} ya estaban registrados"
            : $"Se agregaron {nuevos.Count} feriados de {anio}; revise si el Ministerio de Trabajo publicó algún cambio");
    }

    public async Task<Resultado> EliminarFeriadoAsync(int id)
    {
        var feriado = await _context.Feriados.FindAsync(id);
        if (feriado == null)
            return Resultado.NoEncontrado("Feriado no encontrado");

        _context.Feriados.Remove(feriado);
        await _context.SaveChangesAsync();
        return Resultado.Exito("Feriado eliminado");
    }
}
