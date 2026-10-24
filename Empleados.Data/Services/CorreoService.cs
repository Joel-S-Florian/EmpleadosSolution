using Empleados.Data.Data;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace Empleados.Data.Services;

// Sección "Correo" de appsettings. Sin Host, los correos se guardan como .eml en CarpetaPrueba (para desarrollo).
public class OpcionesCorreo
{
    public string? Host { get; set; }
    public int Puerto { get; set; } = 587;
    public bool Ssl { get; set; } = true;
    public string? Usuario { get; set; }
    public string? Clave { get; set; }
    public string Remitente { get; set; } = "nomina@empleados.local";
    public string CarpetaPrueba { get; set; } = "../correos";
}

public interface IEnviadorCorreo
{
    Task EnviarAsync(MailMessage mensaje);
}

public class EnviadorSmtp : IEnviadorCorreo
{
    private readonly OpcionesCorreo _opciones;

    public EnviadorSmtp(OpcionesCorreo opciones)
    {
        _opciones = opciones;
    }

    public async Task EnviarAsync(MailMessage mensaje)
    {
        mensaje.From ??= new MailAddress(_opciones.Remitente);
        using var cliente = new SmtpClient();
        if (string.IsNullOrWhiteSpace(_opciones.Host))
        {
            Directory.CreateDirectory(_opciones.CarpetaPrueba);
            cliente.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            cliente.PickupDirectoryLocation = Path.GetFullPath(_opciones.CarpetaPrueba);
        }
        else
        {
            cliente.Host = _opciones.Host;
            cliente.Port = _opciones.Puerto;
            cliente.EnableSsl = _opciones.Ssl;
            if (!string.IsNullOrEmpty(_opciones.Usuario))
                cliente.Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Clave);
        }
        await cliente.SendMailAsync(mensaje);
    }
}

// Envío de los recibos de una nómina pagada, uno por empleado, con registro de entregado o fallido
public class RecibosCorreoService
{
    private readonly EmpleadosDbContext _context;
    private readonly IEnviadorCorreo _enviador;

    public RecibosCorreoService(EmpleadosDbContext context, IEnviadorCorreo enviador)
    {
        _context = context;
        _enviador = enviador;
    }

    public Task<List<EnvioCorreo>> EnviosAsync(int nominaId) =>
        _context.EnviosCorreo
            .Where(e => e.ReciboId != null && _context.RecibosPago.Any(r => r.Id == e.ReciboId && r.NominaId == nominaId))
            .OrderByDescending(e => e.Fecha)
            .ToListAsync();

    // shortcut: envío en la misma petición, uno tras otro; para nóminas grandes conviene una cola en segundo plano
    public async Task<Resultado> EnviarNominaAsync(int nominaId)
    {
        var nomina = await _context.Nominas.Include(n => n.Recibos).ThenInclude(r => r.Lineas)
            .Include(n => n.Recibos).ThenInclude(r => r.Empleado)
            .AsSplitQuery()
            .FirstOrDefaultAsync(n => n.Id == nominaId);
        if (nomina == null)
            return Resultado.NoEncontrado("Nómina no encontrada");
        if (nomina.Estado is not (EstadoNomina.Pagada or EstadoNomina.Cerrada))
            return Resultado.Invalido("Los recibos se envían cuando la nómina está pagada");

        var empresa = await _context.Empresas.FirstAsync(e => e.Id == nomina.EmpresaId);
        int enviados = 0, fallidos = 0, sinCorreo = 0;
        foreach (var recibo in nomina.Recibos.OrderBy(r => r.NombreEmpleado))
        {
            var correo = recibo.Empleado?.CorreoElectronico;
            if (string.IsNullOrWhiteSpace(correo))
            {
                sinCorreo++;
                continue;
            }

            var asunto = $"Recibo de pago {nomina.Descripcion} - {empresa.NombreComercial ?? empresa.RazonSocial}";
            var envio = new EnvioCorreo { EmpresaId = nomina.EmpresaId, ReciboId = recibo.Id, EmpleadoId = recibo.EmpleadoId, Destino = correo, Asunto = asunto, Fecha = DateTime.Now };
            try
            {
                using var mensaje = new MailMessage { Subject = asunto, Body = $"Hola {recibo.NombreEmpleado}:\n\nAdjunto su recibo de pago de la nómina {nomina.Descripcion}.\nNeto pagado: RD$ {recibo.Neto:N2}.\n\n{empresa.RazonSocial}" };
                mensaje.To.Add(correo);
                var pdf = ReciboPdf.Generar(empresa, nomina, new[] { recibo });
                mensaje.Attachments.Add(new Attachment(new MemoryStream(pdf), $"recibo-{nomina.Anio}-{nomina.Mes:00}-{recibo.Id}.pdf", "application/pdf"));
                await _enviador.EnviarAsync(mensaje);
                envio.Estado = EstadoEnvio.Enviado;
                enviados++;
            }
            catch (Exception ex) when (ex is SmtpException or FormatException or InvalidOperationException or IOException)
            {
                envio.Estado = EstadoEnvio.Fallido;
                envio.Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                fallidos++;
            }
            _context.EnviosCorreo.Add(envio);
        }
        await _context.SaveChangesAsync();
        return Resultado.Exito($"Recibos por correo: {enviados} enviado(s)" +
                               (fallidos > 0 ? $", {fallidos} fallido(s)" : "") +
                               (sinCorreo > 0 ? $", {sinCorreo} sin correo" : ""));
    }
}
