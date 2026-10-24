using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using FrecuenciaPago = Empleados.Data.Entities.FrecuenciaPago;

namespace Empleados.Data.Services;

// Ciclo de una nómina: crear, calcular (las veces que haga falta), aprobar, pagar y cerrar
public class CicloNominaService
{
    private const string ReferenciaPrestamo = "PRESTAMO:";

    private readonly EmpleadosDbContext _context;
    private readonly NominaService _nomina;
    private readonly IContextoUsuario _contexto;

    public CicloNominaService(EmpleadosDbContext context, NominaService nomina, IContextoUsuario contexto)
    {
        _context = context;
        _nomina = nomina;
        _contexto = contexto;
    }

    public Task<List<NominaPeriodo>> ListarAsync() =>
        _context.Nominas
            .Include(n => n.Recibos)
            .OrderByDescending(n => n.Anio).ThenByDescending(n => n.Mes).ThenByDescending(n => n.Quincena)
            .ToListAsync();

    public Task<NominaPeriodo?> ObtenerAsync(int id) =>
        _context.Nominas
            .Include(n => n.Recibos).ThenInclude(r => r.Lineas)
            .Include(n => n.Recibos).ThenInclude(r => r.Novedades)
            .AsSplitQuery()
            .FirstOrDefaultAsync(n => n.Id == id);

    public Task<ReciboPago?> ObtenerReciboAsync(int reciboId) =>
        _context.RecibosPago
            .Include(r => r.Nomina)
            .Include(r => r.Lineas)
            .Include(r => r.Novedades)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == reciboId);

    // Neto de cada empleado en la nómina anterior de la misma frecuencia (para comparar)
    public async Task<Dictionary<int, decimal>> NetosAnterioresAsync(NominaPeriodo nomina)
    {
        var anterior = await _context.Nominas
            .Where(n => n.EmpresaId == nomina.EmpresaId && n.Frecuencia == nomina.Frecuencia &&
                        (n.Anio < nomina.Anio || (n.Anio == nomina.Anio && (n.Mes < nomina.Mes || (n.Mes == nomina.Mes && n.Quincena < nomina.Quincena)))))
            .OrderByDescending(n => n.Anio).ThenByDescending(n => n.Mes).ThenByDescending(n => n.Quincena)
            .Select(n => n.Id)
            .FirstOrDefaultAsync();
        return await _context.RecibosPago
            .Where(r => r.NominaId == anterior)
            .ToDictionaryAsync(r => r.EmpleadoId, r => r.Neto);
    }

    public async Task<Resultado<NominaPeriodo>> CrearAsync(CrearNominaDto dto)
    {
        if (_contexto.EmpresaId is not int empresaId)
            return Resultado<NominaPeriodo>.Fallo(Resultado.Invalido("Seleccione una empresa antes de crear la nómina"));

        var quincena = dto.Frecuencia == FrecuenciaPago.Mensual ? 0 : dto.Quincena;
        if (await _context.Nominas.AnyAsync(n => n.EmpresaId == empresaId && n.Anio == dto.Anio && n.Mes == dto.Mes && n.Frecuencia == dto.Frecuencia && n.Quincena == quincena))
            return Resultado<NominaPeriodo>.Fallo(Resultado.Invalido("Ya existe una nómina para ese período"));

        var periodo = NominaService.Periodo(dto.Frecuencia, dto.Anio, dto.Mes, Math.Max(quincena, 1));
        var nomina = new NominaPeriodo
        {
            EmpresaId = empresaId,
            Anio = dto.Anio,
            Mes = dto.Mes,
            Quincena = quincena,
            Frecuencia = dto.Frecuencia,
            FechaInicio = periodo.Inicio,
            FechaFin = periodo.Fin,
            FechaCreacion = DateTime.Now,
            CreadaPor = _contexto.UsuarioNombre
        };
        _context.Nominas.Add(nomina);
        await _context.SaveChangesAsync();

        var calculo = await CalcularAsync(nomina.Id);
        return calculo.Ok
            ? Resultado<NominaPeriodo>.Con(nomina, "Nómina creada y calculada")
            : Resultado<NominaPeriodo>.Con(nomina, $"Nómina creada, pero no se pudo calcular: {calculo.Mensaje}");
    }

    // Sincroniza los empleados del período y recalcula todos los recibos con los parámetros vigentes
    public async Task<Resultado> CalcularAsync(int id)
    {
        var nomina = await ObtenerAsync(id);
        if (nomina == null)
            return Resultado.NoEncontrado("Nómina no encontrada");
        if (!nomina.Editable)
            return Resultado.Invalido($"La nómina está {nomina.Estado.ToString().ToLowerInvariant()}: ya no se puede recalcular");

        var empresa = await _context.Empresas.FirstAsync(e => e.Id == nomina.EmpresaId);
        var (parametros, faltante) = await _nomina.ParametrosAsync(nomina.FechaFin, empresa.TasaRiesgoSrl);
        if (parametros == null)
            return Resultado.Invalido(faltante!);
        var periodo = NominaService.Periodo(nomina.Frecuencia, nomina.Anio, nomina.Mes, Math.Max(nomina.Quincena, 1));

        // 2.ª quincena: necesita lo acumulado en la 1.ª, ya aprobada
        Dictionary<int, AcumuladoMes>? acumulados = null;
        string? avisoSinPrimera = null;
        if (nomina.Frecuencia == FrecuenciaPago.Quincenal && nomina.Quincena == 2)
        {
            var primera = await _context.Nominas.Include(n => n.Recibos)
                .FirstOrDefaultAsync(n => n.EmpresaId == nomina.EmpresaId && n.Anio == nomina.Anio && n.Mes == nomina.Mes &&
                                          n.Frecuencia == FrecuenciaPago.Quincenal && n.Quincena == 1);
            if (primera == null)
                avisoSinPrimera = "No hay 1.ª quincena registrada en el sistema: la TSS y el ISR del mes se calculan solo con esta quincena";
            else if (primera.Editable)
                return Resultado.Invalido("Apruebe la 1.ª quincena antes de calcular la 2.ª (se usa lo retenido en ella)");
            else
                acumulados = primera.Recibos.Where(r => r.AcumuladoJson != null)
                    .ToDictionary(r => r.EmpleadoId, r => JsonSerializer.Deserialize<AcumuladoMes>(r.AcumuladoJson!)!);
        }

        var empleados = await _context.Empleados
            .Include(e => e.Departamento).Include(e => e.PuestoNav).Include(e => e.Banco)
            .Where(e => e.EmpresaId == nomina.EmpresaId && e.FrecuenciaPago == nomina.Frecuencia &&
                        e.FechaContratacion <= nomina.FechaFin &&
                        (e.Estado == EstadoEmpleado.Activo ||
                         (e.Estado == EstadoEmpleado.Despedido && e.FechaTerminacion >= nomina.FechaInicio)))
            .ToDictionaryAsync(e => e.Id);

        // Quien ya no corresponde sale de la nómina (salvo que tenga novedades cargadas)
        foreach (var recibo in nomina.Recibos.Where(r => !empleados.ContainsKey(r.EmpleadoId) && r.Novedades.Count == 0).ToList())
            nomina.Recibos.Remove(recibo);
        foreach (var empleado in empleados.Values.Where(e => nomina.Recibos.All(r => r.EmpleadoId != e.Id)))
            nomina.Recibos.Add(new ReciboPago { EmpresaId = nomina.EmpresaId, EmpleadoId = empleado.Id, NombreEmpleado = $"{empleado.Nombre} {empleado.Apellido}" });

        var idsEmpleados = nomina.Recibos.Select(r => r.EmpleadoId).ToList();
        var prestamos = await _context.Prestamos
            .Where(p => idsEmpleados.Contains(p.EmpleadoId) && p.Activo && p.Saldo > 0 && p.FechaInicio <= nomina.FechaFin &&
                        (!p.SoloFinDeMes || periodo.CierraElMes))
            .ToListAsync();
        // El saldo baja al pagar: lo ya incluido en otras nóminas aún sin pagar no se puede volver a descontar
        var idsPrestamos = prestamos.Select(p => p.Id).ToList();
        var comprometido = await _context.RecibosPago
            .Where(r => r.NominaId != nomina.Id && (r.Nomina!.Estado == EstadoNomina.Calculada || r.Nomina.Estado == EstadoNomina.Aprobada))
            .SelectMany(r => r.Lineas)
            .Where(l => l.PrestamoId != null && idsPrestamos.Contains(l.PrestamoId.Value))
            .GroupBy(l => l.PrestamoId!.Value)
            .Select(g => new { PrestamoId = g.Key, Monto = g.Sum(l => l.Monto) })
            .ToDictionaryAsync(x => x.PrestamoId, x => x.Monto);
        var disponibles = prestamos
            .Select(p => (Prestamo: p, Disponible: p.Saldo - comprometido.GetValueOrDefault(p.Id)))
            .Where(x => x.Disponible > 0)
            .ToList();

        // Faltas, retardos y permisos sin goce aprobados que tocan el período
        var incidencias = await _context.Incidencias
            .Where(i => idsEmpleados.Contains(i.EmpleadoId) && i.Estado == EstadoIncidencia.Aprobada && i.Descontar &&
                        i.FechaInicio <= nomina.FechaFin && i.FechaFin >= nomina.FechaInicio)
            .ToListAsync();
        var desdeFeriados = incidencias.Select(i => i.FechaInicio).DefaultIfEmpty(nomina.FechaInicio).Min();
        var hastaFeriados = incidencias.Select(i => i.FechaFin).DefaultIfEmpty(nomina.FechaFin).Max();
        var feriados = await _context.Feriados
            .Where(f => f.Fecha >= desdeFeriados && f.Fecha <= hastaFeriados)
            .Select(f => f.Fecha).ToListAsync();

        foreach (var recibo in nomina.Recibos)
        {
            var delEmpleado = incidencias.Where(i => i.EmpleadoId == recibo.EmpleadoId).OrderBy(i => i.FechaInicio).ToList();
            recibo.DiasIncidencias = delEmpleado.Sum(i => IncidenciaService.DiasEnPeriodo(i, nomina.FechaInicio, nomina.FechaFin, feriados, parametros.HorasJornadaDiaria));
            var nota = delEmpleado.Count == 0 ? null : "Incidencias aprobadas: " + string.Join("; ", delEmpleado.Select(i =>
                $"{Texto(i.Tipo)} {i.FechaInicio:dd/MM}{(i.FechaFin > i.FechaInicio ? $"-{i.FechaFin:dd/MM}" : "")} " +
                (i.Tipo == TipoIncidencia.Retardo ? $"{i.Horas:0.##} h" : $"{i.Dias:0.##} días"))) + $" = {recibo.DiasIncidencias:0.####} días a descontar";
            Calcular(recibo, empleados.GetValueOrDefault(recibo.EmpleadoId), periodo, parametros,
                acumulados?.GetValueOrDefault(recibo.EmpleadoId), avisoSinPrimera, disponibles.Where(x => x.Prestamo.EmpleadoId == recibo.EmpleadoId), nota);
        }

        nomina.TotalIngresos = nomina.Recibos.Sum(r => r.TotalIngresos);
        nomina.TotalDeducciones = nomina.Recibos.Sum(r => r.TotalDeduccionesLey + r.TotalDeduccionesInternas);
        nomina.TotalNeto = nomina.Recibos.Sum(r => r.Neto);
        nomina.TotalAportesPatronales = nomina.Recibos.Sum(r => r.TotalAportesPatronales);
        nomina.Estado = EstadoNomina.Calculada;
        nomina.FechaCalculo = DateTime.Now;
        await _context.SaveChangesAsync();

        var conErrores = nomina.Recibos.Count(r => r.ErroresJson != null);
        return Resultado.Exito(conErrores == 0
            ? $"Nómina calculada: {nomina.Recibos.Count} recibos"
            : $"Nómina calculada: {nomina.Recibos.Count} recibos, {conErrores} con errores que impiden aprobarla");
    }

    private static void Calcular(ReciboPago recibo, Empleado? empleado, PeriodoNomina periodo, ParametrosNomina parametros,
        AcumuladoMes? acumulado, string? avisoSinPrimera, IEnumerable<(Prestamo Prestamo, decimal Disponible)> prestamos, string? notaIncidencias)
    {
        recibo.Lineas.Clear();
        recibo.TotalIngresos = recibo.TotalDeduccionesLey = recibo.TotalDeduccionesInternas = recibo.Neto = 0;
        recibo.TotalAportesPatronales = recibo.BaseTss = recibo.BaseIsrMensual = 0;
        recibo.AcumuladoJson = recibo.ExplicacionJson = recibo.AdvertenciasJson = recibo.ErroresJson = null;

        if (empleado == null)
        {
            recibo.ErroresJson = Json(new[] { "El empleado ya no corresponde a esta nómina; quite sus novedades para retirarlo" });
            return;
        }

        recibo.NombreEmpleado = $"{empleado.Nombre} {empleado.Apellido}";
        recibo.Documento = empleado.NumeroDocumento;
        recibo.Nss = empleado.Nss;
        recibo.Departamento = empleado.Departamento?.Nombre;
        recibo.Puesto = empleado.PuestoNav?.Nombre;
        recibo.Salario = empleado.Salario;
        recibo.TipoSalario = empleado.TipoSalario;
        recibo.Banco = empleado.Banco?.Nombre;
        recibo.NumeroCuenta = empleado.NumeroCuenta;

        var errores = NominaService.DatosFaltantes(empleado);
        var advertencias = new List<string>();
        if (avisoSinPrimera != null)
            advertencias.Add(avisoSinPrimera);

        var deducciones = recibo.Novedades
            .Where(n => n.Tipo == TipoNovedad.Deduccion)
            .Select(n => new DeduccionInterna(n.Concepto, n.Monto, n.Prioridad))
            .Concat(prestamos.Select(x => new DeduccionInterna(x.Prestamo.Concepto, Math.Min(x.Prestamo.MontoCuota, x.Disponible), x.Prestamo.Prioridad, ReferenciaPrestamo + x.Prestamo.Id)))
            .ToList();
        var entrada = NominaService.EntradaBase(empleado, periodo) with
        {
            HorasTrabajadas = recibo.HorasTrabajadas,
            DiasAusenciaSinPago = recibo.DiasAusenciaSinPago + recibo.DiasIncidencias,
            HorasExtra = recibo.HorasExtra,
            HorasExtraExceso = recibo.HorasExtraExceso,
            HorasNocturnas = recibo.HorasNocturnas,
            HorasFeriado = recibo.HorasFeriado,
            OtrosIngresos = recibo.Novedades.Where(n => n.Tipo == TipoNovedad.Ingreso)
                .Select(n => new IngresoAdicional(n.Concepto, n.Monto, n.CotizaTss, n.GravaIsr)).ToList(),
            Deducciones = deducciones,
            Acumulado = acumulado ?? AcumuladoMes.Vacio
        };

        try
        {
            var resultado = MotorNomina.Calcular(entrada, parametros);
            var orden = 0;
            foreach (var l in resultado.Lineas)
                recibo.Lineas.Add(new LineaReciboPago
                {
                    Orden = ++orden,
                    Tipo = l.Tipo.ToString(),
                    Codigo = l.Codigo,
                    Concepto = l.Concepto,
                    Monto = l.Monto,
                    Cantidad = l.Cantidad,
                    CotizaTss = l.CotizaTss,
                    GravaIsr = l.GravaIsr,
                    PrestamoId = l.Referencia?.StartsWith(ReferenciaPrestamo) == true ? int.Parse(l.Referencia[ReferenciaPrestamo.Length..]) : null
                });
            recibo.TotalIngresos = resultado.TotalIngresos;
            recibo.TotalDeduccionesLey = resultado.TotalDeduccionesLey;
            recibo.TotalDeduccionesInternas = resultado.TotalDeduccionesInternas;
            recibo.Neto = resultado.Neto;
            recibo.TotalAportesPatronales = resultado.TotalAportesPatronales;
            recibo.BaseTss = resultado.BaseTssPeriodo;
            recibo.BaseIsrMensual = resultado.BaseIsrMensual;
            recibo.AcumuladoJson = JsonSerializer.Serialize(resultado.AcumuladoAlCierre);
            recibo.ExplicacionJson = Json(notaIncidencias == null ? resultado.Explicacion : resultado.Explicacion.Prepend(notaIncidencias));
            advertencias.AddRange(resultado.Advertencias);
        }
        catch (ArgumentException ex)
        {
            errores.Add(ex.Message);
        }

        recibo.AdvertenciasJson = advertencias.Count > 0 ? Json(advertencias) : null;
        recibo.ErroresJson = errores.Count > 0 ? Json(errores) : null;
    }

    public async Task<Resultado> GuardarNovedadesAsync(int reciboId, NovedadesReciboDto dto)
    {
        var recibo = await ObtenerReciboAsync(reciboId);
        if (recibo == null)
            return Resultado.NoEncontrado("Recibo no encontrado");
        if (!recibo.Nomina!.Editable)
            return Resultado.Invalido("La nómina ya fue aprobada: reábrala para cambiar novedades");

        var (ingresos, deducciones, error) = NominaService.Convertir(dto.OtrosIngresos, dto.Deducciones);
        if (error != null)
            return Resultado.Invalido(error);

        recibo.HorasTrabajadas = dto.HorasTrabajadas;
        recibo.DiasAusenciaSinPago = dto.DiasAusenciaSinPago;
        recibo.HorasExtra = dto.HorasExtra;
        recibo.HorasExtraExceso = dto.HorasExtraExceso;
        recibo.HorasNocturnas = dto.HorasNocturnas;
        recibo.HorasFeriado = dto.HorasFeriado;
        recibo.Novedades.Clear();
        foreach (var i in ingresos)
            recibo.Novedades.Add(new NovedadNomina { Tipo = TipoNovedad.Ingreso, Concepto = i.Concepto, Monto = i.Monto, CotizaTss = i.CotizaTss, GravaIsr = i.GravaIsr });
        foreach (var d in deducciones)
            recibo.Novedades.Add(new NovedadNomina { Tipo = TipoNovedad.Deduccion, Concepto = d.Concepto, Monto = d.Monto, Prioridad = d.Prioridad });
        await _context.SaveChangesAsync();

        var calculo = await CalcularAsync(recibo.NominaId);
        return calculo.Ok ? Resultado.Exito("Novedades guardadas y nómina recalculada") : calculo;
    }

    public async Task<Resultado> AprobarAsync(int id)
    {
        var nomina = await ObtenerAsync(id);
        if (nomina == null)
            return Resultado.NoEncontrado("Nómina no encontrada");
        if (nomina.Estado != EstadoNomina.Calculada)
            return Resultado.Invalido("Solo se puede aprobar una nómina calculada");
        if (nomina.Recibos.Count == 0)
            return Resultado.Invalido("La nómina no tiene recibos");
        var conErrores = nomina.Recibos.Count(r => r.ErroresJson != null);
        if (conErrores > 0)
            return Resultado.Invalido($"Hay {conErrores} recibo(s) con errores; corríjalos y recalcule antes de aprobar");

        nomina.Estado = EstadoNomina.Aprobada;
        nomina.FechaAprobacion = DateTime.Now;
        nomina.AprobadaPor = _contexto.UsuarioNombre;
        await _context.SaveChangesAsync();
        return Resultado.Exito("Nómina aprobada");
    }

    public async Task<Resultado> ReabrirAsync(int id)
    {
        var nomina = await _context.Nominas.FirstOrDefaultAsync(n => n.Id == id);
        if (nomina == null)
            return Resultado.NoEncontrado("Nómina no encontrada");
        if (nomina.Estado != EstadoNomina.Aprobada)
            return Resultado.Invalido("Solo se puede reabrir una nómina aprobada que no se ha pagado");
        if (nomina.Frecuencia == FrecuenciaPago.Quincenal && nomina.Quincena == 1 &&
            await _context.Nominas.AnyAsync(n => n.EmpresaId == nomina.EmpresaId && n.Anio == nomina.Anio && n.Mes == nomina.Mes &&
                                                 n.Frecuencia == FrecuenciaPago.Quincenal && n.Quincena == 2))
            return Resultado.Invalido("La 2.ª quincena ya usa lo retenido en esta: elimínela antes de reabrir la 1.ª");

        nomina.Estado = EstadoNomina.Calculada;
        nomina.FechaAprobacion = null;
        nomina.AprobadaPor = null;
        await _context.SaveChangesAsync();
        return Resultado.Exito("Nómina reabierta");
    }

    // Marca el pago y descuenta de cada préstamo lo que se aplicó en los recibos
    public async Task<Resultado> PagarAsync(int id, DateTime fechaPago)
    {
        var nomina = await ObtenerAsync(id);
        if (nomina == null)
            return Resultado.NoEncontrado("Nómina no encontrada");
        if (nomina.Estado != EstadoNomina.Aprobada)
            return Resultado.Invalido("Solo se puede pagar una nómina aprobada");

        var aplicado = nomina.Recibos.SelectMany(r => r.Lineas)
            .Where(l => l.PrestamoId != null)
            .GroupBy(l => l.PrestamoId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Monto));
        var prestamos = await _context.Prestamos.Where(p => aplicado.Keys.Contains(p.Id)).ToListAsync();
        foreach (var prestamo in prestamos)
        {
            prestamo.Saldo = Math.Max(0, prestamo.Saldo - aplicado[prestamo.Id]);
            if (prestamo.Saldo == 0)
                prestamo.Activo = false;
        }

        nomina.Estado = EstadoNomina.Pagada;
        nomina.FechaPago = fechaPago.Date;
        nomina.PagadaPor = _contexto.UsuarioNombre;
        await _context.SaveChangesAsync();
        return Resultado.Exito(prestamos.Count == 0 ? "Nómina pagada" : $"Nómina pagada; se actualizó el saldo de {prestamos.Count} préstamo(s)");
    }

    public async Task<Resultado> CerrarAsync(int id)
    {
        var nomina = await _context.Nominas.FirstOrDefaultAsync(n => n.Id == id);
        if (nomina == null)
            return Resultado.NoEncontrado("Nómina no encontrada");
        if (nomina.Estado != EstadoNomina.Pagada)
            return Resultado.Invalido("Solo se puede cerrar una nómina pagada");

        nomina.Estado = EstadoNomina.Cerrada;
        nomina.FechaCierre = DateTime.Now;
        await _context.SaveChangesAsync();
        return Resultado.Exito("Nómina cerrada");
    }

    public async Task<Resultado> EliminarAsync(int id)
    {
        var nomina = await _context.Nominas.FirstOrDefaultAsync(n => n.Id == id);
        if (nomina == null)
            return Resultado.NoEncontrado("Nómina no encontrada");
        if (!nomina.Editable)
            return Resultado.Invalido("Solo se puede eliminar una nómina en borrador o calculada");

        _context.Nominas.Remove(nomina);
        await _context.SaveChangesAsync();
        return Resultado.Exito("Nómina eliminada");
    }

    public static IReadOnlyList<string> Lista(string? json) =>
        json == null ? Array.Empty<string>() : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

    // Acentos legibles en la base de datos (sin á)
    private static readonly JsonSerializerOptions OpcionesJson = new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    private static string Json(IEnumerable<string> textos) => JsonSerializer.Serialize(textos, OpcionesJson);

    private static string Texto(Enum valor) =>
        valor.GetType().GetField(valor.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? valor.ToString();
}
