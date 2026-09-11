# Sistema de Gestión de Empleados - ASP.NET Core

Sistema completo para la gestión de recursos humanos desarrollado con ASP.NET Core 8. Implementa una arquitectura de tres capas con dos aplicaciones independientes (Web API y Web MVC) que comparten la misma base de datos a través de una Class Library. 

El proyecto ha sido elevado a un estándar empresarial, incluyendo un rediseño completo de la interfaz con soporte nativo para modo claro/oscuro, accesibilidad WCAG AA, integridad referencial estricta y un conjunto robusto de pruebas unitarias.

## Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server Express](https://www.microsoft.com/es-es/sql-server/sql-server-downloads) o LocalDB
- [SQL Server Management Studio (SSMS)](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) (opcional)
- [Visual Studio 2022](https://visualstudio.microsoft.com/es/) o VS Code con las cargas de trabajo:
  - ASP.NET and web development
  - .NET desktop development
  - Data storage and processing

## Estructura del Proyecto

```text
EmpleadosSolution/
├── Empleados.Data/          # Class Library - Entidades, DbContext, Migraciones, DTOs
├── Empleados.Api/           # ASP.NET Core Web API (REST, JWT, Swagger)
├── Empleados.Mvc/           # ASP.NET Core MVC (Razor, Identity, UI Moderna)
└── Empleados.Tests/         # Pruebas unitarias con xUnit e In-Memory Database
```

## Configuración Inicial

### 1. Clonar el repositorio
```bash
git clone https://github.com/Joel-S-Florian/EmpleadosSolution.git
cd EmpleadosSolution
```

### 2. Restaurar paquetes
```bash
dotnet restore Empleados.Api/Empleados.Api.csproj
dotnet restore Empleados.Mvc/Empleados.Mvc.csproj
dotnet restore Empleados.Tests/Empleados.Tests.csproj
```

### 3. Configurar la cadena de conexión
Asegúrate de que `appsettings.json` en ambos proyectos (Api y Mvc) tenga la conexión correcta:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EmpleadosDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### 4. Aplicar migraciones a la base de datos
```bash
dotnet ef database update --project Empleados.Data --startup-project Empleados.Api
```

### 5. Compilar el proyecto
```bash
dotnet build
```

## Ejecutar la Aplicación

Debes ejecutar las dos aplicaciones en terminales separadas.

**Terminal 1: Web API**
```bash
dotnet run --project Empleados.Api
```
- HTTPS: `https://localhost:7114`
- Swagger: `https://localhost:7114/swagger`

**Terminal 2: Web MVC**
```bash
dotnet run --project Empleados.Mvc
```
- HTTPS: `https://localhost:7293`

## Credenciales de Prueba

**Acceso Web MVC (Identity):**
| Rol | Usuario | Contraseña |
|-----|---------|------------|
| Admin | admin@mvc.com | Admin123 |
| RRHH | rrhh@mvc.com | RRHH123 |

**Usuarios de Prueba (Seed API - Roles Cruzados):**
| Email | Contraseña | Roles en Proyectos/Deptos |
|-------|------------|---------------------------|
| demo@demo.com | demo123 | Owner (Depto 1), Editor (Depto 2), Viewer (Depto 3) |
| ana@demo.com | ana123 | Owner (Depto 2), Editor (Depto 1) |
| luis@demo.com | luis123 | Owner (Depto 3), Viewer (Depto 1) |
| joel@demo.com | joel123 | Editor (Depto 1 y 2), Viewer (Depto 3) |

## Ejecutar Pruebas Unitarias
```bash
dotnet test
```
*Nota: La suite de pruebas incluye 11 tests que validan agregaciones de reportes, integridad referencial de puestos y triggers de notificaciones.*

## Endpoints de la API

### Empleados
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/Empleados?page=1&pageSize=10` | Listado paginado con filtros |
| GET | `/api/Empleados/5` | Obtener empleado por ID |
| POST | `/api/Empleados` | Crear empleado (genera notificación automática) |
| PUT | `/api/Empleados/5` | Actualizar empleado |
| PATCH | `/api/Empleados/5/desactivar` | Desactivar empleado |
| PATCH | `/api/Empleados/5/despedir` | Despedir empleado |
| POST | `/api/Empleados/5/foto` | Subir foto (multipart/form-data) |

### Departamentos y Puestos
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/Departamentos` | Listar departamentos |
| POST | `/api/Departamentos` | Crear departamento |
| GET | `/api/Puestos?departamentoId=X` | Listar puestos filtrados por departamento |
| POST | `/api/Puestos` | Crear puesto (valida existencia de departamento) |

### Reportes y Notificaciones
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/Reportes/Departamentos` | Métricas agregadas (Total, Activos, Nómina) |
| GET | `/api/Notificaciones/mis-notificaciones` | Obtener notificaciones del usuario autenticado |
| POST | `/api/Notificaciones/marcar-leida/{id}` | Marcar notificación como leída |

### Autenticación (API)
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/Auth/login` | Obtener token JWT |

## Tecnologías Utilizadas

- **.NET 8** (LTS)
- **ASP.NET Core Web API** y **ASP.NET Core MVC**
- **Entity Framework Core** (Code First, LINQ optimizado con GroupJoin)
- **SQL Server** (LocalDB / Express)
- **ASP.NET Core Identity** y **JWT Bearer**
- **xUnit** con In-Memory Database para pruebas
- **Bootstrap 5** + **Bootstrap Icons** (sin emojis, diseño minimalista)
- **CSS Design Tokens** (Soporte nativo Modo Claro/Oscuro con persistencia)

## Funcionalidades Avanzadas Implementadas

### 1. Módulo de Reportes Ejecutivos
- Dashboard con métricas agregadas en tiempo real por departamento.
- Consultas LINQ optimizadas para evitar carga innecesaria en memoria.
- Formato de moneda local y visualización responsiva.

### 2. Integridad Referencial de Puestos
- Los puestos están vinculados estrictamente a departamentos mediante Foreign Key.
- Validación cruzada en API y MVC: no se puede asignar un puesto de otro departamento a un empleado.
- Filtros dinámicos en frontend: al seleccionar un departamento, los puestos disponibles se actualizan vía AJAX.

### 3. Sistema de Notificaciones Internas
- Alertas automáticas en la base de datos ante eventos críticos (ej. nuevo registro de empleado).
- Endpoints dedicados para recuperación y marcado de lectura.
- Implementación en MVC con polling asíncrono (30s) y badge de contador en el navbar.

### 4. Rediseño UI/UX Moderno (3 Fases)
- **Sistema de Diseño:** Variables CSS (:root y [data-theme="dark"]) para transiciones suaves y detección de preferencia del sistema.
- **Componentes Reutilizables:** Partials de Razor para _BadgeEstado, _EmptyState y _StatCard, eliminando duplicación de estilos.
- **Accesibilidad WCAG AA:** Anillos de foco visibles (focus-visible), ratios de contraste 16:1 y jerarquía tipográfica clara (Inter / DM Sans).
- **Área de Identity:** Rediseño completo de `/Account/Manage` para coincidir con el sistema de diseño global.

## Reglas de Negocio

- Desactivar no es igual a Despedir (estados mutuamente excluyentes).
- No se permite la eliminación física de empleados (soft delete mediante cambio de estado).
- No se puede eliminar un departamento si tiene puestos o empleados asociados.
- Un empleado despedido no puede ser modificado.
- Un puesto solo puede ser asignado a un empleado si pertenece al mismo departamento que el empleado.

## Autor

- **Joel Starling Florian del Rosario** - [GitHub](https://github.com/Joel-S-Florian)

## Licencia

Este proyecto es educativo y de portafolio, sin fines comerciales.
