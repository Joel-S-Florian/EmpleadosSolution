using Empleados.Data.Data;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;
using PagosEspeciales = Empleados.Nomina.PagosEspeciales;
using ParteBonificacion = Empleados.Nomina.ParteBonificacion;
using ParticipanteBonificacion = Empleados.Nomina.ParticipanteBonificacion;

namespace Empleados.Data.Services;

public record ParteBonificacionEmpleado(int EmpleadoId, string Nombre, FrecuenciaPago Frecuencia, ParteBonificacion Parte, decimal Infotep);

public record RepartoBonificacion(int AnioFiscal, DateTime Inicio, DateTime Cierre, decimal UtilidadNeta, decimal ARepartir, List<ParteBonificacionEmpleado> Partes)
{
    public decimal TotalBonificacion => Partes.Sum(p => p.Parte.Monto);
}

// Participación en los beneficios (art. 223): se reparte y se carga como novedad en una nómina regular,
// para que el ISR se calcule con el resto de los ingresos del mes
public class BonificacionService
{
    public const string ConceptoBonificacion = "Bonificación art. 223";
    public const string ConceptoInfotep = "INFOTEP sobre la bonificación";

    private readonly EmpleadosDbContext _context;
    private readonly ParametrosLegalesService _parametros;
    private readonly CicloNominaService _ciclo;
    private readonly IContextoUsuario _contexto;

    public BonificacionService(EmpleadosDbContext context, ParametrosLegalesService parametros, CicloNominaService ciclo, IContextoUsuario contexto)
    {
        _context = context;
        _parametros = parametros;
        _ciclo = ciclo;
        _contexto = contexto;
    }

    public async Task<Resultado<RepartoBonificacion>> CalcularAsync(int anioFiscal, decimal utilidadNeta)
    {
        if (_contexto.EmpresaId is not int empresaId)
            return Fallo("Seleccione una empresa");
        if (utilidadNeta <= 0)
            return Fallo("La utilidad neta debe ser mayor que 0: sin utilidades no hay bonificación");

        var empresa = await _context.Empresas.FirstAsync(e => e.Id == empresaId);
        var cierre = new DateTime(anioFiscal, empresa.MesCierreFiscal, DateTime.DaysInMonth(anioFiscal, empresa.MesCierreFiscal));
        var inicio = cierre.AddDays(1).AddYears(-1);
        // Parámetros vigentes al pagarla: a más tardar 120 días después del cierre
        var pago = cierre.AddDays(120);
        var divisor = await _parametros.ValorAsync(ClavesParametro.DivisorSalarioDiario, pago);
        var jornada = await _parametros.ValorAsync(ClavesParametro.HorasJornadaDiaria, pago);
        var infotep = await _parametros.ValorAsync(ClavesParametro.InfotepBonificacion, pago);
        if (divisor is not > 0 || jornada is not > 0 || infotep == null)
            return Fallo("Faltan parámetros legales (divisor del salario diario, jornada o INFOTEP)");

        // shortcut: los despedidos antes del pago no participan; si la ley les reconoce la proporción, va en su liquidación (Fase 3.5)
        var empleados = await _context.Empleados
            .Where(e => e.EmpresaId == empresaId && e.Estado != EstadoEmpleado.Despedido && e.FechaContratacion <= cierre)
            .OrderBy(e => e.Nombre).ThenBy(e => e.Apellido)
            .ToListAsync();
        if (empleados.Count == 0)
            return Fallo("No hay empleados que participen en ese ejercicio");

        // Por hora: salario mensual equivalente (tarifa × jornada × divisor)
        var partes = PagosEspeciales.Bonificacion(utilidadNeta, inicio, cierre,
            empleados.Select(e => new ParticipanteBonificacion(e.Id,
                e.TipoSalario == TipoSalario.PorHora ? e.Salario * jornada.Value * divisor.Value : e.Salario, e.FechaContratacion)).ToList(),
            divisor.Value).ToDictionary(p => p.Id);

        var reparto = new RepartoBonificacion(anioFiscal, inicio, cierre, utilidadNeta,
            Math.Round(utilidadNeta * PagosEspeciales.PorcentajeUtilidades / 100, 2, MidpointRounding.AwayFromZero),
            empleados.Select(e => new ParteBonificacionEmpleado(e.Id, $"{e.Nombre} {e.Apellido}", e.FrecuenciaPago, partes[e.Id],
                Math.Round(partes[e.Id].Monto * infotep.Value / 100, 2, MidpointRounding.AwayFromZero))).ToList());
        return Resultado<RepartoBonificacion>.Con(reparto);
    }

    // Reemplaza la bonificación del ejercicio en los recibos de la nómina y la recalcula
    public async Task<Resultado> AplicarAsync(int anioFiscal, decimal utilidadNeta, int nominaId)
    {
        var calculo = await CalcularAsync(anioFiscal, utilidadNeta);
        if (!calculo.Ok)
            return calculo;
        var nomina = await _ciclo.ObtenerAsync(nominaId);
        if (nomina == null)
            return Resultado.NoEncontrado("Nómina no encontrada");
        if (nomina.Tipo != TipoNomina.Regular || !nomina.Editable)
            return Resultado.Invalido("Elija una nómina regular que todavía no esté aprobada");

        var concepto = $"{ConceptoBonificacion} ({anioFiscal})";
        var yaAplicada = (await _context.RecibosPago
                .Where(r => r.NominaId != nominaId && r.Novedades.Any(n => n.Concepto == concepto))
                .Select(r => r.EmpleadoId)
                .ToListAsync())
            .ToHashSet();

        var aplicados = 0;
        var omitidos = new List<string>();
        foreach (var parte in calculo.Valor!.Partes.Where(p => p.Parte.Monto > 0))
        {
            var recibo = nomina.Recibos.FirstOrDefault(r => r.EmpleadoId == parte.EmpleadoId);
            if (recibo == null)
            {
                omitidos.Add(parte.Frecuencia == nomina.Frecuencia ? $"{parte.Nombre} (no está en esta nómina)" : $"{parte.Nombre} (pago {parte.Frecuencia.ToString().ToLowerInvariant()})");
                continue;
            }
            if (yaAplicada.Contains(parte.EmpleadoId))
            {
                omitidos.Add($"{parte.Nombre} (ya la tiene en otra nómina)");
                continue;
            }

            foreach (var anterior in recibo.Novedades.Where(n => n.Concepto == concepto || n.Concepto == $"{ConceptoInfotep} ({anioFiscal})").ToList())
                recibo.Novedades.Remove(anterior);
            recibo.Novedades.Add(new NovedadNomina { Tipo = TipoNovedad.Ingreso, Concepto = concepto, Monto = parte.Parte.Monto, CotizaTss = false, GravaIsr = true });
            // shortcut: la retención del 0.5 % va como deducción del recibo; el archivo del INFOTEP se genera en la Fase 3.6
            if (parte.Infotep > 0)
                recibo.Novedades.Add(new NovedadNomina { Tipo = TipoNovedad.Deduccion, Concepto = $"{ConceptoInfotep} ({anioFiscal})", Monto = parte.Infotep });
            aplicados++;
        }
        if (aplicados == 0)
            return Resultado.Invalido("Ningún empleado de esta nómina recibe bonificación" + (omitidos.Count > 0 ? ": " + string.Join("; ", omitidos) : ""));

        await _context.SaveChangesAsync();
        var recalculo = await _ciclo.CalcularAsync(nominaId);
        if (!recalculo.Ok)
            return recalculo;
        return Resultado.Exito($"Bonificación aplicada a {aplicados} empleado(s) en la nómina {nomina.Descripcion}" +
                               (omitidos.Count > 0 ? $". Sin aplicar: {string.Join("; ", omitidos)}" : ""));
    }

    private static Resultado<RepartoBonificacion> Fallo(string mensaje) =>
        Resultado<RepartoBonificacion>.Fallo(Resultado.Invalido(mensaje));
}
