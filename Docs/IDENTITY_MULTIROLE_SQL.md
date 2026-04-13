# Identity Multirol SQL

Ejemplos para trabajar con usuarios multirol sin cambiar la estructura actual.

## 1. Ver roles actuales de un usuario

```sql
SELECT
    u.management_user_ID,
    u.management_user_Username,
    u.management_user_Email,
    r.management_role_Name,
    ur.management_userrole_status
FROM dbo.management_user_table u
LEFT JOIN dbo.management_userrole_table ur
    ON ur.management_userrole_UserID = u.management_user_ID
LEFT JOIN dbo.management_role_table r
    ON r.management_role_ID = ur.management_userrole_RoleID
WHERE u.management_user_Username = 'docente.multi.demo'
ORDER BY r.management_role_Name;
```

## 2. Asignar varios roles a un usuario existente

```sql
DECLARE @UserId INT = (
    SELECT TOP 1 management_user_ID
    FROM dbo.management_user_table
    WHERE management_user_Username = 'docente.multi.demo'
);

INSERT INTO dbo.management_userrole_table
    (management_userrole_UserID, management_userrole_RoleID, management_userrole_status, management_userrole_createdDate)
SELECT
    @UserId,
    r.management_role_ID,
    1,
    GETDATE()
FROM dbo.management_role_table r
WHERE r.management_role_Name IN ('Maestro', 'Tutor', 'AsesorAcademico')
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.management_userrole_table ur
      WHERE ur.management_userrole_UserID = @UserId
        AND ur.management_userrole_RoleID = r.management_role_ID
  );
```

## 3. Crear usuario nuevo con roles multiples

```sql
BEGIN TRANSACTION;

DECLARE @PersonId INT;
DECLARE @UserId INT;

INSERT INTO dbo.management_person_table
    (management_person_FirstName, management_person_LastNamePaternal, management_person_LastNameMaternal, management_person_Email, management_person_status, management_person_createdDate)
VALUES
    ('Usuario', 'Multirol', 'Demo', 'multirol.demo@demo.local', 1, GETDATE());

SET @PersonId = SCOPE_IDENTITY();

INSERT INTO dbo.management_user_table
    (management_user_PersonID, management_user_Username, management_user_Email, management_user_PasswordHash, management_user_IsLocked, management_user_status, management_user_createdDate, management_user_RoleID)
VALUES
    (
        @PersonId,
        'multirol.demo',
        'multirol.demo@demo.local',
        'VX3B2Q7w2P2N4g6I1h0n1dE8H3k7wQp8W8eL7hWwJdY=',
        0,
        1,
        GETDATE(),
        (SELECT TOP 1 management_role_ID FROM dbo.management_role_table WHERE management_role_Name = 'Maestro')
    );

SET @UserId = SCOPE_IDENTITY();

INSERT INTO dbo.management_userrole_table
    (management_userrole_UserID, management_userrole_RoleID, management_userrole_status, management_userrole_createdDate)
SELECT
    @UserId,
    r.management_role_ID,
    1,
    GETDATE()
FROM dbo.management_role_table r
WHERE r.management_role_Name IN ('Maestro', 'Tutor', 'AsesorAcademico');

COMMIT TRANSACTION;
```

Nota:
La contraseña del ejemplo ya está en SHA256 Base64. Si quieres mantener compatibilidad con el login actual, usa ese mismo formato.

## 4. Crear un rol nuevo

```sql
INSERT INTO dbo.management_role_table
    (management_role_Name, management_role_Description, management_role_status, management_role_createdDate)
VALUES
    ('Biblioteca', 'Rol para modulo de biblioteca', 1, GETDATE());
```

## 5. Editar nombre o descripcion de un rol existente

```sql
UPDATE dbo.management_role_table
SET
    management_role_Name = 'Enfermeria',
    management_role_Description = 'Rol para modulo de enfermeria',
    management_role_status = 1
WHERE management_role_Name = 'Enfermería';
```

## 6. Desactivar un rol sin borrarlo

```sql
UPDATE dbo.management_role_table
SET management_role_status = 0
WHERE management_role_Name = 'RolObsoleto';
```

## 7. Ver usuarios con sus roles agregados

```sql
SELECT
    u.management_user_ID,
    u.management_user_Username,
    u.management_user_Email,
    STRING_AGG(r.management_role_Name, ', ') WITHIN GROUP (ORDER BY r.management_role_Name) AS Roles
FROM dbo.management_user_table u
LEFT JOIN dbo.management_userrole_table ur
    ON ur.management_userrole_UserID = u.management_user_ID
   AND ur.management_userrole_status = 1
LEFT JOIN dbo.management_role_table r
    ON r.management_role_ID = ur.management_userrole_RoleID
   AND r.management_role_status = 1
WHERE u.management_user_status = 1
GROUP BY
    u.management_user_ID,
    u.management_user_Username,
    u.management_user_Email
ORDER BY u.management_user_Username;
```