namespace InvigilatorSchedulerStandard.Services;

public record SolveResponse(string Status, List<ExamAssignment> Exams, List<DayBackups> Backups, List<string> Notes);

public record TeacherInfo(int TeacherId, string Name, string Subject);

public record ExamAssignment(
    int ExamSessionId,
    string Day,
    int SessionIndex,
    int GradeId,
    string GradeName,
    string SubjectName,
    int RequiredInvigilators,
    List<TeacherInfo> Invigilators
);

public record DayBackups(string Day, int BackupCount, List<TeacherInfo> Teachers);
