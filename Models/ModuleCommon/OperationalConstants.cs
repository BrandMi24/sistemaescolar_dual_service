namespace ControlEscolar.Models.ModuleCommon;

public static class ProgramTypes
{
    public const string SERVICIO_SOCIAL = "SERVICIO_SOCIAL";
    public const string PRACTICAS_PROFESIONALES = "PRACTICAS_PROFESIONALES";
}

public static class DualStatusCodes
{
    public const string REGISTERED = "REGISTERED";
    public const string PLACEMENT = "PLACEMENT";
    public const string PROFILE_COMPLETE = "PROFILE_COMPLETE";
    public const string DOCUMENTS = "DOCUMENTS";
    public const string LETTER_REQUESTED = "LETTER_REQUESTED";
    public const string ACCEPTANCE_SUBMITTED = "ACCEPTANCE_SUBMITTED";
    public const string ADVISORS_ASSIGNED = "ADVISORS_ASSIGNED";
    public const string IN_PROGRESS = "IN_PROGRESS";
    public const string COMPLETED = "COMPLETED";
    public const string FINALIZED = "FINALIZED";
}

public static class SSStatusCodes
{
    public const string REGISTERED = "REGISTERED";
    public const string PLACEMENT = "PLACEMENT";
    public const string PROFILE_COMPLETE = "PROFILE_COMPLETE";
    public const string LETTER_REQUESTED = "LETTER_REQUESTED";
    public const string ACCEPTANCE_SUBMITTED = "ACCEPTANCE_SUBMITTED";
    public const string ADVISOR_ASSIGNED = "ADVISOR_ASSIGNED";
    public const string IN_PROGRESS = "IN_PROGRESS";
    public const string COMPLETED = "COMPLETED";
    public const string RELEASED = "RELEASED";
}

public static class DocumentStatusCodes
{
    public const string PENDING = "PENDING";
    public const string APPROVED = "APPROVED";
    public const string REJECTED = "REJECTED";
}
