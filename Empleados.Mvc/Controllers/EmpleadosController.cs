using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Security.Cryptography;

namespace Empleados.Mvc.Controllers;

[Authorize(Roles = "Admin,RRHH")]
public class EmpleadosController : Controller
{
    private readonly EmpleadoService _empleados;
    private readonly DepartamentoService _departamentos;
    private readonly PuestoService _puestos;

    public EmpleadosController(EmpleadoService empleados, DepartamentoService departamentos, PuestoService puestos)
    {
        _empleados = empleados;
        _departamentos = departamentos;
        _puestos = puestos;
    }

    // GET: Empleados
    public async Task<IActionResult> Index(
        string? nombre,
        string? apellido,
        DateTime? fechaContratacion,
        int page = 1,
        int pageSize = 10)
    {
        var resultado = await _empleados.ListarAsync(nombre, apellido, fechaContratacion, page, pageSize);

        ViewBag.Nombre = nombre;
        ViewBag.Apellido = apellido;
        ViewBag.FechaContratacion = fechaContratacion?.ToString("yyyy-MM-dd");
        ViewBag.CurrentPage = resultado.Page;
        ViewBag.PageSize = resultado.PageSize;
        ViewBag.TotalCount = resultado.TotalCount;
        ViewBag.TotalPages = resultado.TotalPages;

        return View(resultado.Items);
    }

    // GET: Empleados/Details/5
    public async Task<IActionResult> Details(int id, [FromServices] IncidenciaService incidencias)
    {
        var empleado = await _empleados.ObtenerAsync(id);
        if (empleado == null)
            return NotFound();

        ViewBag.Historial = await _empleados.HistorialAsync(id);
        ViewBag.Vacaciones = await incidencias.SaldoVacacionesAsync(id);
        ViewBag.Incidencias = (await incidencias.ListarAsync(id)).Take(10).ToList();
        return View(empleado);
    }

    // GET: Empleados/Create
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        await CargarListasAsync(null, null);
        return View(new CrearEmpleadoDto());
    }

    // POST: Empleados/Create
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CrearEmpleadoDto dto)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _empleados.CrearAsync(dto);
            if (resultado.Ok)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AgregarError(resultado);
        }

        await CargarListasAsync(dto.DepartamentoId, dto.PuestoId);
        return View(dto);
    }

    // GET: Empleados/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var empleado = await _empleados.ObtenerAsync(id);
        if (empleado == null)
            return NotFound();
        if (empleado.Estado == EstadoEmpleado.Despedido)
        {
            TempData["Error"] = "No se puede modificar un empleado despedido";
            return RedirectToAction(nameof(Details), new { id });
        }

        var dto = new ActualizarEmpleadoDto
        {
            Nombre = empleado.Nombre,
            Apellido = empleado.Apellido,
            FechaNacimiento = empleado.FechaNacimiento,
            Direccion = empleado.Direccion,
            Telefono = empleado.Telefono,
            CorreoElectronico = empleado.CorreoElectronico,
            Salario = empleado.Salario,
            FechaContratacion = empleado.FechaContratacion,
            DepartamentoId = empleado.DepartamentoId,
            PuestoId = empleado.PuestoId,
            TipoDocumento = empleado.TipoDocumento,
            NumeroDocumento = empleado.NumeroDocumento,
            Nss = empleado.Nss,
            Sexo = empleado.Sexo,
            Afp = empleado.Afp,
            Ars = empleado.Ars,
            DependientesSfs = empleado.DependientesSfs,
            TipoContrato = empleado.TipoContrato,
            FechaFinContrato = empleado.FechaFinContrato,
            FrecuenciaPago = empleado.FrecuenciaPago,
            TipoSalario = empleado.TipoSalario,
            BancoId = empleado.BancoId,
            NumeroCuenta = empleado.NumeroCuenta,
            TipoCuenta = empleado.TipoCuenta
        };
        await CargarListasAsync(dto.DepartamentoId, dto.PuestoId);
        return View(dto);
    }

    // POST: Empleados/Edit/5
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActualizarEmpleadoDto dto)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _empleados.ActualizarAsync(id, dto);
            if (resultado.Tipo == TipoResultado.NoEncontrado)
                return NotFound();
            if (resultado.Ok)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AgregarError(resultado);
        }

        await CargarListasAsync(dto.DepartamentoId, dto.PuestoId);
        return View(dto);
    }

    // POST: Empleados/Desactivar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id)
    {
        var resultado = await _empleados.DesactivarAsync(id);
        if (resultado.Tipo == TipoResultado.NoEncontrado)
            return NotFound();

        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    // POST: Empleados/Despedir/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Despedir(int id)
    {
        var resultado = await _empleados.DespedirAsync(id);
        if (resultado.Tipo == TipoResultado.NoEncontrado)
            return NotFound();

        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    // POST: Empleados/SubirFoto/5
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirFoto(int id, IFormFile? foto)
    {
        using var contenido = foto?.OpenReadStream();
        var resultado = await _empleados.SubirFotoAsync(id, contenido, foto?.FileName, foto?.Length ?? 0);
        if (resultado.Tipo == TipoResultado.NoEncontrado)
            return NotFound();

        TempData[resultado.Ok ? "Success" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Empleados/CrearAccesoPortal/5 - usuario del portal con su correo y una contraseña temporal
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearAccesoPortal(int id, [FromServices] EmpleadosDbContext context,
        [FromServices] UserManager<IdentityUser> usuarios, [FromServices] IEnviadorCorreo enviador)
    {
        var empleado = await context.Empleados.FirstOrDefaultAsync(e => e.Id == id);
        if (empleado == null)
            return NotFound();
        if (empleado.UsuarioId != null)
            return Aviso(id, "El empleado ya tiene acceso al portal", false);
        if (string.IsNullOrWhiteSpace(empleado.CorreoElectronico))
            return Aviso(id, "El empleado no tiene correo electrónico", false);
        if (await usuarios.FindByEmailAsync(empleado.CorreoElectronico) != null)
            return Aviso(id, "Ya existe un usuario con ese correo; use otro correo para el empleado", false);

        var clave = ClaveTemporal();
        var usuario = new IdentityUser { UserName = empleado.CorreoElectronico, Email = empleado.CorreoElectronico, EmailConfirmed = true };
        var creado = await usuarios.CreateAsync(usuario, clave);
        if (!creado.Succeeded)
            return Aviso(id, string.Join(" ", creado.Errors.Select(e => e.Description)), false);
        await usuarios.AddToRoleAsync(usuario, SoloPortalFilter.RolEmpleado);
        context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = empleado.EmpresaId });
        empleado.UsuarioId = usuario.Id;
        await context.SaveChangesAsync();

        var enviado = "";
        try
        {
            using var mensaje = new MailMessage { Subject = "Acceso al portal del empleado", Body =
                $"Hola {empleado.Nombre}:\n\nYa puede ver sus recibos y pedir vacaciones en {Request.Scheme}://{Request.Host}/Portal\n" +
                $"Usuario: {empleado.CorreoElectronico}\nContraseña temporal: {clave}\n\nCámbiela al entrar (menú de su cuenta)." };
            mensaje.To.Add(empleado.CorreoElectronico);
            await enviador.EnviarAsync(mensaje);
            enviado = " y se envió por correo";
        }
        catch (Exception ex) when (ex is SmtpException or FormatException or InvalidOperationException or IOException)
        {
            enviado = " (no se pudo enviar por correo)";
        }
        return Aviso(id, $"Acceso al portal creado. Contraseña temporal: {clave}{enviado}. Solo se muestra esta vez.", true);
    }

    // POST: Empleados/RevocarAccesoPortal/5
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevocarAccesoPortal(int id, [FromServices] EmpleadosDbContext context, [FromServices] UserManager<IdentityUser> usuarios)
    {
        var empleado = await context.Empleados.FirstOrDefaultAsync(e => e.Id == id);
        if (empleado == null)
            return NotFound();
        if (empleado.UsuarioId == null)
            return Aviso(id, "El empleado no tiene acceso al portal", false);

        var usuario = await usuarios.FindByIdAsync(empleado.UsuarioId);
        context.UsuariosEmpresas.RemoveRange(context.UsuariosEmpresas.Where(u => u.UsuarioId == empleado.UsuarioId));
        empleado.UsuarioId = null;
        await context.SaveChangesAsync();
        if (usuario != null)
            await usuarios.DeleteAsync(usuario);
        return Aviso(id, "Acceso al portal revocado", true);
    }

    private IActionResult Aviso(int id, string mensaje, bool ok)
    {
        TempData[ok ? "Success" : "Error"] = mensaje;
        return RedirectToAction(nameof(Details), new { id });
    }

    // 12 caracteres con mayúscula, minúscula y dígito (cumple la política de Identity)
    private static string ClaveTemporal()
    {
        const string letras = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var resto = new string(Enumerable.Range(0, 9).Select(_ => letras[RandomNumberGenerator.GetInt32(letras.Length)]).ToArray());
        return "Px" + RandomNumberGenerator.GetInt32(2, 10) + resto;
    }

    private async Task CargarListasAsync(int? departamentoId, int? puestoId)
    {
        ViewBag.Departamentos = new SelectList(await _departamentos.ListarAsync(), "Id", "Nombre", departamentoId);
        var puestos = departamentoId > 0 ? await _puestos.ListarAsync(departamentoId) : new List<Puesto>();
        ViewBag.Puestos = new SelectList(puestos, "Id", "Nombre", puestoId);
        ViewBag.Bancos = new SelectList(await _empleados.BancosAsync(), "Id", "Nombre");
    }
}
