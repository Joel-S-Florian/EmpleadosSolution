# Sistema de Gestión de Empleados - ASP.NET Core

Sistema completo para la gestión de empleados desarrollado con ASP.NET Core, implementando dos aplicaciones independientes que comparten la misma base de datos.

## Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server Express](https://www.microsoft.com/es-es/sql-server/sql-server-downloads) o LocalDB
- [SQL Server Management Studio (SSMS)](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) (opcional)
- [Visual Studio 2022](https://visualstudio.microsoft.com/es/) con las cargas de trabajo:
  - ASP.NET and web development
  - .NET desktop development
  - Data storage and processing

## Estructura del Proyecto

```
EmpleadosSolution/
├── Empleados.Data/          # Class Library - Entidades, DbContext, Migraciones
├── Empleados.Api/           # ASP.NET Core Web API
├── Empleados.Mvc/           # ASP.NET Core MVC
└── Empleados.Tests/         # Pruebas unitarias con xUnit
```

## ⚙️ Configuración Inicial

### 1. Clonar el repositorio

```bash
git clone https://github.com/Joel-S-Florian/EmpleadosSolution.git
cd EmpleadosSolution
```

### 2. Restaurar paquetes

```bash
dotnet restore
```

### 3. Configurar la cadena de conexión

**Para la API:** `Empleados.Api/appsettings.json`
**Para el MVC:** `Empleados.Mvc/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EmpleadosDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### 4. Configurar la clave JWT de la API

La clave no se guarda en el repositorio. En desarrollo va en *user-secrets* (mínimo 32 caracteres):

```bash
dotnet user-secrets set "Jwt:Key" "una-clave-larga-y-aleatoria-de-al-menos-32-caracteres" --project Empleados.Api
```

En producción usa la variable de entorno `Jwt__Key`. Sin esta clave la API no arranca.

Los orígenes permitidos por CORS se configuran en `Cors:AllowedOrigins` de `Empleados.Api/appsettings.json`.

### 5. Aplicar migraciones a la base de datos

```bash
dotnet ef database update --project Empleados.Data --startup-project Empleados.Api
```

### 6. Compilar el proyecto

```bash
dotnet build
```

## 🖥️ Ejecutar la Web API

```bash
dotnet run --project Empleados.Api
```

La API estará disponible en:
- **HTTPS:** `https://localhost:7114`
- **HTTP:** `http://localhost:5284`
- **Swagger:** `https://localhost:7114/swagger`

## 🌐 Ejecutar la Web MVC

```bash
dotnet run --project Empleados.Mvc
```

La aplicación MVC estará disponible en:
- **HTTPS:** `https://localhost:7293` 

## Credenciales de Prueba

| Rol | Usuario | Contraseña |
|-----|---------|------------|
| **Admin** | admin@mvc.com | Admin123 |
| **RRHH** | rrhh@mvc.com | Rrhh1234 |

Estos usuarios se crean al arrancar el MVC solo en desarrollo (contraseñas en `SeedUsers` de `Empleados.Mvc/appsettings.Development.json`). Las contraseñas nuevas deben tener al menos 8 caracteres, con mayúscula, minúscula y número.

## Ejecutar Pruebas Unitarias

```bash
dotnet test
```

## Endpoints de la API

### Empleados
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/Empleados?page=1&pageSize=10` | Listado paginado |
| GET | `/api/Empleados/5` | Obtener empleado por ID |
| POST | `/api/Empleados` | Crear empleado |
| PUT | `/api/Empleados/5` | Actualizar empleado |
| PATCH | `/api/Empleados/5/desactivar` | Desactivar empleado |
| PATCH | `/api/Empleados/5/despedir` | Despedir empleado |
| POST | `/api/Empleados/5/foto` | Subir foto |

### Departamentos
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/Departamentos` | Listar departamentos |
| GET | `/api/Departamentos/5` | Obtener departamento por ID |
| POST | `/api/Departamentos` | Crear departamento |
| PUT | `/api/Departamentos/5` | Actualizar departamento |
| DELETE | `/api/Departamentos/5` | Eliminar departamento |

### Autenticación (API)
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/Auth/login` | Obtener token JWT |

**Ejemplo de login:**
```json
{
  "correo": "admin@mvc.com",
  "contrasena": "Admin123"
}
```

## Tecnologías Utilizadas

- **.NET 8** (LTS)
- **ASP.NET Core MVC** - Interfaz web
- **ASP.NET Core Web API** - API REST
- **Entity Framework Core** - ORM (Code First)
- **SQL Server** - Base de datos
- **Identity** - Autenticación y autorización
- **JWT Bearer** - Autenticación en API
- **Swagger/OpenAPI** - Documentación de API
- **Bootstrap 5** - UI responsiva
- **xUnit** - Pruebas unitarias

## Funcionalidades Implementadas

### Web MVC
- Autenticación por cookies con Identity
- CRUD completo de empleados (Crear, Leer, Actualizar, Desactivar, Despedir)
- CRUD completo de departamentos
- Paginación y filtros
- Subida de fotos (multipart/form-data)
- Autorización por roles (Admin/RRHH)
- Diseño responsivo con Bootstrap

### Web API
- Autenticación JWT
- Endpoints REST completos para empleados y departamentos
- Paginación y filtros
- DTOs para entrada/salida
- Validación con Data Annotations
- Documentación Swagger
- CORS configurado

### Reglas de Negocio
- Desactivar ≠ Despedir (estados diferentes)
- No eliminar físicamente empleados
- No eliminar departamento con empleados asociados
- Empleado despedido no se puede modificar

## Autor

- **Joel Starling Florian del Rosario** - [GitHub](https://github.com/Joel-S-Florian)

## Licencia

Este proyecto es educativo - sin fines comerciales.

