# EmployeeAPI — GoodRabbit

API REST desarrollada en **.NET 8** con **MongoDB** que cubre dos dominios de negocio relacionados con la administración del capital humano:

- **Gestión de personas:** empleados, departamentos y posiciones (creados de forma implícita al dar de alta un empleado).
- **Control de asistencia (Timekeeper):** dispositivos de marcación, enrolamiento de empleados por PIN, catálogo de tipos de marca y registro/consulta de marcaciones.

Ambos dominios comparten la misma base de datos y el mismo mecanismo de autenticación, pero son conceptualmente independientes: el dominio de personas administra "quién es quién" en la organización, mientras que Timekeeper registra "quién marcó, dónde y cuándo".

> Este documento es la guía operativa del proyecto (cómo instalarlo, ejecutarlo y usarlo). El análisis de arquitectura, modelo de datos y diagramas de secuencia está en [`ANALISIS_TECNICO.md`](./ANALISIS_TECNICO.md).

## Índice

- [Requisitos previos](#requisitos-previos)
- [Instalación y ejecución local](#instalación-y-ejecución-local-sin-docker)
- [Ejecución con Docker Compose](#ejecución-con-docker-compose-recomendado)
- [Configuración y variables de entorno](#configuración-y-variables-de-entorno)
- [Autenticación](#autenticación)
- [Catálogo de endpoints](#catálogo-de-endpoints)
- [Flujo completo de ejemplo: dispositivo → enrolamiento → marcación → consulta](#flujo-completo-de-ejemplo-dispositivo--enrolamiento--marcación--consulta)
- [Pruebas automatizadas](#pruebas-automatizadas)
- [Interfaz web (opcional)](#interfaz-web-opcional)
- [Consideraciones y limitaciones conocidas](#consideraciones-y-limitaciones-conocidas)

## Requisitos previos

- [.NET SDK 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Una instancia de MongoDB corriendo en el puerto `27017`, ya sea:
  - vía [Docker Desktop](https://www.docker.com/products/docker-desktop/) (recomendado, no requiere instalar Mongo por separado), o
  - vía [MongoDB Community Server](https://www.mongodb.com/try/download/community) instalado directamente.

## Instalación y ejecución local (sin Docker)

1. Clona el repositorio y entra a la carpeta del proyecto.
2. Asegúrate de tener una instancia de MongoDB accesible en `mongodb://localhost:27017/`. Si tu Mongo corre en otro host o puerto, ajusta `MongoDB:ConnectionURI` en `appsettings.Development.json` o usa la variable de entorno correspondiente (ver [sección de configuración](#configuración-y-variables-de-entorno)).
3. Restaura las dependencias:
   ```bash
   dotnet restore
   ```
4. Ejecuta el proyecto:
   ```bash
   dotnet run
   ```
5. La API queda disponible en `http://localhost:5146` (perfil `http` de `launchSettings.json`). Swagger se abre automáticamente en `http://localhost:5146/swagger`.

## Ejecución con Docker Compose (recomendado)

El repositorio incluye un `docker-compose.yml` que levanta tres servicios: la API, MongoDB y Mongo Express (interfaz web para inspeccionar la base de datos).

```bash
docker compose up --build
```

- **API:** `http://localhost:8080` (Swagger en `http://localhost:8080/swagger/index.html`)
- **MongoDB:** expuesto en `localhost:27017` (por si quieres conectarte con Compass u otra herramienta)
- **Mongo Express:** `http://localhost:8081` (usuario/clave por defecto de la imagen: `admin` / `pass`)

Para detener y limpiar los contenedores:
```bash
docker compose down
```
Para además borrar los datos persistidos de Mongo:
```bash
docker compose down -v
```

## Configuración y variables de entorno

La configuración vive en `appsettings.json` / `appsettings.Development.json` y se puede sobreescribir con variables de entorno usando la notación `__` (doble guion bajo) que usa la configuración estándar de ASP.NET Core.

| Configuración | Clave en `appsettings.json` | Variable de entorno equivalente | Valor de ejemplo |
|---|---|---|---|
| Cadena de conexión a MongoDB | `MongoDB:ConnectionURI` | `MongoDB__ConnectionURI` | `mongodb://localhost:27017/` |
| Base de datos | `MongoDB:DatabaseName` | `MongoDB__DatabaseName` | `sample_employee` |
| Secreto para firmar el JWT | `Jwt:SecretKey` | `Jwt__SecretKey` | *(mínimo 32 caracteres; ver nota abajo)* |
| Horas de expiración del token | `Jwt:ExpirationHours` | `Jwt__ExpirationHours` | `12` |

Ejemplo de override al ejecutar localmente (PowerShell):
```powershell
$env:Jwt__SecretKey = "una-clave-de-al-menos-32-caracteres-segura"
$env:MongoDB__ConnectionURI = "mongodb://localhost:27017/"
dotnet run
```

Ejemplo de override en `docker-compose.yml` (agregar bajo `environment:` del servicio `api`):
```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - Jwt__SecretKey=una-clave-de-al-menos-32-caracteres-segura
```

> **Importante:** el valor `"dev-only-secret-key-change-me-32b!"` que trae `appsettings.json` es un valor de ejemplo para desarrollo local, **no debe usarse en ningún ambiente real**, y ningún secreto real debe agregarse al repositorio (ver [Restricciones](#consideraciones-y-limitaciones-conocidas)).

## Autenticación

1. La API valida credenciales contra la colección de usuarios en MongoDB. Si no existe ningún usuario aún, se crea automáticamente uno por defecto (`admin` / `admin`) la primera vez que se llama a login.
2. `POST /api/auth/login` con `{ "username": "admin", "password": "admin" }` devuelve un JWT firmado con `Jwt:SecretKey`.
3. El token debe enviarse en el header `Authorization: Bearer <token>` en todas las llamadas subsecuentes.
4. El token es válido por `Jwt:ExpirationHours` horas (12 por defecto) desde su emisión. Si vuelves a llamar a login con un token vigente, la API reutiliza el mismo token en vez de generar uno nuevo.
5. Rutas exceptuadas de autenticación: `POST /api/auth/login`, `GET /health`, y `/swagger/*`.

## Catálogo de endpoints

Todas las rutas (salvo las exceptuadas arriba) requieren el header `Authorization: Bearer <token>`.

### Auth

| Método | Ruta | Body | Respuesta | Errores |
|---|---|---|---|---|
| POST | `/api/auth/login` | `{ "username": string, "password": string }` | `200` `{ username, token, expiresAtUtc }` | `401` credenciales inválidas |

### Empleados

| Método | Ruta | Parámetros / Body | Respuesta | Errores |
|---|---|---|---|---|
| GET | `/api/employee` | Query opcional: `departmentName`, `positionName` | `200` `Employee[]` | — |
| GET | `/api/employee/{id}` | `id` (24 caracteres, ObjectId) | `200` `Employee` | `404` no existe |
| POST | `/api/employee` | `{ Name, Email, Dni?, Department, Position }` | `201` `Employee` creado | `400` si se envía `Id`, o si `Name`/`Email`/`Department`/`Position` no cumplen el formato requerido (ver nota), o si `Dni` no tiene el formato `12345678-9`; `409` si el email ya existe |
| PUT | `/api/employee/{id}` | `Employee` completo | `200` `Employee` actualizado | `400` si el body no cumple las validaciones de formato (mismas que en `POST`); `404` no existe |
| PATCH | `/api/employee/{id}` | `{ Name?, Email?, Department? }` (campos parciales) | `200` `Employee` actualizado | `400` si algún campo enviado no cumple el formato (los campos omitidos no se validan); `404` no existe |
| DELETE | `/api/employee/{id}` | — | `204` | `404` no existe |
| GET | `/api/employee/departments` | — | `200` `Department[]` | — |
| GET | `/api/employee/departments/{departmentId}/positions` | — | `200` `Position[]` | — |

> **Formato requerido:** `Name`/`Department`/`Position` entre 2 y 100 caracteres; `Email` con formato de correo válido (máx. 150 caracteres); `Dni` (opcional) con formato `12345678-9` si se envía. El DNI sigue siendo opcional porque un empleado también puede identificarse por `Id` o `Pin` al marcar (ver flujo de marcación más abajo).

### Dispositivos

| Método | Ruta | Body | Respuesta | Errores |
|---|---|---|---|---|
| GET | `/api/device` | — | `200` `Device[]` | — |
| GET | `/api/device/{id}` | — | `200` `Device` | `404` no existe |
| POST | `/api/device` | `{ Name, Location, Timezone }` (`Timezone` en formato IANA, ej. `America/Santiago`) | `201` `Device` creado | `400` si se envía `Id`; `409` si el nombre ya existe |
| PUT | `/api/device/{id}` | `Device` completo | `200` `Device` actualizado | `404` no existe |
| DELETE | `/api/device/{id}` | — | `204` | `404` no existe |

### Enrolamientos

| Método | Ruta | Parámetros / Body | Respuesta | Errores |
|---|---|---|---|---|
| GET | `/api/enrollment` | Query opcional: `employeeId`, `deviceId` | `200` `Enrollment[]` | — |
| GET | `/api/enrollment/{id}` | — | `200` `Enrollment` | `404` no existe |
| POST | `/api/enrollment` | `{ Employee_Id, Device_Id?, Pin, Active? }` (`Pin`: 4 a 10 dígitos) | `201` `Enrollment` creado | `400` si se envía `Id`; `404` si el empleado o dispositivo no existen; `409` si el PIN ya está en uso en ese alcance (dispositivo específico o global) |
| DELETE | `/api/enrollment/{id}` | — | `204` | `404` no existe |

### Marcaciones (Timekeeper)

| Método | Ruta | Parámetros / Body | Respuesta | Errores |
|---|---|---|---|---|
| GET | `/api/punch` | Query opcional: `employeeId`, `deviceId`, `from`, `to` | `200` `Punch[]` ordenado por fecha | `400` si `from` > `to` |
| GET | `/api/punch/{id}` | — | `200` `Punch` | `404` no existe |
| GET | `/api/punch/types` | — | `200` `PunchType[]` (sembrados por defecto: `IN`, `OUT`, `BREAK_IN`, `BREAK_OUT`) | — |
| POST | `/api/punch/types` | `{ Code, Name, Description? }` | `201` `PunchType` creado | `400` si se envía `Id`; `409` si el código ya existe |
| POST | `/api/punch` | `{ Device_Id, PunchType, Punch_Dtm?, Employee_Id? \| Dni? \| Pin? }` | `201` `Punch` creado con `Status: "VALID"` | `400` dispositivo/tipo de marca inválido, empleado no identificable, o fecha futura; `404` dispositivo no encontrado; `409` marca duplicada (mismo empleado, dispositivo y fecha) |

> El empleado que marca se identifica con **uno** de estos tres datos, en este orden de prioridad: `Employee_Id` directo, `Dni`, o `Pin` (buscando el enrolamiento activo en ese dispositivo). Si no se puede resolver con ninguno, se rechaza con `400`.
>
> Toda respuesta que incluya un `Punch` trae además `punch_Dtm_Local`: la hora local de la marca, calculada a partir de `punch_Dtm` (UTC) y `timezone`. Es un campo calculado, no se guarda en la base — si el dispositivo no informó `timezone`, o la zona no se pudo resolver, este campo viene `null`.

## Flujo completo de ejemplo: dispositivo → enrolamiento → marcación → consulta

Todos los ejemplos asumen que ya tienes un token válido en `$TOKEN` (obtenido de `POST /api/auth/login`) y lo envías como `Authorization: Bearer $TOKEN`.

**1. Login**
```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'
```
```json
{ "username": "admin", "token": "eyJhbGciOi...", "expiresAtUtc": "2026-09-09T14:00:00Z" }
```

**2. Crear un dispositivo**
```bash
curl -X POST http://localhost:8080/api/device \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Reloj Recepcion","location":"Casa Matriz","timezone":"America/Santiago"}'
```
```json
{ "id": "66df0a1b2c3d4e5f60718293", "name": "Reloj Recepcion", "location": "Casa Matriz", "timezone": "America/Santiago" }
```

**3. Crear un empleado** *(si aún no existe)*
```bash
curl -X POST http://localhost:8080/api/employee \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Ana Pérez","email":"ana.perez@example.com","dni":"12345678-9","department":"Operaciones","position":"Analista"}'
```
```json
{ "id": "66df0a1b2c3d4e5f60718294", "name": "Ana Pérez", "email": "ana.perez@example.com", "dni": "12345678-9", "department": "Operaciones", "position": "Analista", "department_Id": "...", "position_Id": "..." }
```

**4. Enrolar al empleado en el dispositivo con un PIN**
```bash
curl -X POST http://localhost:8080/api/enrollment \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"employee_Id":"66df0a1b2c3d4e5f60718294","device_Id":"66df0a1b2c3d4e5f60718293","pin":"4821"}'
```
```json
{ "id": "66df0a1b2c3d4e5f60718295", "employee_Id": "66df0a1b2c3d4e5f60718294", "device_Id": "66df0a1b2c3d4e5f60718293", "pin": "4821", "active": true }
```

**5. Registrar una marcación usando el PIN (simula lo que enviaría el reloj)**
```bash
curl -X POST http://localhost:8080/api/punch \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"device_Id":"66df0a1b2c3d4e5f60718293","punchType":"IN","pin":"4821"}'
```
```json
{ "id": "66df0a1b2c3d4e5f60718296", "device_Id": "66df0a1b2c3d4e5f60718293", "employee_Id": "66df0a1b2c3d4e5f60718294", "punchType": "IN", "punch_Dtm": "2026-09-09T13:05:00Z", "punch_Dtm_Local": "2026-09-09T10:05:00", "timezone": "America/Santiago", "status": "VALID" }
```

**6. Consultar las marcaciones del empleado**
```bash
curl "http://localhost:8080/api/punch?employeeId=66df0a1b2c3d4e5f60718294" \
  -H "Authorization: Bearer $TOKEN"
```
```json
[ { "id": "66df0a1b2c3d4e5f60718296", "punchType": "IN", "punch_Dtm": "2026-09-09T13:05:00Z", "punch_Dtm_Local": "2026-09-09T10:05:00", "status": "VALID", "...": "..." } ]
```

## Pruebas automatizadas

El repositorio incluye un proyecto xUnit en `Tests/` con 40 pruebas que cubren: generación y validación de tokens JWT (incluyendo que `tokenExpiration` ahora guarda la fecha completa, no solo la hora del día), validaciones de modelos (`Punch`, `Enrollment`, `Device`, `Employee`, `PatchEmployee` — formato de email, DNI, campos obligatorios y actualizaciones parciales), que la respuesta de login no exponga la contraseña, el hasher de contraseñas (`PasswordHasher`: verificación correcta, contraseña incorrecta, salts distintos por hash, que el hash nunca contiene la contraseña en texto plano, y manejo de valores con formato inválido/legado), y la conversión de zona horaria (`TimeZoneHelper`: conversión correcta con zonas horarias reales, manejo de zonas inválidas o ausentes sin lanzar excepciones). No requieren conexión a MongoDB.

```bash
dotnet test Tests/EmployeeAPI.Tests.csproj
```

Resultado esperado:
```
Aprobado! - Con error: 0, Superado: 40, Omitido: 0, Total: 40
```

## Interfaz web (opcional)

El repositorio incluye una interfaz web mínima en `frontend/` (punto 5 del challenge, actividad opcional — no reemplaza las actividades obligatorias de arriba). Permite iniciar sesión, consultar y filtrar empleados por departamento/posición o por texto libre (nombre, email, DNI), y — como extensión valorada — consultar marcaciones filtrables por empleado, dispositivo, tipo de marca y rango de fechas. Muestra estados de carga, error y vacío en cada consulta.

Está hecha en HTML/CSS/JavaScript vanilla con módulos ES6 nativos (sin build ni dependencias de Node), separada en capas independientes: `api-client.js` (infraestructura HTTP), `session.js` (dominio de la sesión/token), `view.js` (presentación/DOM) y `app.js` (orquestación, la única capa que conoce a las otras tres).

Para probarla:
1. Con la API corriendo (Docker Compose o `dotnet run`), abre `frontend/index.html` con un servidor estático local — por ejemplo, la extensión "Live Server" de VS Code (clic derecho sobre el archivo → "Open with Live Server"). No la abras con doble clic directo: los módulos ES6 necesitan servirse por HTTP, no funcionan como archivo local (`file://`).
2. Inicia sesión con `admin` / `admin`. Si tu API no corre en `http://localhost:8080`, ajusta el campo "URL de la API" en la pantalla de login antes de entrar.

El token se guarda en `sessionStorage` (no `localStorage`) y se limpia automáticamente al cerrar sesión o si el servidor responde `401`.

## Consideraciones y limitaciones conocidas

- ~~Contraseñas en texto plano~~ **[Corregido]** las contraseñas ahora se almacenan con hash PBKDF2 + salt (ver sección 8 de `ANALISIS_TECNICO.md` para el detalle de la mejora implementada y su evidencia de validación).
- **CORS abierto:** el middleware de autenticación refleja cualquier `Origin` como permitido con `credentials: true`, lo cual es muy permisivo para producción.
- ~~`PATCH /api/employee/{id}` no sincronizaba `Department_Id` al cambiar el departamento~~ **[Corregido]** ahora resuelve `Department_Id` igual que `POST` (ver sección 8.4 de `ANALISIS_TECNICO.md`). Queda como limitación conocida: `Position`/`Position_Id` no se pueden editar por este medio, por lo que pueden quedar inconsistentes con un `Department` cambiado.
- **Sin paginación:** los listados de empleados y marcaciones devuelven todos los resultados sin límite, lo que puede ser un problema de rendimiento con datasets grandes.
- **Condición de carrera en el email único:** el controlador valida duplicados antes de insertar, pero si dos solicitudes llegan casi simultáneamente con el mismo email, el índice único de Mongo puede rechazar el insert con una excepción no controlada (`500`) en lugar de un `409`.
- **No subir secretos reales:** los valores de `Jwt:SecretKey` y cadenas de conexión en este repositorio son de ejemplo para desarrollo local. Nunca reemplaces estos valores con credenciales reales dentro de archivos versionados; usa variables de entorno o un gestor de secretos.

Para el detalle de arquitectura, modelo de datos y diagramas, revisa [`ANALISIS_TECNICO.md`](./ANALISIS_TECNICO.md).
