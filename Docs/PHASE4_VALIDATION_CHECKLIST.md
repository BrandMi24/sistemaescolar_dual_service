# Phase 4 Validation Checklist (Dual + Servicio Social)

## 1. Data Seed
- Start app once and verify at least one active row exists in `operational_program_table` for:
  - `operational_program_Type = PRACTICAS_PROFESIONALES`
  - `operational_program_Type = SERVICIO_SOCIAL`

## 2. Alumno Flow
- Login as a user with role `Alumno` or `Student`.
- Submit all forms in:
  - `Views/Alumno/ModeloDual.cshtml`
  - `Views/Alumno/ServicioSocial.cshtml`
- Validate the explicit acceptance letter upload in both modules:
  - Dual: `DualStudentController.AcceptanceLetter`
  - Servicio Social: `SocialServiceStudentController.AcceptanceLetter`
- Validate weekly report upload with evidence file:
  - Dual: `DualStudentController.WeeklyReports`
  - Servicio Social: `SocialServiceStudentController.WeeklyReports`
- Verify data is persisted to:
  - `operational_studentassignment_table`
  - `operational_organization_table`
  - `operational_document_table`

## 2.1 Alumno E2E Expected Document Types
- Dual should generate documents with types:
  - `RESUME_SPANISH`, `RESUME_ENGLISH`, `IMSS_CERTIFICATE`
  - `PRESENTATION_LETTER`, `ACCEPTANCE_LETTER`, `WEEKLY_REPORT`
- Servicio Social should generate documents with types:
  - `PRESENTATION_LETTER`, `ACCEPTANCE_LETTER`, `WEEKLY_REPORT`
  - (Optional legacy flow) `HOURS_LOG`

## 3. Coordinador Security
- Call coordinator assignment endpoints with a non-coordinator role and verify `403`.
- Call same endpoints with `Coordinador`/`Admin` role and verify success:
  - `GetAssignableStudents`
  - `AssignStudentsToTeacher`
  - `ReassignSupervisor`

## 4. Tutor Security
- Call tutor approval endpoints with a non-tutor role and verify `403`.
- Call same endpoints with `Tutor`/`Teacher`/`Admin` role and verify success:
  - `ApproveDocument`
  - `RejectDocument`
  - `ApproveHours`
  - `FinalApproval`

## 4.1 Asesor Academico Security
- Call asesor endpoints with a non-asesor role and verify `403`.
- Call same endpoints with `AsesorAcademico`/`Asesor`/`Tutor`/`Admin` role and verify success:
  - `AlumnosAsignados`
  - `RevisionDocumentos`
  - `AprobarDocumento`
  - `RechazarDocumento`
  - `Evaluaciones`
  - `GuardarEvaluacion`

## 4.2 Coordinacion E2E (Asignacion Docente)
- Login as `Coordinador` or `Admin`.
- Validate assignment matrix in:
  - `CoordinadorController.Asignaciones`
  - `CoordinadorController.SeguimientoDualEstadias`
- Reassign teacher and verify `operational_studentassignment_TeacherID` changes.

## 5. Build and Runtime
- Run `dotnet build` and confirm success.
- Run app and validate no startup exception in seed execution.
- Validate uploads are created under:
  - `wwwroot/uploads/dual`
  - `wwwroot/uploads/social`
