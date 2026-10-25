# Sistema de Gestión de Empleados - ASP.NET Core

[![CI](https://github.com/Joel-S-Florian/EmpleadosSolution/actions/workflows/ci.yml/badge.svg)](https://github.com/Joel-S-Florian/EmpleadosSolution/actions/workflows/ci.yml)

Sistema completo para la gestión de recursos humanos desarrollado con ASP.NET Core 10. Implementa una arquitectura de tres capas con dos aplicaciones independientes (Web API y Web MVC) que comparten la misma base de datos a través de una Class Library. 

El proyecto ha sido elevado a un estándar empresarial, incluyendo un rediseño completo de la interfaz con soporte nativo para modo claro/oscuro, accesibilidad WCAG AA, integridad referencial estricta y un conjunto robusto de pruebas unitarias.

## Requisitos Previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Herramienta EF Core: `dotnet tool install -g dotnet-ef` (o `dotnet tool update -g dotnet-ef`)
- [SQL Server Express](https://www.microsoft.com/es-es/sql-server/sql-server-downloads) o LocalDB
- [SQL Server Management Studio (SSMS)](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) (opcional)
- [Visual Studio 2026](https://visualstudio.microsoft.com/es/) (o VS Code con C# Dev Kit) con las cargas de trabajo:
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

### 4. Configurar la clave JWT de la API
La clave no se guarda en el repositorio. En desarrollo va en *user-secrets* (mínimo 32 caracteres):
```bash
dotnet user-secrets set "Jwt:Key" "una-clave-larga-y-aleatoria-de-al-menos-32-caracteres" --project Empleados.Api
```
En producción usa la variable de entorno `Jwt__Key`. Sin esta clave la API no arranca.

Los orígenes permitidos por CORS se configuran en `Cors:AllowedOrigins` de `Empleados.Api/appsettings.json`.

**Fotos de empleados:** la API y el MVC guardan y sirven las fotos (en `/uploads`) desde la misma carpeta, configurada en `Uploads:Path` de ambos `appsettings.json`. Por defecto es `../uploads`, la carpeta `uploads/` en la raíz de la solución, que no se versiona. En producción usa la misma ruta absoluta en las dos apps.

### 5. Aplicar migraciones a la base de datos
```bash
dotnet ef database update --project Empleados.Data --startup-project Empleados.Api
```

### 6. Compilar el proyecto
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

**Datos de demostración (solo desarrollo):** crea la empresa *Distribuidora del Caribe SRL* con 34 empleados, préstamos, incidencias, las nóminas de enero a octubre de 2026 calculadas con el motor real, la bonificación 2025 y dos liquidaciones. Octubre queda a medio ciclo para ver la aprobación. Si ya existe, no se duplica.
```bash
dotnet run --project Empleados.Mvc -- --demo
```

## Credenciales de Prueba

**Acceso Web MVC (Identity):**
| Rol | Usuario | Contraseña |
|-----|---------|------------|
| Admin | admin@mvc.com | Admin123 |
| RRHH | rrhh@mvc.com | Rrhh1234 |

Estos usuarios se crean al arrancar el MVC solo en desarrollo (contraseñas en `SeedUsers` de `Empleados.Mvc/appsettings.Development.json`). Las mismas credenciales sirven para `/api/Auth/login`. Las contraseñas nuevas deben tener al menos 8 caracteres, con mayúscula, minúscula y número.

## Ejecutar Pruebas Unitarias
```bash
dotnet test
```
*Nota: La suite de pruebas incluye 170 tests que validan los casos de nómina de [docs/CASOS_NOMINA.md](docs/CASOS_NOMINA.md), el motor de cálculo, reportes, integridad referencial, notificaciones, reglas de negocio, subida de fotos, aislamiento entre empresas, parámetros legales por vigencia, datos de nómina, historial de movimientos, feriados de la Ley 139-97, vacaciones, incidencias, Salario de Navidad, bonificación y prestaciones laborales.*

**CI:** cada push a `master` y cada PR ejecutan en GitHub Actions (`.github/workflows/ci.yml`) la compilación, los tests y una revisión de que el modelo de EF coincide con las migraciones desde la API y desde el MVC.

## Arquitectura

La lógica de negocio vive en `Empleados.Data/Services` y la comparten la API y el MVC, así las reglas son las mismas en las dos aplicaciones. Los controladores solo traducen el resultado del servicio a HTTP (API) o a vistas (MVC).

El sistema evoluciona hacia una nómina para República Dominicana; el plan por fases está en [docs/PLAN_NOMINA.md](docs/PLAN_NOMINA.md).

### Motor de cálculo de nómina
`Empleados.Nomina` es una librería sin base de datos ni web. Recibe el empleado, el período (quincenal o mensual), las horas extra, las ausencias, los otros ingresos y las deducciones, junto con los parámetros legales vigentes, y devuelve el recibo:
- **Ingresos:** salario, prorrateo, ausencias, horas extra y recargos.
- **Deducciones de ley:** AFP y SFS con topes mensuales, e ISR.
- **Deducciones internas** y **aportes del empleador** (AFP, SFS, SRL e INFOTEP).
- Una **explicación paso a paso** de cada cálculo.

En la pantalla **Nómina → Simulador** y en `POST /api/Nomina/simular` se prueba con los datos reales de un empleado, sin guardar nada.

### Ciclo de nómina
**Nómina → Nóminas** maneja una nómina por empresa, período (quincena o mes) y frecuencia.
- **Borrador / Calculada:** incluye a los empleados activos con esa frecuencia. Se cargan las **novedades** de cada recibo (horas extra, ausencias, bonos, descuentos) y se recalcula las veces que haga falta.
- **Aprobada** (solo Admin): no se puede aprobar si algún recibo tiene errores, por ejemplo si falta la cédula o el NSS. La 2.ª quincena solo se calcula con la 1.ª aprobada, porque usa lo ya retenido. Una nómina aprobada se puede **reabrir** mientras no se haya pagado.
- **Pagada:** ya no se modifica, y baja el saldo de los **préstamos en cuotas** (Nómina → Préstamos).
- **Cerrada:** cierre del período.

**Nómina → Navidad y bonificación** crea la nómina del **Salario de Navidad** (1/12 del salario ordinario del año, sin TSS ni ISR) y reparte la **Bonificación** (10 % de la utilidad con tope de 45 o 60 días), que se paga como ingreso en una nómina regular.

**Nómina → Liquidaciones** calcula las prestaciones al terminar el contrato según la causa (preaviso, cesantía, asistencia económica, vacaciones y Navidad proporcionales, menos los saldos de préstamos). Genera el PDF con el recibo de descargo; al marcarla pagada, el empleado sale de la empresa.

**Nómina → Archivos TSS y banco** genera la autodeterminación mensual de la TSS (AM) desde las nóminas aprobadas, el resumen de aportes e ISR del mes, el CSV de pago al banco y la planilla de personal (base de la DGT-3).

**Portal del empleado:** desde la ficha del empleado, el Admin le da acceso (usuario con su correo y contraseña temporal). El empleado ve sus recibos pagados y su saldo de vacaciones, pide vacaciones o permisos y descarga su carta de trabajo. Al pagar una nómina, cada recibo se envía por correo; configure la sección `Correo` (Host, Puerto, Usuario, Clave, Remitente). Sin `Host`, los correos se guardan como archivos .eml en `../correos`.

**Desplegables:** todos los `<select class="form-select">` usan el componente Smart Select (buscador, carga progresiva, menú que no se recorta). Las reglas están en [docs/ESTANDAR_DESPLEGABLES.md](docs/ESTANDAR_DESPLEGABLES.md).

**Reportes:** 12 reportes de personal (plantilla, por departamento, cumpleaños, contratos por vencer), nómina (costo por departamento, detalle de una nómina, comparativo mensual, acumulados, préstamos) y asistencia (ausentismo, vacaciones pendientes, rotación), con filtros por año, mes, departamento, estado o nómina, y descarga en Excel (.xlsx), CSV o PDF. Cada nómina tiene además un **PDF resumen** con una fila por empleado.

**Nómina → Incidencias** registra faltas, retardos, permisos, licencias y vacaciones (también colectivas). Al aprobarse, las faltas, los retardos y los permisos sin goce se descuentan solos en la nómina del período. Las vacaciones se validan contra el saldo del año de servicio, visible en la ficha del empleado. En **Parámetros legales** se generan los feriados de cualquier año con los traslados de la Ley 139-97.

Cada recibo guarda una copia de los datos del empleado, sus líneas y la explicación del cálculo, y se descarga en **PDF** (uno o todos los de la nómina). Mientras la nómina no esté pagada, el PDF lleva la marca BORRADOR. Las reglas de cálculo y los casos para validarlas están en [docs/CASOS_NOMINA.md](docs/CASOS_NOMINA.md).

### Multiempresa
- Cada departamento, puesto y empleado pertenece a una **empresa** (empleador con su RNC).
- Cada usuario solo ve las empresas que tiene asignadas (pantalla **Empresas**, solo para Admin). Un filtro global de EF Core aplica esta regla a todas las consultas.
- **MVC:** la empresa se elige en el selector de la barra superior.
- **API:** la empresa se envía en la cabecera `X-Empresa-Id`. Si no se envía, se usa la primera empresa asignada; si se pide una no asignada, no se devuelven datos. `GET /api/Empresas` lista las empresas del usuario.

### Parámetros legales
Las tasas de TSS (AFP, SFS y SRL) y sus topes, la escala de ISR, los salarios mínimos por tamaño de empresa, los feriados y los valores del Código de Trabajo se guardan **con fecha de vigencia** (pantalla **Parámetros legales**). Cuando cambia la ley se registra una vigencia nueva y los valores anteriores no se modifican. Los valores iniciales de 2026 salen de fuentes secundarias y deben verificarse con la DGII, la TSS y el Ministerio de Trabajo.

### Datos de nómina del empleado
Documento (cédula o pasaporte), NSS, AFP, ARS, dependientes del SFS, tipo de contrato, frecuencia de pago (por defecto la de la empresa), tipo de salario y cuenta bancaria. Al crear o editar un empleado se valida que el salario no sea menor que el mínimo vigente para el tamaño de la empresa. Cada alta y cada cambio de salario, puesto, departamento, frecuencia o estado queda en el **historial de movimientos**.

## Endpoints de la API

Todos requieren token JWT, salvo el login. Crear, editar o eliminar departamentos, puestos y empleados, y subir fotos, requiere rol **Admin**. Desactivar y despedir requiere **Admin** o **RRHH**.

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/Empresas` | Empresas del usuario y empresa actual |
| GET | `/api/Parametros?fecha=2026-03-01` | Parámetros legales vigentes en una fecha |
| GET | `/api/Parametros/isr?fecha=…` | Escala anual de ISR vigente |
| GET | `/api/Parametros/salarios-minimos` | Salarios mínimos por tamaño de empresa |
| GET | `/api/Parametros/feriados/2026` | Feriados del año |
| POST | `/api/Nomina/simular` | Simula el recibo de un empleado en un período (Admin o RRHH) |
| GET / POST | `/api/Nominas` | Lista nóminas / crea y calcula la nómina de un período |
| GET | `/api/Nominas/5` | Nómina con sus recibos |
| POST | `/api/Nominas/5/calcular` · `aprobar` · `reabrir` · `pagar?fechaPago=` · `cerrar` | Ciclo de la nómina (aprobar, reabrir, pagar y cerrar: solo Admin) |
| PUT | `/api/Nominas/recibos/7/novedades` | Novedades de un recibo (recalcula) |
| GET | `/api/Nominas/recibos/7/pdf` | Recibo en PDF |
| GET / POST | `/api/Prestamos` | Préstamos en cuotas |
| GET / POST | `/api/Incidencias` | Faltas, retardos, permisos, licencias y vacaciones |
| POST / DELETE | `/api/Incidencias/5/aprobar` · `rechazar?motivo=` · `/api/Incidencias/5` | Aprobar, rechazar o eliminar |
| GET | `/api/Incidencias/vacaciones/5` | Saldo de vacaciones del empleado |
| POST | `/api/Incidencias/colectivas` | Vacaciones colectivas |
| GET / POST / DELETE | `/api/Liquidaciones` · `/api/Liquidaciones/5` · `/5/pdf` · `/5/pagar?fechaPago=` | Liquidaciones de prestaciones (pagar: solo Admin) |
| GET | `/api/Archivos/tss/2026/10` · `/txt` · `/api/Archivos/dispersion/5?bancoId=` · `/api/Archivos/planilla` | Autodeterminación TSS, pago al banco y planilla |
| GET | `/api/Reportes/catalogo` · `/api/Reportes/{tipo}?anio=&mes=&departamentoId=&estado=&nominaId=` | Catálogo de reportes y cada reporte en JSON |
| GET | `/api/Nominas/5/resumen-pdf` | PDF resumen de la nómina |
| POST | `/api/Nominas/5/enviar-recibos` | Reenvía los recibos por correo (al pagar se envían solos) |
| POST | `/api/PagosEspeciales/salario-navidad?anio=` | Crea y calcula el Salario de Navidad |
| GET / POST | `/api/PagosEspeciales/bonificacion?anioFiscal=&utilidad=` (`&nominaId=` al aplicar) | Reparto de la bonificación y su aplicación |

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

- **.NET 10** (LTS, soporte hasta noviembre de 2028)
- **ASP.NET Core Web API** y **ASP.NET Core MVC**
- **Entity Framework Core** (Code First, LINQ optimizado con GroupJoin)
- **SQL Server** (LocalDB / Express)
- **ASP.NET Core Identity** y **JWT Bearer**
- **xUnit** con In-Memory Database para pruebas
- **Bootstrap 5** + **Bootstrap Icons** (sin emojis, diseño minimalista)
- **Swagger/OpenAPI** (Swashbuckle) para documentar la API
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
