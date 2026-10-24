using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using FrecuenciaPago = Empleados.Data.Entities.FrecuenciaPago;

namespace Empleados.Data.Services;

public record SimulacionNomina(
    int EmpleadoId,
    string Empleado,
    string Frecuencia,
    // Para la 2.ª quincena: la 1.ª calculada solo con el salario, para tener lo acumulado del mes
    ReciboNomina? PrimeraQuincena,
    ReciboNomina Recibo,
    IReadOnlyList<string> Advertencias);

// Une el motor de cálculo (Empleados.Nomina) con los datos del empleado y los parámetros legales vigentes
public class NominaService
{
    private readonly EmpleadosDbContext _context;
    private readonly ParametrosLegalesService _parametros;

    public NominaService(EmpleadosDbContext context, ParametrosLegalesService parametros)
    {
        _context = context;
        _parametros = parametros;
    }

    public async Task<Resultado<SimulacionNomina>> SimularAsync(SimulacionNominaDto dto)
    {
        var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id == dto.EmpleadoId);
        if (empleado == null)
            return Resultado<SimulacionNomina>.Fallo(Resultado.NoEncontrado("Empleado no encontrado"));
        var empresa = await _context.Empresas.FirstAsync(e => e.Id == empleado.EmpresaId);

        var periodo = empleado.FrecuenciaPago == FrecuenciaPago.Mensual
            ? PeriodoNomina.Mensual(dto.Anio, dto.Mes)
            : PeriodoNomina.Quincenal(dto.Anio, dto.Mes, dto.Quincena);

        var (parametros, faltante) = await ParametrosAsync(periodo.Fin, empresa.TasaRiesgoSrl);
        if (parametros == null)
            return Resultado<SimulacionNomina>.Fallo(Resultado.Invalido(faltante!));

        var ingresos = new List<IngresoAdicional>();
        foreach (var i in dto.OtrosIngresos.Where(i => !string.IsNullOrWhiteSpace(i.Concepto) || i.Monto != 0))
        {
            if (string.IsNullOrWhiteSpace(i.Concepto))
                return Resultado<SimulacionNomina>.Fallo(Resultado.Invalido("Indique el concepto de cada ingreso adicional"));
            ingresos.Add(new IngresoAdicional(i.Concepto.Trim(), i.Monto, i.CotizaTss, i.GravaIsr));
        }
        var deducciones = new List<DeduccionInterna>();
        foreach (var d in dto.Deducciones.Where(d => !string.IsNullOrWhiteSpace(d.Concepto) || d.Monto != 0))
        {
            if (string.IsNullOrWhiteSpace(d.Concepto))
                return Resultado<SimulacionNomina>.Fallo(Resultado.Invalido("Indique el concepto de cada deducción"));
            deducciones.Add(new DeduccionInterna(d.Concepto.Trim(), d.Monto, d.Prioridad));
        }

        var salarioBase = new EntradaNomina
        {
            Periodo = periodo,
            Modalidad = empleado.TipoSalario == TipoSalario.PorHora ? ModalidadSalario.PorHora : ModalidadSalario.FijoMensual,
            Salario = empleado.Salario,
            FechaIngreso = empleado.FechaContratacion,
            FechaSalida = empleado.Estado == EstadoEmpleado.Despedido ? empleado.FechaTerminacion : null,
            DependientesSfs = empleado.DependientesSfs,
            HorasTrabajadas = dto.HorasTrabajadas
        };
        var entrada = salarioBase with
        {
            DiasAusenciaSinPago = dto.DiasAusenciaSinPago,
            HorasExtra = dto.HorasExtra,
            HorasExtraExceso = dto.HorasExtraExceso,
            HorasNocturnas = dto.HorasNocturnas,
            HorasFeriado = dto.HorasFeriado,
            OtrosIngresos = ingresos,
            Deducciones = deducciones
        };

        try
        {
            ReciboNomina? primera = null;
            if (periodo.Frecuencia == Frecuencia.Quincenal && periodo.Quincena == 2)
            {
                primera = MotorNomina.Calcular(salarioBase with { Periodo = PeriodoNomina.Quincenal(dto.Anio, dto.Mes, 1) }, parametros);
                entrada = entrada with { Acumulado = primera.AcumuladoAlCierre };
            }
            var recibo = MotorNomina.Calcular(entrada, parametros);
            return Resultado<SimulacionNomina>.Con(new SimulacionNomina(
                empleado.Id,
                $"{empleado.Nombre} {empleado.Apellido}",
                empleado.FrecuenciaPago.ToString(),
                primera,
                recibo,
                await AdvertenciasAsync(empleado, empresa, periodo)));
        }
        catch (ArgumentException ex)
        {
            return Resultado<SimulacionNomina>.Fallo(Resultado.Invalido(ex.Message));
        }
    }

    // Parámetros vigentes al cierre del período; si falta uno obligatorio, devuelve cuál
    public async Task<(ParametrosNomina? Parametros, string? Faltante)> ParametrosAsync(DateTime fecha, decimal tasaSrl)
    {
        var vigentes = (await _parametros.VigentesAsync(fecha)).ToDictionary(p => p.Clave, p => p.Valor);
        var faltante = ClavesParametro.Descripciones.Keys
            .Except(ClavesParametro.Opcionales)
            .FirstOrDefault(k => !vigentes.ContainsKey(k));
        if (faltante != null)
            return (null, $"Falta el parámetro legal \"{ClavesParametro.Descripciones[faltante]}\" vigente al {fecha:dd/MM/yyyy}");

        var escala = await _parametros.EscalaIsrAsync(fecha);
        if (escala.Count == 0)
            return (null, $"No hay escala de ISR vigente al {fecha:dd/MM/yyyy}");

        decimal V(string clave) => vigentes[clave];
        return (new ParametrosNomina
        {
            AfpEmpleado = V(ClavesParametro.AfpEmpleado),
            AfpEmpleador = V(ClavesParametro.AfpEmpleador),
            SfsEmpleado = V(ClavesParametro.SfsEmpleado),
            SfsEmpleador = V(ClavesParametro.SfsEmpleador),
            SalarioMinimoCotizable = V(ClavesParametro.SalarioMinimoCotizable),
            TopeAfpSalarios = V(ClavesParametro.TopeAfpSalarios),
            TopeSfsSalarios = V(ClavesParametro.TopeSfsSalarios),
            TopeSrlSalarios = V(ClavesParametro.TopeSrlSalarios),
            InfotepEmpleador = V(ClavesParametro.InfotepEmpleador),
            DivisorSalarioDiario = V(ClavesParametro.DivisorSalarioDiario),
            HorasJornadaDiaria = V(ClavesParametro.HorasJornadaDiaria),
            RecargoHoraExtra = V(ClavesParametro.RecargoHoraExtra),
            RecargoHoraExtraExceso = V(ClavesParametro.RecargoHoraExtraExceso),
            RecargoNocturno = V(ClavesParametro.RecargoNocturno),
            RecargoFeriado = V(ClavesParametro.RecargoFeriado),
            CostoDependienteSfs = vigentes.TryGetValue(ClavesParametro.CostoDependienteSfs, out var costo) ? costo : null,
            TasaSrl = tasaSrl,
            EscalaIsr = escala.Select(t => new TramoEscala(t.Desde, t.Hasta, t.Tasa, t.CuotaFija)).ToList()
        }, null);
    }

    private async Task<List<string>> AdvertenciasAsync(Empleado empleado, Empresa empresa, PeriodoNomina periodo)
    {
        var advertencias = new List<string>();
        if (string.IsNullOrEmpty(empleado.NumeroDocumento))
            advertencias.Add("Falta la cédula o el pasaporte del empleado (obligatorio para la nómina real y la TSS)");
        if (string.IsNullOrEmpty(empleado.Nss))
            advertencias.Add("Falta el NSS del empleado (obligatorio para la TSS)");
        if (empleado.Estado == EstadoEmpleado.Inactivo)
            advertencias.Add("El empleado está inactivo");
        if (empleado.TipoSalario == TipoSalario.FijoMensual)
        {
            var minimo = await _parametros.SalarioMinimoAsync(empresa.Tamano, periodo.Fin);
            if (minimo.HasValue && empleado.Salario < minimo.Value)
                advertencias.Add($"El salario es menor que el mínimo vigente al {periodo.Fin:dd/MM/yyyy} (RD${minimo.Value.ToString("N2", CultureInfo.InvariantCulture)})");
        }
        return advertencias;
    }
}
