namespace Empleados.Data.Services;

public enum TipoResultado
{
    Ok,
    NoEncontrado,
    Invalido,
    Prohibido
}

// Resultado de una operación de negocio. Campo indica el input con error (para ModelState en el MVC).
public record Resultado(TipoResultado Tipo, string? Mensaje = null, string? Campo = null)
{
    public bool Ok => Tipo == TipoResultado.Ok;

    public static Resultado Exito(string? mensaje = null) => new(TipoResultado.Ok, mensaje);
    public static Resultado NoEncontrado(string mensaje) => new(TipoResultado.NoEncontrado, mensaje);
    public static Resultado Invalido(string mensaje, string? campo = null) => new(TipoResultado.Invalido, mensaje, campo);
    public static Resultado Prohibido(string mensaje) => new(TipoResultado.Prohibido, mensaje);
}

public record Resultado<T>(TipoResultado Tipo, T? Valor = default, string? Mensaje = null, string? Campo = null)
    : Resultado(Tipo, Mensaje, Campo)
{
    public static Resultado<T> Con(T valor, string? mensaje = null) => new(TipoResultado.Ok, valor, mensaje);
    public static Resultado<T> Fallo(Resultado error) => new(error.Tipo, default, error.Mensaje, error.Campo);
}

public record Paginado<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
