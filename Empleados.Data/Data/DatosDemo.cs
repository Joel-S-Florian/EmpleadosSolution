using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Empleados.Data.Data;

// Empresa de demostración con un año de operación: estructura, empleados, préstamos, incidencias,
// nóminas de enero a octubre de 2026 calculadas con el motor real, bonificación y liquidaciones.
// Solo para desarrollo: se ejecuta con "dotnet run --project Empleados.Mvc -- --demo".
public static class DatosDemo
{
    public const string Rnc = "131245789";
    private const int Anio = 2026;

    // Usuario que figura como autor de lo generado
    private class ContextoDemo : IContextoUsuario
    {
        public ContextoDemo(int empresaId) => EmpresaId = empresaId;
        public bool EsSistema => true;
        public string? UsuarioId => null;
        public string? UsuarioNombre => "Datos de demostración";
        public int? EmpresaId { get; }
    }

    private record PuestoDemo(string Departamento, string Nombre, decimal? Minimo, decimal? Maximo);

    private record EmpleadoDemo(string Nombre, string Apellido, Sexo Sexo, string Puesto, decimal Salario, string Ingreso, string Nacimiento,
        FrecuenciaPago Frecuencia = FrecuenciaPago.Quincenal, TipoSalario Tipo = TipoSalario.FijoMensual, int? Banco = 1,
        string? FinContrato = null, EstadoEmpleado Estado = EstadoEmpleado.Activo);

    private static readonly PuestoDemo[] Puestos =
    {
        new("Administración", "Gerente General", 180000, 300000),
        new("Administración", "Asistente Administrativa", 32000, 45000),
        new("Administración", "Recepcionista", 28000, 35000),
        new("Ventas", "Gerente de Ventas", 110000, 160000),
        new("Ventas", "Ejecutivo de Ventas", 35000, 60000),
        new("Ventas", "Vendedor de Ruta", 30000, 45000),
        new("Almacén y Logística", "Supervisor de Almacén", 55000, 75000),
        new("Almacén y Logística", "Almacenista", 28000, 36000),
        new("Almacén y Logística", "Chofer", 30000, 40000),
        new("Almacén y Logística", "Ayudante", null, null),
        new("Contabilidad", "Contador", 70000, 100000),
        new("Contabilidad", "Auxiliar Contable", 35000, 50000),
        new("Tecnología", "Desarrollador", 80000, 140000),
        new("Tecnología", "Soporte Técnico", 40000, 60000),
        new("Recursos Humanos", "Encargada de Recursos Humanos", 75000, 110000),
        new("Recursos Humanos", "Analista de Nómina", 45000, 65000)
    };

    private static readonly EmpleadoDemo[] Empleados =
    {
        new("Rafael Antonio", "Peña Almonte", Sexo.Masculino, "Gerente General", 240000, "2015-03-02", "1975-06-14", FrecuenciaPago.Mensual),
        new("Yokasta", "Ramírez Féliz", Sexo.Femenino, "Asistente Administrativa", 38000, "2019-07-15", "1990-10-24"),
        new("Dulce María", "Castillo de León", Sexo.Femenino, "Recepcionista", 30500, "2023-02-01", "1998-04-09", Banco: 2),
        new("Héctor Manuel", "Guzmán Tejada", Sexo.Masculino, "Gerente de Ventas", 135000, "2017-09-04", "1980-11-30", FrecuenciaPago.Mensual),
        new("Mariela", "Santos Batista", Sexo.Femenino, "Ejecutivo de Ventas", 48000, "2020-01-13", "1992-03-18"),
        new("Wilson", "Rodríguez Paulino", Sexo.Masculino, "Ejecutivo de Ventas", 42000, "2022-05-02", "1994-08-07", Banco: 3),
        new("Arisleidy", "Mejía Cruz", Sexo.Femenino, "Ejecutivo de Ventas", 39500, "2024-08-19", "1997-12-02"),
        new("Starlin", "Pérez Ventura", Sexo.Masculino, "Vendedor de Ruta", 33000, "2021-04-05", "1996-02-21", Banco: 2),
        new("Félix", "Encarnación Polanco", Sexo.Masculino, "Vendedor de Ruta", 32000, "2025-03-10", "1999-07-11", Banco: null),
        new("Ramón Emilio", "Taveras Núñez", Sexo.Masculino, "Supervisor de Almacén", 64000, "2016-11-07", "1983-05-26"),
        new("Junior", "Ozuna Matos", Sexo.Masculino, "Almacenista", 31000, "2022-09-12", "2000-09-15", Banco: 2),
        new("Eddy", "Brito Sánchez", Sexo.Masculino, "Almacenista", 30000, "2024-01-15", "1995-01-28", Banco: null),
        new("Domingo", "Valdez Rosario", Sexo.Masculino, "Chofer", 35000, "2018-06-18", "1985-12-12", Banco: 2),
        new("Cristian", "Lantigua Morel", Sexo.Masculino, "Chofer", 33500, "2023-10-02", "1993-06-03"),
        new("Yan Carlos", "De la Rosa Pujols", Sexo.Masculino, "Ayudante", 165, "2024-04-01", "2001-03-14", Tipo: TipoSalario.PorHora, Banco: 3),
        new("Alexander", "Frías Cabrera", Sexo.Masculino, "Ayudante", 160, "2025-11-03", "2003-08-22", Tipo: TipoSalario.PorHora, Banco: null),
        new("Carmen Rosa", "Hernández Vargas", Sexo.Femenino, "Contador", 88000, "2016-02-15", "1979-09-05", FrecuenciaPago.Mensual),
        new("Nathalie", "Jiménez Abreu", Sexo.Femenino, "Auxiliar Contable", 42000, "2021-08-23", "1995-11-19"),
        new("Luis Manuel", "Ortiz Caraballo", Sexo.Masculino, "Auxiliar Contable", 38000, "2026-02-02", "1998-01-07"),
        new("Miguel Ángel", "Reyes Florentino", Sexo.Masculino, "Desarrollador", 125000, "2019-03-11", "1988-04-30", FrecuenciaPago.Mensual, Banco: 3),
        new("Paola", "Sánchez Grullón", Sexo.Femenino, "Desarrollador", 95000, "2023-06-05", "1996-10-24", FrecuenciaPago.Mensual),
        new("Víctor", "Almánzar Rivas", Sexo.Masculino, "Soporte Técnico", 48000, "2022-02-14", "1997-05-16"),
        new("Indhira", "Báez Marte", Sexo.Femenino, "Encargada de Recursos Humanos", 92000, "2018-01-08", "1984-07-27", FrecuenciaPago.Mensual),
        new("Yesenia", "Domínguez Lora", Sexo.Femenino, "Analista de Nómina", 55000, "2020-10-19", "1991-02-13"),
        new("José Miguel", "Fernández Acosta", Sexo.Masculino, "Ejecutivo de Ventas", 45000, "2026-04-06", "1993-12-20"),
        new("Massiel", "Cordero Tavárez", Sexo.Femenino, "Recepcionista", 29000, "2026-07-01", "2002-05-05", Banco: 2),
        new("Francisco", "Liriano Soto", Sexo.Masculino, "Vendedor de Ruta", 31000, "2026-01-12", "1990-08-30", FinContrato: "2026-11-15"),
        new("Katherine", "Medina Pichardo", Sexo.Femenino, "Soporte Técnico", 42000, "2026-03-02", "1999-03-03", FinContrato: "2026-12-20"),
        new("Lourdes", "Herrera Bonilla", Sexo.Femenino, "Asistente Administrativa", 34000, "2020-05-18", "1987-10-10", Estado: EstadoEmpleado.Inactivo),
        new("Juan Carlos", "Polanco Durán", Sexo.Masculino, "Almacenista", 32500, "2019-08-05", "1986-04-17"),
        new("Esperanza", "Martínez Read", Sexo.Femenino, "Auxiliar Contable", 40000, "2017-04-24", "1982-06-08"),
        new("Manuel", "Guerrero Silverio", Sexo.Masculino, "Supervisor de Almacén", 60000, "2021-01-18", "1989-09-29", Banco: 2),
        // Salen durante el año: renuncia el 15 de junio y desahucio el 30 de septiembre
        new("Pedro Antonio", "Vásquez Concepción", Sexo.Masculino, "Ejecutivo de Ventas", 41000, "2019-02-04", "1990-01-25"),
        new("Rosa Elena", "Cáceres Ureña", Sexo.Femenino, "Chofer", 34000, "2018-09-10", "1984-11-02", Banco: 2)
    };

    public static async Task<string> CrearAsync(EmpleadosDbContext db, IEnumerable<string> usuarioIds)
    {
        if (await db.Empresas.IgnoreQueryFilters().AnyAsync(e => e.Rnc == Rnc))
            return "Los datos de demostración ya existen (Distribuidora del Caribe SRL)";

        var empresa = new Empresa
        {
            Rnc = Rnc, RazonSocial = "Distribuidora del Caribe SRL", NombreComercial = "DisCaribe", Tamano = TamanoEmpresa.Mediana,
            TasaRiesgoSrl = 1.10m, FrecuenciaPagoPredeterminada = FrecuenciaPago.Quincenal, MesCierreFiscal = 12,
            Direccion = "Av. John F. Kennedy 102, Santo Domingo", Telefono = "809-555-0142", CorreoElectronico = "rrhh@discaribe.com.do", Activa = true
        };
        db.Empresas.Add(empresa);
        await db.SaveChangesAsync();
        foreach (var usuario in usuarioIds)
            db.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario, EmpresaId = empresa.Id });

        var departamentos = Puestos.Select(p => p.Departamento).Distinct()
            .ToDictionary(n => n, n => new Departamento { EmpresaId = empresa.Id, Nombre = n, Descripcion = $"Departamento de {n.ToLowerInvariant()}" });
        db.Departamentos.AddRange(departamentos.Values);
        await db.SaveChangesAsync();
        var puestos = Puestos.ToDictionary(p => p.Nombre, p => new Puesto
        {
            EmpresaId = empresa.Id, DepartamentoId = departamentos[p.Departamento].Id, Nombre = p.Nombre, SalarioMinimo = p.Minimo, SalarioMaximo = p.Maximo
        });
        db.Puestos.AddRange(puestos.Values);
        await db.SaveChangesAsync();

        var azar = new Random(Anio);
        var afps = new[] { "AFP Popular", "AFP Reservas", "AFP Siembra", "AFP Crecer" };
        var arss = new[] { "ARS Humano", "ARS Universal", "ARS Senasa", "ARS Palic" };
        var empleados = Empleados.Select((d, i) =>
        {
            var puesto = puestos[d.Puesto];
            return new Empleado
            {
                EmpresaId = empresa.Id, Nombre = d.Nombre, Apellido = d.Apellido, Sexo = d.Sexo,
                CorreoElectronico = Correo(d.Nombre, d.Apellido), Telefono = $"809-{azar.Next(200, 999)}-{azar.Next(1000, 9999)}",
                Direccion = $"Calle {azar.Next(1, 40)} #{azar.Next(1, 120)}, {(i % 3 == 0 ? "Los Mina" : i % 3 == 1 ? "Naco" : "Villa Mella")}, Santo Domingo",
                FechaNacimiento = Fecha(d.Nacimiento), FechaContratacion = Fecha(d.Ingreso), Salario = d.Salario, Estado = d.Estado,
                DepartamentoId = puesto.DepartamentoId, PuestoId = puesto.Id, TipoDocumento = TipoDocumento.Cedula, NumeroDocumento = Cedula(azar),
                Nss = azar.Next(100000000, 999999999).ToString(), Afp = afps[i % afps.Length], Ars = arss[i % arss.Length],
                DependientesSfs = i % 7 == 0 ? 1 : 0, TipoContrato = d.FinContrato == null ? TipoContrato.Indefinido : TipoContrato.TiempoDefinido,
                FechaFinContrato = d.FinContrato == null ? null : Fecha(d.FinContrato), FrecuenciaPago = d.Frecuencia, TipoSalario = d.Tipo,
                BancoId = d.Banco, NumeroCuenta = d.Banco == null ? null : azar.NextInt64(1000000000, 9999999999).ToString(),
                TipoCuenta = d.Banco == null ? null : i % 4 == 0 ? TipoCuenta.Corriente : TipoCuenta.Ahorro
            };
        }).ToList();
        db.Empleados.AddRange(empleados);
        await db.SaveChangesAsync();
        db.MovimientosEmpleados.AddRange(empleados.Select(e => new MovimientoEmpleado
        {
            EmpresaId = empresa.Id, EmpleadoId = e.Id, Fecha = e.FechaContratacion, Tipo = TipoMovimiento.Alta, ValorNuevo = e.Salario.ToString("N2", CultureInfo.GetCultureInfo("es-DO")),
            UsuarioNombre = "Datos de demostración"
        }));
        await db.SaveChangesAsync();
        Empleado E(string nombre) => empleados.First(e => e.Nombre == nombre);

        // Servicios reales con la empresa de demostración como contexto
        var contexto = new ContextoDemo(empresa.Id);
        var parametros = new ParametrosLegalesService(db);
        var ciclo = new CicloNominaService(db, new NominaService(db, parametros), contexto);
        var incidencias = new IncidenciaService(db, parametros, contexto);
        var liquidaciones = new LiquidacionService(db, parametros, incidencias, contexto);
        var bonificacion = new BonificacionService(db, parametros, ciclo, contexto);
        var prestamos = new PrestamoService(db);
        await parametros.GenerarFeriadosAsync(Anio + 1);

        foreach (var (nombre, concepto, total, cuota, desde, finDeMes) in new[]
        {
            ("Wilson", "Préstamo personal", 30000m, 2500m, "2026-02-01", false),
            ("Junior", "Avance de salario", 8000m, 2000m, "2026-05-01", false),
            ("Domingo", "Cooperativa", 24000m, 2000m, "2026-01-01", true),
            ("Nathalie", "Préstamo para estudios", 45000m, 3000m, "2026-03-01", false),
            ("Víctor", "Compra de equipo", 12000m, 1500m, "2026-08-01", false)
        })
        {
            var r = await prestamos.CrearAsync(new PrestamoDto { EmpleadoId = E(nombre).Id, Concepto = concepto, MontoTotal = total, MontoCuota = cuota, FechaInicio = Fecha(desde), SoloFinDeMes = finDeMes });
            Exigir(r, $"préstamo de {nombre}");
        }

        async Task Incidencia(string nombre, TipoIncidencia tipo, string desde, string hasta, decimal horas = 0, bool aprobar = true, string? comentario = null)
        {
            var r = await incidencias.CrearAsync(new IncidenciaDto { EmpleadoId = E(nombre).Id, Tipo = tipo, FechaInicio = Fecha(desde), FechaFin = Fecha(hasta), Horas = horas, Comentario = comentario });
            Exigir(r, $"{tipo} de {nombre}");
            if (aprobar)
                Exigir(await incidencias.AprobarAsync(r.Valor!.Id), $"aprobar {tipo} de {nombre}");
        }
        await Incidencia("Starlin", TipoIncidencia.Retardo, "2026-02-12", "2026-02-12", horas: 2);
        await Incidencia("Mariela", TipoIncidencia.Falta, "2026-03-10", "2026-03-10", comentario: "Sin justificación");
        await Incidencia("Dulce María", TipoIncidencia.Vacaciones, "2026-03-30", "2026-04-03");
        await Incidencia("Arisleidy", TipoIncidencia.LicenciaMedica, "2026-04-06", "2026-04-10", comentario: "Certificado médico");
        await Incidencia("Junior", TipoIncidencia.Falta, "2026-05-19", "2026-05-20");
        await Incidencia("Rafael Antonio", TipoIncidencia.Vacaciones, "2026-07-13", "2026-07-24");
        await Incidencia("Cristian", TipoIncidencia.Retardo, "2026-07-09", "2026-07-09", horas: 1.5m);
        await Incidencia("Eddy", TipoIncidencia.Falta, "2026-08-04", "2026-08-04");
        await Incidencia("Ramón Emilio", TipoIncidencia.Vacaciones, "2026-08-17", "2026-08-28");
        await Incidencia("Nathalie", TipoIncidencia.PermisoSinGoce, "2026-09-14", "2026-09-15", comentario: "Asuntos personales");
        await Incidencia("Víctor", TipoIncidencia.LicenciaMatrimonio, "2026-09-21", "2026-09-25");
        await Incidencia("Paola", TipoIncidencia.Vacaciones, "2026-10-19", "2026-10-23");
        await Incidencia("Mariela", TipoIncidencia.Vacaciones, "2026-11-16", "2026-11-20", aprobar: false);
        await Incidencia("Yesenia", TipoIncidencia.PermisoConGoce, "2026-10-30", "2026-10-30", aprobar: false, comentario: "Cita médica");
        await Incidencia("Héctor Manuel", TipoIncidencia.Vacaciones, "2026-12-21", "2026-12-31", aprobar: false);

        // Nóminas de enero a octubre; octubre queda a medio ciclo para ver el flujo de aprobación
        for (var mes = 1; mes <= 10; mes++)
        {
            foreach (var (frecuencia, quincena) in new[] { (FrecuenciaPago.Quincenal, 1), (FrecuenciaPago.Quincenal, 2), (FrecuenciaPago.Mensual, 0) })
            {
                var creada = await ciclo.CrearAsync(new CrearNominaDto { Anio = Anio, Mes = mes, Frecuencia = frecuencia, Quincena = Math.Max(quincena, 1) });
                Exigir(creada, $"nómina {mes}/{frecuencia}/{quincena}");
                var nomina = (await ciclo.ObtenerAsync(creada.Valor!.Id))!;

                // Horas de los empleados por hora y alguna hora extra
                foreach (var recibo in nomina.Recibos.ToList())
                {
                    var empleado = empleados.First(e => e.Id == recibo.EmpleadoId);
                    var novedades = new NovedadesReciboDto();
                    if (empleado.TipoSalario == TipoSalario.PorHora)
                        novedades.HorasTrabajadas = 84 + azar.Next(0, 9);
                    if (empleado.Nombre is "Domingo" or "Cristian" && (mes + quincena) % 2 == 0)
                        novedades.HorasExtra = azar.Next(4, 13);
                    if (empleado.Nombre == "Mariela" && quincena == 2)
                        novedades.OtrosIngresos.Add(new IngresoSimuladoDto { Concepto = "Comisión por ventas", Monto = azar.Next(30, 90) * 100, CotizaTss = true, GravaIsr = true });
                    if (novedades.HorasTrabajadas > 0 || novedades.HorasExtra > 0 || novedades.OtrosIngresos.Count > 0)
                        Exigir(await ciclo.GuardarNovedadesAsync(recibo.Id, novedades), $"novedades de {empleado.Nombre}");
                }
                if (mes == 4 && quincena != 2)
                    Exigir(await bonificacion.AplicarAsync(Anio - 1, 3_500_000, nomina.Id), "bonificación 2025");

                var pago = quincena == 1 ? new DateTime(Anio, mes, 15) : new DateTime(Anio, mes, DateTime.DaysInMonth(Anio, mes));
                if (mes == 10 && quincena != 1)
                    continue; // 2.ª quincena y mensual de octubre: calculadas, por aprobar
                Exigir(await ciclo.AprobarAsync(nomina.Id), $"aprobar {nomina.Descripcion}");
                Exigir(await ciclo.PagarAsync(nomina.Id, pago), $"pagar {nomina.Descripcion}");
                if (mes < 10)
                    Exigir(await ciclo.CerrarAsync(nomina.Id), $"cerrar {nomina.Descripcion}");

                // Salidas del año, después de la quincena en la que trabajaron por última vez
                if (mes == 6 && quincena == 1)
                    await Liquidar("Pedro Antonio", CausaTerminacion.Renuncia, "2026-06-15", "2026-06-19");
                if (mes == 9 && quincena == 2)
                    await Liquidar("Rosa Elena", CausaTerminacion.Desahucio, "2026-09-30", "2026-10-05");
            }
        }

        async Task Liquidar(string nombre, CausaTerminacion causa, string salida, string pago)
        {
            var r = await liquidaciones.CrearAsync(new LiquidacionDto { EmpleadoId = E(nombre).Id, Causa = causa, FechaSalida = Fecha(salida) });
            Exigir(r, $"liquidación de {nombre}");
            Exigir(await liquidaciones.PagarAsync(r.Valor!.Id, Fecha(pago)), $"pagar liquidación de {nombre}");
        }

        var nominas = await db.Nominas.CountAsync(n => n.EmpresaId == empresa.Id);
        return $"Distribuidora del Caribe SRL: {empleados.Count} empleados, {nominas} nóminas, 5 préstamos, 15 incidencias y 2 liquidaciones";
    }

    private static void Exigir(Resultado resultado, string que)
    {
        if (!resultado.Ok)
            throw new InvalidOperationException($"Datos de demostración: no se pudo crear {que}: {resultado.Mensaje}");
    }

    private static DateTime Fecha(string valor) => DateTime.ParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Correo(string nombre, string apellido)
    {
        string Limpio(string texto) => new string(texto.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetter(c)).ToArray()).ToLowerInvariant();
        return $"{Limpio(nombre.Split(' ')[0])}.{Limpio(apellido.Split(' ')[0])}@discaribe.com.do";
    }

    // 11 dígitos con el dígito verificador de la cédula dominicana (Luhn)
    private static string Cedula(Random azar)
    {
        var base10 = "402" + azar.Next(0, 9999999).ToString("0000000");
        var suma = base10.Select((c, i) => { var d = (c - '0') * (i % 2 == 0 ? 1 : 2); return d > 9 ? d - 9 : d; }).Sum();
        return base10 + (10 - suma % 10) % 10;
    }
}
