using Empleados.Api.Services;
using Empleados.Data.Data;
using Empleados.Data.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Empleados API",
        Version = "v1",
        Description = "API para gestión de empleados y departamentos"
    });

    // Configurar Swagger para usar JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Pega solo el token JWT (sin el prefijo Bearer)",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// Registrar DbContext
builder.Services.AddDbContext<EmpleadosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ========================================
// AGREGAR IDENTITY (para autenticación)
// ========================================
// AddIdentityCore (sin cookies) para que el esquema por defecto sea JWT
builder.Services.AddIdentityCore<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<EmpleadosDbContext>()
    .AddDefaultTokenProviders();

// CORS: solo los orígenes configurados en Cors:AllowedOrigins
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// La clave JWT viene de user-secrets (desarrollo) o variables de entorno (producción)
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        "Falta Jwt:Key (mínimo 32 caracteres). En desarrollo: dotnet user-secrets set \"Jwt:Key\" \"<clave>\" --project Empleados.Api");

// Registrar servicios. Las fotos van a la carpeta compartida con el MVC (Uploads:Path, relativa al proyecto)
var carpetaUploads = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["Uploads:Path"] ?? "../uploads"));
builder.Services.AddScoped<AuthService>();
builder.Services.AddServiciosEmpleados(carpetaUploads);

// Configurar autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "EmpleadosApi",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EmpleadosApiClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Empleados API v1");
    });
}

app.UseHttpsRedirection();
Directory.CreateDirectory(carpetaUploads);
app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(carpetaUploads), RequestPath = AlmacenFotos.RutaPublica });
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();