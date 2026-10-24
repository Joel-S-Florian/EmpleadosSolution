using Empleados.Data.Data;
using Empleados.Data.Services;

namespace Empleados.Tests;

// Construye los servicios reales sobre el DbContext en memoria de cada test
internal static class Servicios
{
    public static EmpleadoService Empleados(EmpleadosDbContext context, IContextoUsuario? contexto = null, AlmacenFotos? fotos = null) =>
        new(context,
            new NotificacionService(context),
            fotos ?? new AlmacenFotos(Path.Combine(Path.GetTempPath(), "empleados-tests")),
            new ParametrosLegalesService(context),
            contexto ?? new ContextoFijo());

    public static DepartamentoService Departamentos(EmpleadosDbContext context, IContextoUsuario? contexto = null) =>
        new(context, contexto ?? new ContextoFijo());

    public static PuestoService Puestos(EmpleadosDbContext context) => new(context);
    public static NotificacionService Notificaciones(EmpleadosDbContext context) => new(context);
    public static ReporteService Reportes(EmpleadosDbContext context) => new(context);
}

// Usuario y empresa fijos para tests. Pasado al DbContext, activa el filtro por empresa.
internal class ContextoFijo : IContextoUsuario
{
    public ContextoFijo(int? empresaId = 1, string? usuarioId = "user1")
    {
        EmpresaId = empresaId;
        UsuarioId = usuarioId;
    }

    public bool EsSistema => false;
    public string? UsuarioId { get; }
    public string? UsuarioNombre => UsuarioId;
    public int? EmpresaId { get; }
}
