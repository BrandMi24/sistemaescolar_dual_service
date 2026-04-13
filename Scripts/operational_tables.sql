/* ============================================================
   Complete database schema — management_* + operational_* tables
   Servicio Social & Practicas Profesionales
   Database: TITESTUTTN2026   |   SQL Server 2022
   ============================================================ */
USE [TITESTUTTN2026]
GO

/* ============================================================
   MANAGEMENT TABLES
   Creation order respects FK dependencies:
   person → career → group → role → permission
   → user → student → teacher
   → userrole → rolepermission → usercareer
   → studentcareer_history → studentgroup_history
   ============================================================ */

/* ============================================================
   1. management_person_table
      Base personal data shared by students, teachers, and users
   ============================================================ */
CREATE TABLE [dbo].[management_person_table](
    [management_person_ID]               INT IDENTITY(1,1) NOT NULL,
    [management_person_FirstName]        NVARCHAR(100)     NOT NULL,
    [management_person_LastNamePaternal] NVARCHAR(100)     NOT NULL,
    [management_person_LastNameMaternal] NVARCHAR(100)     NULL,
    [management_person_BirthDate]        DATE              NULL,
    [management_person_Gender]           NVARCHAR(20)      NULL,
    [management_person_CURP]             NVARCHAR(18)      NULL,
    [management_person_Email]            NVARCHAR(150)     NULL,
    [management_person_Phone]            NVARCHAR(30)      NULL,
    [management_person_status]           BIT               NOT NULL,
    [management_person_createdDate]      DATETIME          NOT NULL,
    CONSTRAINT [PK_management_person] PRIMARY KEY CLUSTERED ([management_person_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_person_table] ADD CONSTRAINT [DF_management_person_status]      DEFAULT ((1))       FOR [management_person_status]
GO
ALTER TABLE [dbo].[management_person_table] ADD CONSTRAINT [DF_management_person_createdDate] DEFAULT (GETDATE()) FOR [management_person_createdDate]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_management_person_CURP] ON [dbo].[management_person_table]
(
    [management_person_CURP] ASC
) WHERE ([management_person_CURP] IS NOT NULL) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_management_person_Email] ON [dbo].[management_person_table]
(
    [management_person_Email] ASC
) WHERE ([management_person_Email] IS NOT NULL) ON [PRIMARY]
GO

/* ============================================================
   2. management_career_table
      Academic careers / programs offered by the institution
   ============================================================ */
CREATE TABLE [dbo].[management_career_table](
    [management_career_ID]          INT IDENTITY(1,1) NOT NULL,
    [management_career_Code]        NVARCHAR(30)      NOT NULL,
    [management_career_Name]        NVARCHAR(150)     NOT NULL,
    [management_career_status]      BIT               NOT NULL,
    [management_career_createdDate] DATETIME          NOT NULL,
    CONSTRAINT [PK_management_career] PRIMARY KEY CLUSTERED ([management_career_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_career_table] ADD CONSTRAINT [DF_management_career_status]      DEFAULT ((1))       FOR [management_career_status]
GO
ALTER TABLE [dbo].[management_career_table] ADD CONSTRAINT [DF_management_career_createdDate] DEFAULT (GETDATE()) FOR [management_career_createdDate]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_management_career_Code] ON [dbo].[management_career_table]
(
    [management_career_Code] ASC
) ON [PRIMARY]
GO

/* ============================================================
   3. management_group_table
      Student groups / sections within a career
   ============================================================ */
CREATE TABLE [dbo].[management_group_table](
    [management_group_ID]          INT IDENTITY(1,1) NOT NULL,
    [management_group_CareerID]    INT               NULL,
    [management_group_Code]        NVARCHAR(30)      NOT NULL,
    [management_group_Name]        NVARCHAR(100)     NULL,
    [management_group_Shift]       NVARCHAR(20)      NULL,
    [management_group_status]      BIT               NOT NULL,
    [management_group_createdDate] DATETIME          NOT NULL,
    CONSTRAINT [PK_management_group] PRIMARY KEY CLUSTERED ([management_group_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_group_table] ADD CONSTRAINT [DF_management_group_status]      DEFAULT ((1))       FOR [management_group_status]
GO
ALTER TABLE [dbo].[management_group_table] ADD CONSTRAINT [DF_management_group_createdDate] DEFAULT (GETDATE()) FOR [management_group_createdDate]
GO

ALTER TABLE [dbo].[management_group_table] WITH CHECK ADD CONSTRAINT [FK_management_group_career]
    FOREIGN KEY ([management_group_CareerID]) REFERENCES [dbo].[management_career_table] ([management_career_ID]) ON DELETE SET NULL
GO
ALTER TABLE [dbo].[management_group_table] CHECK CONSTRAINT [FK_management_group_career]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_management_group_Code] ON [dbo].[management_group_table]
(
    [management_group_Code] ASC
) ON [PRIMARY]
GO

/* ============================================================
   4. management_role_table
      Application roles (Admin, Teacher, Student, etc.)
   ============================================================ */
CREATE TABLE [dbo].[management_role_table](
    [management_role_ID]          INT IDENTITY(1,1) NOT NULL,
    [management_role_Name]        NVARCHAR(80)      NOT NULL,
    [management_role_Description] NVARCHAR(200)     NULL,
    [management_role_status]      BIT               NOT NULL,
    [management_role_createdDate] DATETIME          NOT NULL,
    CONSTRAINT [PK_management_role] PRIMARY KEY CLUSTERED ([management_role_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_role_table] ADD CONSTRAINT [DF_management_role_status]      DEFAULT ((1))       FOR [management_role_status]
GO
ALTER TABLE [dbo].[management_role_table] ADD CONSTRAINT [DF_management_role_createdDate] DEFAULT (GETDATE()) FOR [management_role_createdDate]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_management_role_Name] ON [dbo].[management_role_table]
(
    [management_role_Name] ASC
) ON [PRIMARY]
GO

/* ============================================================
   5. management_permission_table
      Fine-grained permission keys assigned to roles
   ============================================================ */
CREATE TABLE [dbo].[management_permission_table](
    [management_permission_ID]          INT IDENTITY(1,1) NOT NULL,
    [management_permission_Key]         NVARCHAR(120)     NOT NULL,
    [management_permission_Description] NVARCHAR(250)     NULL,
    [management_permission_status]      BIT               NOT NULL,
    [management_permission_createdDate] DATETIME          NOT NULL,
    CONSTRAINT [PK_management_permission] PRIMARY KEY CLUSTERED ([management_permission_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_permission_table] ADD CONSTRAINT [DF_management_permission_status]      DEFAULT ((1))       FOR [management_permission_status]
GO
ALTER TABLE [dbo].[management_permission_table] ADD CONSTRAINT [DF_management_permission_createdDate] DEFAULT (GETDATE()) FOR [management_permission_createdDate]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_management_permission_Key] ON [dbo].[management_permission_table]
(
    [management_permission_Key] ASC
) ON [PRIMARY]
GO

/* ============================================================
   6. management_user_table
      System accounts — linked optionally to a person record
   ============================================================ */
CREATE TABLE [dbo].[management_user_table](
    [management_user_ID]            INT IDENTITY(1,1) NOT NULL,
    [management_user_PersonID]      INT               NULL,
    [management_user_Username]      NVARCHAR(80)      NOT NULL,
    [management_user_Email]         NVARCHAR(150)     NULL,
    [management_user_PasswordHash]  NVARCHAR(500)     NOT NULL,
    [management_user_IsLocked]      BIT               NOT NULL,
    [management_user_LockReason]    NVARCHAR(200)     NULL,
    [management_user_LastLoginDate] DATETIME          NULL,
    [management_user_status]        BIT               NOT NULL,
    [management_user_createdDate]   DATETIME          NOT NULL,
    CONSTRAINT [PK_management_user] PRIMARY KEY CLUSTERED ([management_user_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_user_table] ADD CONSTRAINT [DF_management_user_IsLocked]    DEFAULT ((0))       FOR [management_user_IsLocked]
GO
ALTER TABLE [dbo].[management_user_table] ADD CONSTRAINT [DF_management_user_status]      DEFAULT ((1))       FOR [management_user_status]
GO
ALTER TABLE [dbo].[management_user_table] ADD CONSTRAINT [DF_management_user_createdDate] DEFAULT (GETDATE()) FOR [management_user_createdDate]
GO

ALTER TABLE [dbo].[management_user_table] WITH CHECK ADD CONSTRAINT [FK_management_user_person]
    FOREIGN KEY ([management_user_PersonID]) REFERENCES [dbo].[management_person_table] ([management_person_ID]) ON DELETE SET NULL
GO
ALTER TABLE [dbo].[management_user_table] CHECK CONSTRAINT [FK_management_user_person]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_management_user_Username] ON [dbo].[management_user_table]
(
    [management_user_Username] ASC
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_management_user_Email] ON [dbo].[management_user_table]
(
    [management_user_Email] ASC
) WHERE ([management_user_Email] IS NOT NULL) ON [PRIMARY]
GO

/* ============================================================
   7. management_student_table
      Students enrolled in a career and group
   ============================================================ */
CREATE TABLE [dbo].[management_student_table](
    [management_student_ID]              INT IDENTITY(1,1) NOT NULL,
    [management_student_PersonID]        INT               NOT NULL,
    [management_student_CareerID]        INT               NULL,
    [management_student_GroupID]         INT               NULL,
    [management_student_Matricula]       NVARCHAR(30)      NULL,
    [management_student_EnrollmentFolio] NVARCHAR(30)      NULL,
    [management_student_StatusCode]      NVARCHAR(30)      NOT NULL,
    [management_student_status]          BIT               NOT NULL,
    [management_student_createdDate]     DATETIME          NOT NULL,
    CONSTRAINT [PK_management_student] PRIMARY KEY CLUSTERED ([management_student_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_student_table] ADD CONSTRAINT [DF_management_student_StatusCode]  DEFAULT ('PREINSCRITO') FOR [management_student_StatusCode]
GO
ALTER TABLE [dbo].[management_student_table] ADD CONSTRAINT [DF_management_student_status]      DEFAULT ((1))          FOR [management_student_status]
GO
ALTER TABLE [dbo].[management_student_table] ADD CONSTRAINT [DF_management_student_createdDate] DEFAULT (GETDATE())    FOR [management_student_createdDate]
GO

ALTER TABLE [dbo].[management_student_table] WITH CHECK ADD CONSTRAINT [FK_management_student_person]
    FOREIGN KEY ([management_student_PersonID]) REFERENCES [dbo].[management_person_table] ([management_person_ID])
GO
ALTER TABLE [dbo].[management_student_table] CHECK CONSTRAINT [FK_management_student_person]
GO

ALTER TABLE [dbo].[management_student_table] WITH CHECK ADD CONSTRAINT [FK_management_student_career]
    FOREIGN KEY ([management_student_CareerID]) REFERENCES [dbo].[management_career_table] ([management_career_ID]) ON DELETE SET NULL
GO
ALTER TABLE [dbo].[management_student_table] CHECK CONSTRAINT [FK_management_student_career]
GO

ALTER TABLE [dbo].[management_student_table] WITH CHECK ADD CONSTRAINT [FK_management_student_group]
    FOREIGN KEY ([management_student_GroupID]) REFERENCES [dbo].[management_group_table] ([management_group_ID]) ON DELETE SET NULL
GO
ALTER TABLE [dbo].[management_student_table] CHECK CONSTRAINT [FK_management_student_group]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_management_student_Matricula] ON [dbo].[management_student_table]
(
    [management_student_Matricula] ASC
) WHERE ([management_student_Matricula] IS NOT NULL) ON [PRIMARY]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_management_student_EnrollmentFolio] ON [dbo].[management_student_table]
(
    [management_student_EnrollmentFolio] ASC
) WHERE ([management_student_EnrollmentFolio] IS NOT NULL) ON [PRIMARY]
GO

/* ============================================================
   8. management_teacher_table
      Teaching staff members linked to a person record
   ============================================================ */
CREATE TABLE [dbo].[management_teacher_table](
    [management_teacher_ID]             INT IDENTITY(1,1) NOT NULL,
    [management_teacher_PersonID]       INT               NOT NULL,
    [management_teacher_EmployeeNumber] NVARCHAR(30)      NULL,
    [management_teacher_StatusCode]     NVARCHAR(30)      NOT NULL,
    [management_teacher_status]         BIT               NOT NULL,
    [management_teacher_createdDate]    DATETIME          NOT NULL,
    CONSTRAINT [PK_management_teacher] PRIMARY KEY CLUSTERED ([management_teacher_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_teacher_table] ADD CONSTRAINT [DF_management_teacher_StatusCode]  DEFAULT ('ACTIVO')  FOR [management_teacher_StatusCode]
GO
ALTER TABLE [dbo].[management_teacher_table] ADD CONSTRAINT [DF_management_teacher_status]      DEFAULT ((1))       FOR [management_teacher_status]
GO
ALTER TABLE [dbo].[management_teacher_table] ADD CONSTRAINT [DF_management_teacher_createdDate] DEFAULT (GETDATE()) FOR [management_teacher_createdDate]
GO

ALTER TABLE [dbo].[management_teacher_table] WITH CHECK ADD CONSTRAINT [FK_management_teacher_person]
    FOREIGN KEY ([management_teacher_PersonID]) REFERENCES [dbo].[management_person_table] ([management_person_ID])
GO
ALTER TABLE [dbo].[management_teacher_table] CHECK CONSTRAINT [FK_management_teacher_person]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_management_teacher_EmployeeNumber] ON [dbo].[management_teacher_table]
(
    [management_teacher_EmployeeNumber] ASC
) WHERE ([management_teacher_EmployeeNumber] IS NOT NULL) ON [PRIMARY]
GO

/* ============================================================
   9. management_userrole_table
      Many-to-many: users ↔ roles
   ============================================================ */
CREATE TABLE [dbo].[management_userrole_table](
    [management_userrole_ID]          INT IDENTITY(1,1) NOT NULL,
    [management_userrole_UserID]      INT               NOT NULL,
    [management_userrole_RoleID]      INT               NOT NULL,
    [management_userrole_status]      BIT               NOT NULL,
    [management_userrole_createdDate] DATETIME          NOT NULL,
    CONSTRAINT [PK_management_userrole] PRIMARY KEY CLUSTERED ([management_userrole_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_userrole_table] ADD CONSTRAINT [DF_management_userrole_status]      DEFAULT ((1))       FOR [management_userrole_status]
GO
ALTER TABLE [dbo].[management_userrole_table] ADD CONSTRAINT [DF_management_userrole_createdDate] DEFAULT (GETDATE()) FOR [management_userrole_createdDate]
GO

ALTER TABLE [dbo].[management_userrole_table] WITH CHECK ADD CONSTRAINT [FK_management_userrole_user]
    FOREIGN KEY ([management_userrole_UserID]) REFERENCES [dbo].[management_user_table] ([management_user_ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[management_userrole_table] CHECK CONSTRAINT [FK_management_userrole_user]
GO

ALTER TABLE [dbo].[management_userrole_table] WITH CHECK ADD CONSTRAINT [FK_management_userrole_role]
    FOREIGN KEY ([management_userrole_RoleID]) REFERENCES [dbo].[management_role_table] ([management_role_ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[management_userrole_table] CHECK CONSTRAINT [FK_management_userrole_role]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_management_userrole_UserID_RoleID] ON [dbo].[management_userrole_table]
(
    [management_userrole_UserID] ASC,
    [management_userrole_RoleID] ASC
) ON [PRIMARY]
GO

/* ============================================================
   10. management_rolepermission_table
       Many-to-many: roles ↔ permissions
   ============================================================ */
CREATE TABLE [dbo].[management_rolepermission_table](
    [management_rolepermission_ID]           INT IDENTITY(1,1) NOT NULL,
    [management_rolepermission_RoleID]       INT               NOT NULL,
    [management_rolepermission_PermissionID] INT               NOT NULL,
    [management_rolepermission_status]       BIT               NOT NULL,
    [management_rolepermission_createdDate]  DATETIME          NOT NULL,
    CONSTRAINT [PK_management_rolepermission] PRIMARY KEY CLUSTERED ([management_rolepermission_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_rolepermission_table] ADD CONSTRAINT [DF_management_rolepermission_status]      DEFAULT ((1))       FOR [management_rolepermission_status]
GO
ALTER TABLE [dbo].[management_rolepermission_table] ADD CONSTRAINT [DF_management_rolepermission_createdDate] DEFAULT (GETDATE()) FOR [management_rolepermission_createdDate]
GO

ALTER TABLE [dbo].[management_rolepermission_table] WITH CHECK ADD CONSTRAINT [FK_management_rolepermission_role]
    FOREIGN KEY ([management_rolepermission_RoleID]) REFERENCES [dbo].[management_role_table] ([management_role_ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[management_rolepermission_table] CHECK CONSTRAINT [FK_management_rolepermission_role]
GO

ALTER TABLE [dbo].[management_rolepermission_table] WITH CHECK ADD CONSTRAINT [FK_management_rolepermission_permission]
    FOREIGN KEY ([management_rolepermission_PermissionID]) REFERENCES [dbo].[management_permission_table] ([management_permission_ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[management_rolepermission_table] CHECK CONSTRAINT [FK_management_rolepermission_permission]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_management_rolepermission_RoleID_PermissionID] ON [dbo].[management_rolepermission_table]
(
    [management_rolepermission_RoleID]       ASC,
    [management_rolepermission_PermissionID] ASC
) ON [PRIMARY]
GO

/* ============================================================
   11. management_usercareer_table
       Many-to-many: users ↔ careers (with optional role label)
   ============================================================ */
CREATE TABLE [dbo].[management_usercareer_table](
    [management_usercareer_ID]           INT IDENTITY(1,1) NOT NULL,
    [management_usercareer_UserID]       INT               NOT NULL,
    [management_usercareer_CareerID]     INT               NOT NULL,
    [management_usercareer_RoleInCareer] NVARCHAR(50)      NULL,
    [management_usercareer_status]       BIT               NOT NULL,
    [management_usercareer_createdDate]  DATETIME          NOT NULL,
    CONSTRAINT [PK_management_usercareer] PRIMARY KEY CLUSTERED ([management_usercareer_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_usercareer_table] ADD CONSTRAINT [DF_management_usercareer_status]      DEFAULT ((1))       FOR [management_usercareer_status]
GO
ALTER TABLE [dbo].[management_usercareer_table] ADD CONSTRAINT [DF_management_usercareer_createdDate] DEFAULT (GETDATE()) FOR [management_usercareer_createdDate]
GO

ALTER TABLE [dbo].[management_usercareer_table] WITH CHECK ADD CONSTRAINT [FK_management_usercareer_user]
    FOREIGN KEY ([management_usercareer_UserID]) REFERENCES [dbo].[management_user_table] ([management_user_ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[management_usercareer_table] CHECK CONSTRAINT [FK_management_usercareer_user]
GO

ALTER TABLE [dbo].[management_usercareer_table] WITH CHECK ADD CONSTRAINT [FK_management_usercareer_career]
    FOREIGN KEY ([management_usercareer_CareerID]) REFERENCES [dbo].[management_career_table] ([management_career_ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[management_usercareer_table] CHECK CONSTRAINT [FK_management_usercareer_career]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_management_usercareer_UserID_CareerID] ON [dbo].[management_usercareer_table]
(
    [management_usercareer_UserID]   ASC,
    [management_usercareer_CareerID] ASC
) ON [PRIMARY]
GO

/* ============================================================
   12. management_studentcareer_history_table
       Audit trail of career changes for a student
   ============================================================ */
CREATE TABLE [dbo].[management_studentcareer_history_table](
    [management_studentcareer_history_ID]          INT IDENTITY(1,1) NOT NULL,
    [management_studentcareer_history_StudentID]   INT               NOT NULL,
    [management_studentcareer_history_CareerID]    INT               NOT NULL,
    [management_studentcareer_history_StartDate]   DATE              NOT NULL,
    [management_studentcareer_history_EndDate]     DATE              NULL,
    [management_studentcareer_history_Reason]      NVARCHAR(200)     NULL,
    [management_studentcareer_history_status]      BIT               NOT NULL,
    [management_studentcareer_history_createdDate] DATETIME          NOT NULL,
    CONSTRAINT [PK_management_studentcareer_history] PRIMARY KEY CLUSTERED ([management_studentcareer_history_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_studentcareer_history_table] ADD CONSTRAINT [DF_management_studentcareer_history_status]      DEFAULT ((1))       FOR [management_studentcareer_history_status]
GO
ALTER TABLE [dbo].[management_studentcareer_history_table] ADD CONSTRAINT [DF_management_studentcareer_history_createdDate] DEFAULT (GETDATE()) FOR [management_studentcareer_history_createdDate]
GO

ALTER TABLE [dbo].[management_studentcareer_history_table] WITH CHECK ADD CONSTRAINT [FK_management_studentcareer_history_student]
    FOREIGN KEY ([management_studentcareer_history_StudentID]) REFERENCES [dbo].[management_student_table] ([management_student_ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[management_studentcareer_history_table] CHECK CONSTRAINT [FK_management_studentcareer_history_student]
GO

ALTER TABLE [dbo].[management_studentcareer_history_table] WITH CHECK ADD CONSTRAINT [FK_management_studentcareer_history_career]
    FOREIGN KEY ([management_studentcareer_history_CareerID]) REFERENCES [dbo].[management_career_table] ([management_career_ID])
GO
ALTER TABLE [dbo].[management_studentcareer_history_table] CHECK CONSTRAINT [FK_management_studentcareer_history_career]
GO

/* ============================================================
   13. management_studentgroup_history_table
       Audit trail of group changes for a student
   ============================================================ */
CREATE TABLE [dbo].[management_studentgroup_history_table](
    [management_studentgroup_history_ID]          INT IDENTITY(1,1) NOT NULL,
    [management_studentgroup_history_StudentID]   INT               NOT NULL,
    [management_studentgroup_history_GroupID]     INT               NOT NULL,
    [management_studentgroup_history_StartDate]   DATE              NOT NULL,
    [management_studentgroup_history_EndDate]     DATE              NULL,
    [management_studentgroup_history_Reason]      NVARCHAR(200)     NULL,
    [management_studentgroup_history_status]      BIT               NOT NULL,
    [management_studentgroup_history_createdDate] DATETIME          NOT NULL,
    CONSTRAINT [PK_management_studentgroup_history] PRIMARY KEY CLUSTERED ([management_studentgroup_history_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[management_studentgroup_history_table] ADD CONSTRAINT [DF_management_studentgroup_history_status]      DEFAULT ((1))       FOR [management_studentgroup_history_status]
GO
ALTER TABLE [dbo].[management_studentgroup_history_table] ADD CONSTRAINT [DF_management_studentgroup_history_createdDate] DEFAULT (GETDATE()) FOR [management_studentgroup_history_createdDate]
GO

ALTER TABLE [dbo].[management_studentgroup_history_table] WITH CHECK ADD CONSTRAINT [FK_management_studentgroup_history_student]
    FOREIGN KEY ([management_studentgroup_history_StudentID]) REFERENCES [dbo].[management_student_table] ([management_student_ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[management_studentgroup_history_table] CHECK CONSTRAINT [FK_management_studentgroup_history_student]
GO

ALTER TABLE [dbo].[management_studentgroup_history_table] WITH CHECK ADD CONSTRAINT [FK_management_studentgroup_history_group]
    FOREIGN KEY ([management_studentgroup_history_GroupID]) REFERENCES [dbo].[management_group_table] ([management_group_ID])
GO
ALTER TABLE [dbo].[management_studentgroup_history_table] CHECK CONSTRAINT [FK_management_studentgroup_history_group]
GO

/* ============================================================
   OPERATIONAL TABLES
   Servicio Social (SS) & Practicas Profesionales (PP)
   ============================================================ */

/* ============================================================
   1. operational_organization_table
      Companies / institutions where students perform SS or PP
   ============================================================ */
CREATE TABLE [dbo].[operational_organization_table](
    [operational_organization_ID]          INT IDENTITY(1,1) NOT NULL,
    [operational_organization_Name]        NVARCHAR(200)     NOT NULL,
    [operational_organization_Type]        NVARCHAR(50)      NOT NULL,
    [operational_organization_Address]     NVARCHAR(300)     NULL,
    [operational_organization_City]        NVARCHAR(100)     NULL,
    [operational_organization_State]       NVARCHAR(100)     NULL,
    [operational_organization_Phone]       NVARCHAR(30)      NULL,
    [operational_organization_Email]       NVARCHAR(150)     NULL,
    [operational_organization_ContactName] NVARCHAR(150)     NULL,
    [operational_organization_Notes]       NVARCHAR(500)     NULL,
    [operational_organization_status]      BIT               NOT NULL,
    [operational_organization_createdDate] DATETIME          NOT NULL,
    CONSTRAINT [PK_operational_organization] PRIMARY KEY CLUSTERED ([operational_organization_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[operational_organization_table] ADD CONSTRAINT [DF_operational_organization_status]      DEFAULT ((1))       FOR [operational_organization_status]
GO
ALTER TABLE [dbo].[operational_organization_table] ADD CONSTRAINT [DF_operational_organization_createdDate] DEFAULT (GETDATE()) FOR [operational_organization_createdDate]
GO

/* ============================================================
   2. operational_program_table
      Programs / periods — Type distinguishes SS from PP
   ============================================================ */
CREATE TABLE [dbo].[operational_program_table](
    [operational_program_ID]            INT IDENTITY(1,1) NOT NULL,
    [operational_program_Code]          NVARCHAR(30)      NOT NULL,
    [operational_program_Name]          NVARCHAR(200)     NOT NULL,
    [operational_program_Type]          NVARCHAR(50)      NOT NULL,
    [operational_program_Period]        NVARCHAR(50)      NULL,
    [operational_program_Year]          INT               NULL,
    [operational_program_CareerID]      INT               NULL,
    [operational_program_CoordinatorID] INT               NULL,
    [operational_program_RequiredHours] INT               NOT NULL,
    [operational_program_StartDate]     DATE              NULL,
    [operational_program_EndDate]       DATE              NULL,
    [operational_program_Description]   NVARCHAR(500)     NULL,
    [operational_program_IsActive]      BIT               NOT NULL,
    [operational_program_status]        BIT               NOT NULL,
    [operational_program_createdDate]   DATETIME          NOT NULL,
    CONSTRAINT [PK_operational_program] PRIMARY KEY CLUSTERED ([operational_program_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[operational_program_table] ADD CONSTRAINT [DF_operational_program_RequiredHours] DEFAULT ((480))     FOR [operational_program_RequiredHours]
GO
ALTER TABLE [dbo].[operational_program_table] ADD CONSTRAINT [DF_operational_program_IsActive]      DEFAULT ((1))       FOR [operational_program_IsActive]
GO
ALTER TABLE [dbo].[operational_program_table] ADD CONSTRAINT [DF_operational_program_status]        DEFAULT ((1))       FOR [operational_program_status]
GO
ALTER TABLE [dbo].[operational_program_table] ADD CONSTRAINT [DF_operational_program_createdDate]   DEFAULT (GETDATE()) FOR [operational_program_createdDate]
GO

ALTER TABLE [dbo].[operational_program_table] WITH CHECK ADD CONSTRAINT [FK_operational_program_career]
    FOREIGN KEY ([operational_program_CareerID]) REFERENCES [dbo].[management_career_table] ([management_career_ID]) ON DELETE SET NULL
GO
ALTER TABLE [dbo].[operational_program_table] CHECK CONSTRAINT [FK_operational_program_career]
GO

ALTER TABLE [dbo].[operational_program_table] WITH CHECK ADD CONSTRAINT [FK_operational_program_coordinator]
    FOREIGN KEY ([operational_program_CoordinatorID]) REFERENCES [dbo].[management_teacher_table] ([management_teacher_ID]) ON DELETE SET NULL
GO
ALTER TABLE [dbo].[operational_program_table] CHECK CONSTRAINT [FK_operational_program_coordinator]
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_operational_program_Code] ON [dbo].[operational_program_table]
(
    [operational_program_Code] ASC
) ON [PRIMARY]
GO

/* ============================================================
   3. operational_studentassignment_table
      Student participation — links student to program, org, teacher
   ============================================================ */
CREATE TABLE [dbo].[operational_studentassignment_table](
    [operational_studentassignment_ID]             INT IDENTITY(1,1) NOT NULL,
    [operational_studentassignment_StudentID]      INT               NOT NULL,
    [operational_studentassignment_ProgramID]      INT               NOT NULL,
    [operational_studentassignment_OrganizationID] INT               NULL,
    [operational_studentassignment_TeacherID]      INT               NULL,
    [operational_studentassignment_StatusCode]     NVARCHAR(50)      NOT NULL,
    [operational_studentassignment_StartDate]      DATE              NULL,
    [operational_studentassignment_EndDate]        DATE              NULL,
    [operational_studentassignment_TotalHours]     DECIMAL(8,2)      NOT NULL,
    [operational_studentassignment_ApprovedHours]  DECIMAL(8,2)      NOT NULL,
    [operational_studentassignment_EvaluationScore] DECIMAL(5,2)     NULL,
    [operational_studentassignment_EvaluationNotes] NVARCHAR(500)    NULL,
    [operational_studentassignment_Notes]          NVARCHAR(500)     NULL,
    [operational_studentassignment_CompletedDate]  DATETIME          NULL,
    [operational_studentassignment_status]         BIT               NOT NULL,
    [operational_studentassignment_createdDate]    DATETIME          NOT NULL,
    CONSTRAINT [PK_operational_studentassignment] PRIMARY KEY CLUSTERED ([operational_studentassignment_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[operational_studentassignment_table] ADD CONSTRAINT [DF_operational_studentassignment_StatusCode]    DEFAULT ('REGISTERED') FOR [operational_studentassignment_StatusCode]
GO
ALTER TABLE [dbo].[operational_studentassignment_table] ADD CONSTRAINT [DF_operational_studentassignment_TotalHours]    DEFAULT ((0))          FOR [operational_studentassignment_TotalHours]
GO
ALTER TABLE [dbo].[operational_studentassignment_table] ADD CONSTRAINT [DF_operational_studentassignment_ApprovedHours] DEFAULT ((0))          FOR [operational_studentassignment_ApprovedHours]
GO
ALTER TABLE [dbo].[operational_studentassignment_table] ADD CONSTRAINT [DF_operational_studentassignment_status]        DEFAULT ((1))          FOR [operational_studentassignment_status]
GO
ALTER TABLE [dbo].[operational_studentassignment_table] ADD CONSTRAINT [DF_operational_studentassignment_createdDate]   DEFAULT (GETDATE())    FOR [operational_studentassignment_createdDate]
GO

ALTER TABLE [dbo].[operational_studentassignment_table] WITH CHECK ADD CONSTRAINT [FK_operational_studentassignment_student]
    FOREIGN KEY ([operational_studentassignment_StudentID]) REFERENCES [dbo].[management_student_table] ([management_student_ID])
GO
ALTER TABLE [dbo].[operational_studentassignment_table] CHECK CONSTRAINT [FK_operational_studentassignment_student]
GO

ALTER TABLE [dbo].[operational_studentassignment_table] WITH CHECK ADD CONSTRAINT [FK_operational_studentassignment_program]
    FOREIGN KEY ([operational_studentassignment_ProgramID]) REFERENCES [dbo].[operational_program_table] ([operational_program_ID])
GO
ALTER TABLE [dbo].[operational_studentassignment_table] CHECK CONSTRAINT [FK_operational_studentassignment_program]
GO

ALTER TABLE [dbo].[operational_studentassignment_table] WITH CHECK ADD CONSTRAINT [FK_operational_studentassignment_organization]
    FOREIGN KEY ([operational_studentassignment_OrganizationID]) REFERENCES [dbo].[operational_organization_table] ([operational_organization_ID]) ON DELETE SET NULL
GO
ALTER TABLE [dbo].[operational_studentassignment_table] CHECK CONSTRAINT [FK_operational_studentassignment_organization]
GO

ALTER TABLE [dbo].[operational_studentassignment_table] WITH CHECK ADD CONSTRAINT [FK_operational_studentassignment_teacher]
    FOREIGN KEY ([operational_studentassignment_TeacherID]) REFERENCES [dbo].[management_teacher_table] ([management_teacher_ID]) ON DELETE SET NULL
GO
ALTER TABLE [dbo].[operational_studentassignment_table] CHECK CONSTRAINT [FK_operational_studentassignment_teacher]
GO

CREATE NONCLUSTERED INDEX [IX_operational_studentassignment_StudentID] ON [dbo].[operational_studentassignment_table]
(
    [operational_studentassignment_StudentID] ASC
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_operational_studentassignment_ProgramID] ON [dbo].[operational_studentassignment_table]
(
    [operational_studentassignment_ProgramID] ASC
) ON [PRIMARY]
GO

/* ============================================================
   4. operational_document_table
      File uploads with per-document status & teacher review
   ============================================================ */
CREATE TABLE [dbo].[operational_document_table](
    [operational_document_ID]                 INT IDENTITY(1,1) NOT NULL,
    [operational_document_AssignmentID]        INT               NOT NULL,
    [operational_document_UploadedByUserID]    INT               NULL,
    [operational_document_DocumentType]        NVARCHAR(50)      NOT NULL,
    [operational_document_Title]               NVARCHAR(200)     NOT NULL,
    [operational_document_FileName]            NVARCHAR(300)     NULL,
    [operational_document_FilePath]            NVARCHAR(500)     NULL,
    [operational_document_ContentType]         NVARCHAR(100)     NULL,
    [operational_document_FileSize]            BIGINT            NULL,
    [operational_document_Notes]               NVARCHAR(500)     NULL,
    [operational_document_OriginalFileName]    NVARCHAR(300)     NULL,
    [operational_document_StatusCode]          NVARCHAR(50)      NOT NULL,
    [operational_document_ReviewedByTeacherID] INT               NULL,
    [operational_document_ReviewDate]          DATETIME          NULL,
    [operational_document_ReviewComments]      NVARCHAR(500)     NULL,
    [operational_document_UploadDate]          DATETIME          NOT NULL,
    [operational_document_status]              BIT               NOT NULL,
    [operational_document_createdDate]         DATETIME          NOT NULL,
    CONSTRAINT [PK_operational_document] PRIMARY KEY CLUSTERED ([operational_document_ID] ASC)
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[operational_document_table] ADD CONSTRAINT [DF_operational_document_StatusCode]   DEFAULT ('PENDING')  FOR [operational_document_StatusCode]
GO
ALTER TABLE [dbo].[operational_document_table] ADD CONSTRAINT [DF_operational_document_UploadDate]   DEFAULT (GETDATE())  FOR [operational_document_UploadDate]
GO
ALTER TABLE [dbo].[operational_document_table] ADD CONSTRAINT [DF_operational_document_status]       DEFAULT ((1))        FOR [operational_document_status]
GO
ALTER TABLE [dbo].[operational_document_table] ADD CONSTRAINT [DF_operational_document_createdDate]  DEFAULT (GETDATE())  FOR [operational_document_createdDate]
GO

ALTER TABLE [dbo].[operational_document_table] WITH CHECK ADD CONSTRAINT [FK_operational_document_assignment]
    FOREIGN KEY ([operational_document_AssignmentID]) REFERENCES [dbo].[operational_studentassignment_table] ([operational_studentassignment_ID])
GO
ALTER TABLE [dbo].[operational_document_table] CHECK CONSTRAINT [FK_operational_document_assignment]
GO

ALTER TABLE [dbo].[operational_document_table] WITH CHECK ADD CONSTRAINT [FK_operational_document_uploadedby]
    FOREIGN KEY ([operational_document_UploadedByUserID]) REFERENCES [dbo].[management_user_table] ([management_user_ID]) ON DELETE SET NULL
GO
ALTER TABLE [dbo].[operational_document_table] CHECK CONSTRAINT [FK_operational_document_uploadedby]
GO

ALTER TABLE [dbo].[operational_document_table] WITH CHECK ADD CONSTRAINT [FK_operational_document_reviewedby]
    FOREIGN KEY ([operational_document_ReviewedByTeacherID]) REFERENCES [dbo].[management_teacher_table] ([management_teacher_ID]) ON DELETE SET NULL
GO
ALTER TABLE [dbo].[operational_document_table] CHECK CONSTRAINT [FK_operational_document_reviewedby]
GO

CREATE NONCLUSTERED INDEX [IX_operational_document_AssignmentID] ON [dbo].[operational_document_table]
(
    [operational_document_AssignmentID] ASC
) ON [PRIMARY]
GO
