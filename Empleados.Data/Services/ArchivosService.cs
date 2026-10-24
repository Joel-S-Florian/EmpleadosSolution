using Empleados.Data.Data;
using Empleados.Data.Entities;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Empleados.Data.Services;

public record FilaTss(int EmpleadoId, string Nombre, TrabajadorTss? Trabajador, decimal Afp, decimal Sfs, decimal Isr, List<string> Avisos);

public record ReporteTss(int Anio, int Mes, List<FilaTss> Filas, Dictionary<string, decimal> Totales, List<string> Avisos, string? NombreArchivo, string? Contenido);

public record ArchivoDescarga(string Nombre, byte[] Contenido, List<string> Avisos);

// Archivos para la TSS, el banco y el Ministerio de Trabajo
public class ArchivosService
{
    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    private readonly EmpleadosDbContext _context;
    private readonly IContextoUsuario _contexto;

    public ArchivosService(EmpleadosDbContext context, IContextoUsuario contexto)
    {
        _context = context;
        _contexto = contexto;
    }

    // Autodeterminación del mes (AM) con lo de las nóminas aprobadas, más el resumen de retenciones y aportes
    public async Task<Resultado<ReporteTss>> AutodeterminacionAsync(int anio, int mes)
    {
        if (_contexto.EmpresaId is not int empresaId)
            return Resultado<ReporteTss>.Fallo(Resultado.Invalido("Seleccione una empresa"));
        var empresa = await _context.Empresas.FirstAsync(e => e.Id == empresaId);
        var desde = new DateTime(anio, mes, 1);
        var hasta = desde.AddMonths(1).AddDays(-1);
        var avisos = new List<string>();
        if (empresa.Rnc.All(c => c == '0' || !char.IsDigit(c)))
            avisos.Add($"El RNC de la empresa ({empresa.Rnc}) es de prueba: corríjalo en Empresas antes de enviar el archivo");

        var nominas = await _context.Nominas
            .Include(n => n.Recibos).ThenInclude(r => r.Lineas)
            .Where(n => n.EmpresaId == empresaId && n.Anio == anio && n.Mes == mes)
            .AsSplitQuery()
            .ToListAsync();
        var regulares = nominas.Where(n => n.Tipo == TipoNomina.Regular).ToList();
        foreach (var pendiente in regulares.Where(n => n.Editable))
            avisos.Add($"La nómina {pendiente.Descripcion} no está aprobada: no se incluye");
        var recibos = regulares.Where(n => !n.Editable).SelectMany(n => n.Recibos).ToList();
        if (recibos.Count == 0)
            return Resultado<ReporteTss>.Fallo(Resultado.Invalido($"No hay nóminas aprobadas de {mes:00}/{anio}"));

        // Exentos del ISR: regalía pagada en el mes (01) y preaviso, cesantía y asistencia de liquidaciones pagadas (02)
        var regalia = nominas.Where(n => n.Tipo == TipoNomina.SalarioNavidad && n.FechaPago >= desde && n.FechaPago <= hasta)
            .SelectMany(n => n.Recibos).GroupBy(r => r.EmpleadoId).ToDictionary(g => g.Key, g => g.Sum(r => r.Neto));
        var liquidaciones = await _context.Liquidaciones
            .Where(l => l.EmpresaId == empresaId && l.Estado == EstadoLiquidacion.Pagada && l.FechaPago >= desde && l.FechaPago <= hasta)
            .ToListAsync();
        var prestaciones = liquidaciones.GroupBy(l => l.EmpleadoId).ToDictionary(g => g.Key, g => g.Sum(l => LiquidacionService.Lineas(l.LineasJson)
            .Where(x => x.Codigo is Prestaciones.Codigos.Preaviso or Prestaciones.Codigos.Cesantia or Prestaciones.Codigos.Asistencia)
            .Sum(x => x.Monto)));

        var ids = recibos.Select(r => r.EmpleadoId).Distinct().ToList();
        var empleados = await _context.Empleados.Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id);
        var filas = new List<FilaTss>();
        foreach (var grupo in recibos.GroupBy(r => r.EmpleadoId))
        {
            var empleado = empleados[grupo.Key];
            var lineas = grupo.SelectMany(r => r.Lineas).ToList();
            decimal Suma(string codigo) => lineas.Where(l => l.Codigo == codigo).Sum(l => l.Monto);
            var ingresos = lineas.Where(l => l.Tipo == nameof(TipoLinea.Ingreso)).ToList();
            var avisosFila = new List<string>();

            var (tipo, documento) = Documento(empleado);
            if (tipo == null)
                avisosFila.Add("Sin cédula, NSS ni pasaporte: no va en el archivo");
            else if (tipo != 'C' && empleado.Sexo == null)
                avisosFila.Add("Sin cédula, la TSS pide sexo y fecha de nacimiento");

            var (primerApellido, segundoApellido) = Apellidos(empleado.Apellido);
            var trabajador = tipo == null ? null : new TrabajadorTss(
                "001", tipo.Value, documento!, empleado.Nombre, primerApellido, segundoApellido,
                empleado.Sexo switch { Sexo.Masculino => 'M', Sexo.Femenino => 'F', _ => null },
                empleado.FechaNacimiento,
                grupo.Sum(r => r.BaseTss),
                ingresos.Where(l => l.GravaIsr && l.CotizaTss).Sum(l => l.Monto),
                ingresos.Where(l => l.GravaIsr && !l.CotizaTss).Sum(l => l.Monto),
                empleado.FechaContratacion.Date > desde || empleado.FechaTerminacion?.Date < hasta ? ArchivoTss.TipoMesIncompleto : ArchivoTss.TipoNormal,
                regalia.GetValueOrDefault(empleado.Id),
                prestaciones.GetValueOrDefault(empleado.Id));
            filas.Add(new FilaTss(empleado.Id, $"{empleado.Nombre} {empleado.Apellido}", trabajador,
                Suma(Codigos.Afp), Suma(Codigos.Sfs) + Suma(Codigos.SfsDependientes), Suma(Codigos.Isr), avisosFila));
        }
        filas = filas.OrderBy(f => f.Nombre).ToList();

        var todas = recibos.SelectMany(r => r.Lineas).ToList();
        decimal Total(string codigo) => todas.Where(l => l.Codigo == codigo).Sum(l => l.Monto);
        var totales = new Dictionary<string, decimal>
        {
            ["Salario cotizable"] = recibos.Sum(r => r.BaseTss),
            ["AFP empleado"] = Total(Codigos.Afp),
            ["SFS empleado"] = Total(Codigos.Sfs) + Total(Codigos.SfsDependientes),
            ["AFP empleador"] = Total(Codigos.AfpPatronal),
            ["SFS empleador"] = Total(Codigos.SfsPatronal),
            ["Riesgos laborales"] = Total(Codigos.Srl),
            ["INFOTEP"] = Total(Codigos.Infotep),
            ["ISR retenido (IR-3)"] = Total(Codigos.Isr)
        };
        totales["Total a pagar a la TSS"] = totales["AFP empleado"] + totales["SFS empleado"] + totales["AFP empleador"] + totales["SFS empleador"] + totales["Riesgos laborales"];

        var enArchivo = filas.Where(f => f.Trabajador != null).Select(f => f.Trabajador!).ToList();
        var (nombre, contenido) = enArchivo.Count == 0 ? (null, null) : ArchivoTss.Generar(empresa.Rnc, anio, mes, enArchivo);
        return Resultado<ReporteTss>.Con(new ReporteTss(anio, mes, filas, totales, avisos, nombre, contenido));
    }

    // shortcut: CSV genérico (banco, cuenta, monto); el formato exacto de BHD o Banreservas se agrega con el layout que entrega el banco
    public async Task<Resultado<ArchivoDescarga>> DispersionAsync(int nominaId, int? bancoId)
    {
        var nomina = await _context.Nominas.Include(n => n.Recibos).ThenInclude(r => r.Empleado).ThenInclude(e => e!.Banco)
            .FirstOrDefaultAsync(n => n.Id == nominaId);
        if (nomina == null)
            return Resultado<ArchivoDescarga>.Fallo(Resultado.NoEncontrado("Nómina no encontrada"));
        if (nomina.Editable)
            return Resultado<ArchivoDescarga>.Fallo(Resultado.Invalido("Apruebe la nómina antes de generar el pago al banco"));

        var avisos = new List<string>();
        var csv = new StringBuilder("Banco,CodigoBanco,TipoCuenta,Cuenta,TipoDocumento,Documento,Nombre,Monto,Concepto\r\n");
        decimal total = 0;
        var cantidad = 0;
        foreach (var recibo in nomina.Recibos.Where(r => r.Neto > 0).OrderBy(r => r.NombreEmpleado))
        {
            var empleado = recibo.Empleado!;
            if (empleado.Banco == null || string.IsNullOrWhiteSpace(empleado.NumeroCuenta))
            {
                avisos.Add($"{recibo.NombreEmpleado}: sin cuenta bancaria ({recibo.Neto.ToString("N2", CultureInfo.GetCultureInfo("es-DO"))} por otro medio)");
                continue;
            }
            if (bancoId != null && empleado.BancoId != bancoId)
                continue;
            csv.AppendJoin(',', Csv(empleado.Banco.Nombre), Csv(empleado.Banco.Codigo), Csv(empleado.TipoCuenta?.ToString()), Csv(empleado.NumeroCuenta),
                    empleado.TipoDocumento == TipoDocumento.Cedula ? "C" : "P", Csv(empleado.NumeroDocumento), Csv(recibo.NombreEmpleado),
                    recibo.Neto.ToString("0.00", Invariante), Csv($"Nomina {nomina.Descripcion}"))
                .Append("\r\n");
            total += recibo.Neto;
            cantidad++;
        }
        if (cantidad == 0)
            return Resultado<ArchivoDescarga>.Fallo(Resultado.Invalido("Ningún recibo tiene cuenta en ese banco"));
        avisos.Insert(0, $"{cantidad} pago(s) por {total.ToString("N2", CultureInfo.GetCultureInfo("es-DO"))}");
        return Resultado<ArchivoDescarga>.Con(new ArchivoDescarga($"pago-nomina-{nomina.Id}{(bancoId != null ? $"-banco-{bancoId}" : "")}.csv", Utf8ConBom(csv), avisos));
    }

    // Base para la planilla de personal del Ministerio de Trabajo (DGT-3)
    public async Task<ArchivoDescarga> PlanillaPersonalAsync()
    {
        var empleados = await _context.Empleados.Include(e => e.Departamento).Include(e => e.PuestoNav)
            .Where(e => e.Estado != EstadoEmpleado.Despedido)
            .OrderBy(e => e.Apellido).ThenBy(e => e.Nombre)
            .ToListAsync();
        var csv = new StringBuilder("TipoDocumento,Documento,NSS,Nombres,Apellidos,Sexo,FechaNacimiento,FechaIngreso,Departamento,Puesto,TipoContrato,TipoSalario,Salario\r\n");
        foreach (var e in empleados)
            csv.AppendJoin(',', e.TipoDocumento, Csv(e.NumeroDocumento), Csv(e.Nss), Csv(e.Nombre), Csv(e.Apellido), e.Sexo?.ToString() ?? "",
                    e.FechaNacimiento.ToString("yyyy-MM-dd"), e.FechaContratacion.ToString("yyyy-MM-dd"), Csv(e.Departamento?.Nombre), Csv(e.PuestoNav?.Nombre),
                    e.TipoContrato, e.TipoSalario, e.Salario.ToString("0.00", Invariante))
                .Append("\r\n");
        var avisos = empleados.Where(e => e.NumeroDocumento == null).Select(e => $"{e.Nombre} {e.Apellido}: sin documento").ToList();
        return new ArchivoDescarga($"planilla-personal-{DateTime.Today:yyyyMMdd}.csv", Utf8ConBom(csv), avisos);
    }

    private static (char? Tipo, string? Numero) Documento(Empleado e)
    {
        var numero = e.NumeroDocumento == null ? null : new string(e.NumeroDocumento.Where(char.IsLetterOrDigit).ToArray());
        if (e.TipoDocumento == TipoDocumento.Cedula && !string.IsNullOrEmpty(numero))
            return ('C', numero);
        if (!string.IsNullOrWhiteSpace(e.Nss))
            return ('N', e.Nss.Trim());
        return string.IsNullOrEmpty(numero) ? (null, null) : ('P', numero);
    }

    // "Pérez Gómez" -> ("Pérez", "Gómez"); los apellidos compuestos quedan en el segundo
    private static (string, string) Apellidos(string apellido)
    {
        var partes = apellido.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch { 0 => ("", ""), 1 => (partes[0], ""), _ => (partes[0], partes[1]) };
    }

    private static string Csv(string? valor) =>
        valor == null ? "" : valor.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? $"\"{valor.Replace("\"", "\"\"")}\"" : valor;

    private static byte[] Utf8ConBom(StringBuilder texto) => Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(texto.ToString())).ToArray();
}
