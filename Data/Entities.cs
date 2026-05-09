namespace InvigilatorSchedulerStandard.Data;


public class Grade : IUserOwnedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? UserId { get; set; }
}

public class Teacher : IUserOwnedEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";     // Arabic OK (NVARCHAR)
    public string Subject { get; set; } = "";
    public string? UserId { get; set; }

    public List<TeacherRestrictedExamSession> RestrictedExamSessions { get; set; } = new();
}

public class ExamSession : IUserOwnedEntity
{
    public int Id { get; set; }
    public string Day { get; set; } = "";      // "Sun", "Mon"...
    public int SessionIndex { get; set; }      // 1..N
    public int GradeId { get; set; }
    public string SubjectName { get; set; } = "";
    public string? UserId { get; set; }

    // 0 => use default setting
    public int InvigilatorsOverride { get; set; } = 0;
}

public class TeacherRestrictedExamSession
{
    public int TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public int ExamSessionId { get; set; }
    public ExamSession ExamSession { get; set; } = null!;
}

public class RuleConfig : IUserOwnedEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string JsonValue { get; set; } = "{}";
    public string? UserId { get; set; }
}

public static class RuleCodes
{
    public const string Settings = "SETTINGS";
    public const string NoExamForBackupSameDay = "NO_EXAM_FOR_BACKUP_SAME_DAY";
    public const string MaxSessionsPerTeacherPerDay = "MAX_SESSIONS_PER_TEACHER_PER_DAY";
}
