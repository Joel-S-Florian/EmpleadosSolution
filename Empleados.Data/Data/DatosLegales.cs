using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Data.Data;

// Valores legales iniciales (2026). Fuentes secundarias: validar contra DGII, TSS y Ministerio de Trabajo.
// Los cambios posteriores se registran como nuevas vigencias desde la pantalla de parámetros, no aquí.
internal static class DatosLegales
{
    private static readonly DateTime Inicio2026 = new(2026, 1, 1);

    public static void Sembrar(ModelBuilder modelBuilder)
    {
        const string tss = "TSS 2026 (Ley 87-01, Res. 01-2025)";
        const string codigo = "Código de Trabajo, Ley 16-92";
        var id = 0;
        ParametroLegal P(string clave, decimal valor, string fuente) =>
            new() { Id = ++id, Clave = clave, Valor = valor, VigenteDesde = Inicio2026, Fuente = fuente };

        modelBuilder.Entity<ParametroLegal>().HasData(
            P(ClavesParametro.AfpEmpleado, 2.87m, tss),
            P(ClavesParametro.AfpEmpleador, 7.10m, tss),
            P(ClavesParametro.SfsEmpleado, 3.04m, tss),
            P(ClavesParametro.SfsEmpleador, 7.09m, tss),
            P(ClavesParametro.SalarioMinimoCotizable, 23223m, tss),
            P(ClavesParametro.TopeAfpSalarios, 20m, tss),
            P(ClavesParametro.TopeSfsSalarios, 10m, tss),
            P(ClavesParametro.TopeSrlSalarios, 4m, tss),
            P(ClavesParametro.InfotepEmpleador, 1.00m, "Ley 116-80 (INFOTEP)"),
            P(ClavesParametro.InfotepBonificacion, 0.50m, "Ley 116-80 (INFOTEP)"),
            P(ClavesParametro.DivisorSalarioDiario, 23.83m, codigo),
            P(ClavesParametro.HorasJornadaDiaria, 8m, codigo),
            P(ClavesParametro.HorasJornadaSemanal, 44m, codigo),
            P(ClavesParametro.UmbralHorasExtraExceso, 68m, codigo + ", art. 203"),
            P(ClavesParametro.RecargoHoraExtra, 35m, codigo + ", art. 203"),
            P(ClavesParametro.RecargoHoraExtraExceso, 100m, codigo + ", art. 203"),
            P(ClavesParametro.RecargoNocturno, 15m, codigo + ", art. 204"),
            P(ClavesParametro.RecargoFeriado, 100m, codigo),
            P(ClavesParametro.VacacionesHasta5Anios, 14m, codigo + ", art. 177"),
            P(ClavesParametro.VacacionesMas5Anios, 18m, codigo + ", art. 177"),
            P(ClavesParametro.LicenciaPaternidadDias, 2m, codigo + ", art. 54"),
            P(ClavesParametro.LicenciaMaternidadSemanas, 14m, "Res. CNSS 418-02 / Convenio 183 OIT"));

        // Escala anual de ISR de asalariados (DGII, igual a 2025)
        modelBuilder.Entity<TramoIsr>().HasData(
            new TramoIsr { Id = 1, VigenteDesde = Inicio2026, Desde = 0m, Hasta = 416220m, Tasa = 0m, CuotaFija = 0m },
            new TramoIsr { Id = 2, VigenteDesde = Inicio2026, Desde = 416220m, Hasta = 624329m, Tasa = 15m, CuotaFija = 0m },
            new TramoIsr { Id = 3, VigenteDesde = Inicio2026, Desde = 624329m, Hasta = 867123m, Tasa = 20m, CuotaFija = 31216m },
            new TramoIsr { Id = 4, VigenteDesde = Inicio2026, Desde = 867123m, Hasta = null, Tasa = 25m, CuotaFija = 79776m });

        // Resolución CNS-01-2025, segundo tramo vigente desde el 1-feb-2026 (no sectorizado)
        var febrero2026 = new DateTime(2026, 2, 1);
        modelBuilder.Entity<SalarioMinimo>().HasData(
            new SalarioMinimo { Id = 1, VigenteDesde = febrero2026, Tamano = TamanoEmpresa.Grande, Monto = 29988.00m },
            new SalarioMinimo { Id = 2, VigenteDesde = febrero2026, Tamano = TamanoEmpresa.Mediana, Monto = 27489.60m },
            new SalarioMinimo { Id = 3, VigenteDesde = febrero2026, Tamano = TamanoEmpresa.Pequena, Monto = 18421.20m },
            new SalarioMinimo { Id = 4, VigenteDesde = febrero2026, Tamano = TamanoEmpresa.Micro, Monto = 16993.20m });

        // Fechas de disfrute 2026 publicadas por el Ministerio de Trabajo (Ley 139-97)
        var feriados = new (int Mes, int Dia, string Nombre)[]
        {
            (1, 1, "Año Nuevo"),
            (1, 5, "Día de los Santos Reyes (trasladado)"),
            (1, 21, "Día de la Altagracia"),
            (1, 26, "Día de Duarte"),
            (2, 27, "Día de la Independencia"),
            (4, 3, "Viernes Santo"),
            (5, 4, "Día del Trabajo (trasladado)"),
            (6, 4, "Corpus Christi"),
            (8, 16, "Día de la Restauración"),
            (9, 24, "Día de las Mercedes"),
            (11, 9, "Día de la Constitución (trasladado)"),
            (12, 25, "Navidad")
        };
        modelBuilder.Entity<Feriado>().HasData(feriados.Select((f, i) =>
            new Feriado { Id = i + 1, Fecha = new DateTime(2026, f.Mes, f.Dia), Nombre = f.Nombre }));

        // shortcut: códigos internos; la dispersión bancaria (Fase 3.6) agregará el código ACH de cada banco
        var bancos = new (string Codigo, string Nombre)[]
        {
            ("BHD", "Banco BHD"),
            ("BANRESERVAS", "Banco de Reservas (Banreservas)"),
            ("POPULAR", "Banco Popular Dominicano"),
            ("SCOTIABANK", "Scotiabank"),
            ("BANESCO", "Banesco"),
            ("SANTACRUZ", "Banco Santa Cruz"),
            ("PROMERICA", "Banco Promerica"),
            ("CARIBE", "Banco Caribe"),
            ("BDI", "Banco BDI"),
            ("VIMENCA", "Banco Vimenca"),
            ("LOPEZDEHARO", "Banco López de Haro"),
            ("LAFISE", "Banco Lafise"),
            ("ADEMI", "Banco Ademi"),
            ("CITI", "Citibank"),
            ("APAP", "Asociación Popular de Ahorros y Préstamos"),
            ("CIBAO", "Asociación Cibao de Ahorros y Préstamos")
        };
        modelBuilder.Entity<Banco>().HasData(bancos.Select((b, i) =>
            new Banco { Id = i + 1, Codigo = b.Codigo, Nombre = b.Nombre }));
    }
}
