# Matriz de Permisos por Controlador/Accion

Documento base para pruebas y para mantenimiento futuro del sistema multirol.

## 1. Roles y alias efectivos

El login normaliza nombres de roles para que funcionen con las rutas existentes.

- Admin: `Admin`, `Administrator`, `Master`
- Coordinacion: `Coordinador`, `ServiceLearningCoordinator`
- Direccion: `Director`
- Docencia/Tutoria: `Maestro`, `Teacher`, `Tutor`
- Asesoria academica: `Asesor`, `AsesorAcademico`, `AcademicSupervisor`
- Estudiante: `Alumno`, `Student`
- Operacion administrativa: `Admisiones`, `Preinscripciones`, `Administrativo`, `Enfermeria`

## 2. Matriz actual por controlador/accion

Notas:
- Si un controlador tiene `[Authorize(Roles = ...)]` a nivel clase, aplica a todas sus acciones salvo que una accion tenga otra restriccion.
- Donde se indique "Autenticado", significa cualquier usuario logueado.

### AccountController

| Accion | Metodo | Roles permitidos | Observacion |
|---|---|---|---|
| Login (GET/POST) | GET/POST | Publico | Valida usuario/clave y emite claims multirol |
| Logout | POST | Autenticado | Cierra sesion |

### HomeController

| Accion | Metodo | Roles permitidos | Observacion |
|---|---|---|---|
| Index | GET | Publico | Landing general |
| Privacy | GET | Publico | Informativa |
| AccessDenied | GET | Publico | Vista de acceso denegado |

### AdminController

Clase protegida por: `Admin,Administrator,Master`

| Accion | Metodo | Roles permitidos | Observacion |
|---|---|---|---|
| Index | GET | Admin/Administrator/Master | Dashboard administrativo |

### DashboardController

Clase protegida por: `Director,Admin,Administrator,Master`

| Accion | Metodo | Roles permitidos | Observacion |
|---|---|---|---|
| Index | GET | Director/Admin/Administrator/Master | Vision ejecutiva |
| Aspirantes | GET | Director/Admin/Administrator/Master | Tablero |
| Admisiones | GET | Director/Admin/Administrator/Master | Tablero |
| Tramites | GET | Director/Admin/Administrator/Master | Tablero |
| Diagnostico | GET | Director/Admin/Administrator/Master | Tablero |

### CoordinadorController

Clase protegida por: `Coordinador,ServiceLearningCoordinator,Director,Admin,Administrator,Master`

| Accion | Metodo | Roles permitidos | Observacion |
|---|---|---|---|
| Index, Ciclos, Catalogos | GET | Coordinador/Director/Admin/Master | Navegacion principal |
| GetUsersJson | GET | Coordinador/Director/Admin/Master | Consulta usuarios |
| CreateUser (GET/POST) | GET/POST | Coordinador/Director/Admin/Master | Crea usuario y asigna rol |
| EditUser (GET/POST) | GET/POST | Coordinador/Director/Admin/Master | Edita usuario; reemplaza rol |
| DeleteUser | POST | Coordinador/Director/Admin/Master | Soft delete usuario y user roles |
| Modulo alumnos/docentes/grupos/carreras (Get/Create/Edit/Delete) | GET/POST | Coordinador/Director/Admin/Master | Gestion academica |
| Asignaciones (GetAssignableStudents, AssignStudentsToTeacher, ReassignSupervisor) | GET/POST | Coordinador/ServiceLearningCoordinator/Admin/Administrator | Tiene restriccion puntual adicional |
| GetHistorial | GET | Coordinador/ServiceLearningCoordinator/Admin/Administrator | Auditoria |

### TutorController

Clase protegida por: `Tutor,Teacher,Maestro,AcademicSupervisor,AsesorAcademico,Asesor,Admin,Administrator,Master`

| Accion | Metodo | Roles permitidos | Observacion |
|---|---|---|---|
| Index, Asistencia, Entrevista, Seguimiento, Tramites | GET | Roles de clase | Operacion tutorial |
| ApproveDocument / RejectDocument / ApproveHours / FinalApproval / DownloadOperationalDocument | POST/GET | Tutor/Teacher/AcademicSupervisor/Admin/Administrator | Restriccion puntual adicional en accion |

### AsesorAcademicoController

Clase protegida por: `AsesorAcademico,Asesor,AcademicSupervisor,Tutor,Teacher,Maestro,Admin,Administrator,Master`

| Accion | Metodo | Roles permitidos | Observacion |
|---|---|---|---|
| AlumnosAsignados | GET | Roles de clase | Bandeja asesor |
| RevisionDocumentos | GET | Roles de clase | Revision documental |
| AprobarDocumento / RechazarDocumento | POST | Roles de clase | Validaciones de estado |
| Evaluaciones | GET | Roles de clase | Evaluacion academica |
| GuardarEvaluacion | POST | Roles de clase | Guarda calificacion |
| Index | GET | Roles de clase | Redirige a AlumnosAsignados |

### AlumnoController

Clase protegida por: `Alumno,Student`

| Accion | Metodo | Roles permitidos | Observacion |
|---|---|---|---|
| Index, Entrevista, Calificaciones, Asistencias, ModeloDual, ServicioSocial, Tramites | GET | Alumno/Student | Portal alumno |

### DualStudentController

Clase protegida por: `Alumno,Student`

| Accion | Metodo | Roles permitidos | Observacion |
|---|---|---|---|
| StudentInfo, PlacementSupport, Documents, PresentationLetter, AcceptanceLetter, SaveBusinessAdvisor, WeeklyReports | GET/POST | Alumno/Student | Flujo Dual con carta de aceptacion y reportes |

### SocialServiceStudentController

Clase protegida por: `Alumno,Student`

| Accion | Metodo | Roles permitidos | Observacion |
|---|---|---|---|
| StudentInfo, PlacementSupport, PresentationLetter, AcceptanceLetter, WeeklyReports, HoursLog | GET/POST | Alumno/Student | Flujo Servicio Social con carga de evidencias |

### AdmisionesController

Clase protegida por: `Admisiones,Preinscripciones,Administrativo,Coordinador,Director,Admin,Administrator,Master`

| Accion | Metodo | Roles permitidos | Observacion |
|---|---|---|---|
| Index | GET | Roles de clase | Bandeja de admisiones |

### PreinscripcionesController

Clase protegida por: `Preinscripciones,Admisiones,Administrativo,Coordinador,Director,Admin,Administrator,Master`

| Accion | Metodo | Roles permitidos | Observacion |
|---|---|---|---|
| Index, Details | GET | Roles de clase | Gestion preinscripciones |
| Create (GET/POST) | GET/POST | Roles de clase | Alta |
| Edit (GET/POST) | GET/POST | Roles de clase | Edicion |
| Delete (GET/POST) | GET/POST | Roles de clase | Soft delete |

### Controladores de apoyo

| Controlador | Roles permitidos | Accion |
|---|---|---|
| AsistenciaController | Tutor/Teacher/Maestro/Coordinador/Admin/Administrator/Master | Index |
| CalificacionesController | Tutor/Teacher/Maestro/Coordinador/Admin/Administrator/Master | Index |
| EntrevistaController | Tutor/Teacher/Maestro/AsesorAcademico/Asesor/Coordinador/Director/Admin/Administrator/Master | Index |
| EstadiasController | Tutor/Teacher/Maestro/AsesorAcademico/Asesor/Coordinador/Admin/Administrator/Master | Index |
| KardexController | Alumno/Student/Tutor/Teacher/Maestro/Coordinador/Admin/Administrator/Master | Index |
| TramitesController | Autenticado (clase) | Index, MisTramites, Gestion y operacion de tramite |

## 3. Estado actual de administracion de roles y usuarios

### 3.1 Puede Admin crear usuarios y asignar roles?

Si, indirectamente en el modulo de Coordinador (porque Admin entra al CoordinadorController por autorizacion de clase):
- Crear usuario: `CreateUser`
- Editar usuario: `EditUser`
- Eliminar usuario: `DeleteUser`

### 3.2 Puede asignar multiples roles desde UI?

No completamente.
- Estado actual: el formulario `CreateUserViewModel` usa un solo campo `Role`.
- En `EditUser`, primero elimina roles actuales y despues inserta solo un rol.
- Multirol hoy se maneja por seeder o SQL.

### 3.3 Puede crear/editar/eliminar roles desde UI?

No hay modulo UI dedicado a CRUD de roles en este momento.
- Si se requiere alta/edicion de rol, hoy se hace via SQL o seeder (`IdentityDemoSeedService`).

## 4. Plan de pruebas por rol

### 4.1 Casos base por rol

- Admin/Administrator/Master:
  - Debe entrar a `/Admin`, `/Coordinador`, `/Dashboard`.
  - Debe poder acceder a gestion de usuarios.
- Director:
  - Debe entrar a `/Dashboard` y `/Coordinador`.
  - No debe acceder a rutas exclusivas de alumno.
- Coordinador/ServiceLearningCoordinator:
  - Debe entrar a `/Coordinador`.
  - Debe operar asignaciones y catalogos.
- Tutor/Teacher/Maestro:
  - Debe entrar a `/Tutor` y `/AsesorAcademico`.
- Asesor/AsesorAcademico/AcademicSupervisor:
  - Debe entrar a `/AsesorAcademico`.
- Alumno/Student:
  - Debe entrar a `/Alumno`, `/DualStudent`, `/SocialServiceStudent`.
- Admisiones/Preinscripciones/Administrativo:
  - Debe entrar a `/Admisiones` y `/Preinscripciones`.

### 4.2 Pruebas de acceso denegado

Con cada rol, intentar abrir por URL directa un modulo que no corresponda.
Resultado esperado:
- HTTP 302 a `/Home/AccessDenied` (flujo cookie)
- Se muestra vista de acceso denegado.

## 5. Guia para agregar nuevos modulos/vistas de forma correcta

Cuando agreguen un controlador o vista nueva, sigan este flujo:

1. Definir roles objetivo del modulo
- Elegir roles canonicos (ejemplo: `Coordinador`, `Admin`, `Alumno`).

2. Proteger controlador/acciones
- Poner `[Authorize(Roles = "...")]` en la clase o accion.
- Evitar dejar modulo publico por omision.

3. Ajustar alias de login si aparece un nombre nuevo
- Si el rol nuevo puede venir con nombre alterno, agregar alias en `GetRoleAliases` de `AccountController`.

4. Agregar navegacion condicional en layout
- Mostrar enlaces solo si `User.IsInRole(...)`.
- No usar enlaces fijos para todos los usuarios.

5. Definir ruta default (si aplica)
- Si el nuevo rol debe aterrizar en modulo especifico tras login, actualizar `ResolveDefaultRoute` en `AccountController`.

6. Seed y datos de prueba
- Agregar rol/usuarios demo en `IdentityDemoSeedService`.
- Crear al menos una cuenta con ese rol para QA.

7. Checklist de QA
- Usuario con rol permitido: ve menu + entra ruta.
- Usuario sin rol: no ve menu + acceso denegado por URL directa.

## 6. Recomendacion para siguiente iteracion

Para tener administracion real de multirol en UI:
- Cambiar `CreateUserViewModel.Role` por `List<int> Roles`.
- En Create/Edit, insertar multiples filas en `management_userrole_table`.
- Mantener `management_user_RoleID` como rol primario (opcional) solo para compatibilidad heredada.
