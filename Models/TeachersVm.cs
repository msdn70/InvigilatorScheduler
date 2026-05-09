using System.ComponentModel.DataAnnotations;
using InvigilatorSchedulerStandard.Data;

namespace InvigilatorSchedulerStandard.Models;

public class TeachersVm
{
    [Required, StringLength(200)]
    public string Name { get; set; } = "";

    [Required, StringLength(100)]
    public string Subject { get; set; } = "";

    // Selected restricted committees
    public List<int> RestrictedExamSessionIds { get; set; } = new();

    // For UI
    public List<TeacherRow> Items { get; set; } = new();
    public List<ExamPickRow> ExamPickList { get; set; } = new();

    public class TeacherRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Subject { get; set; } = "";
        public List<int> RestrictedIds { get; set; } = new();
    }

    public class ExamPickRow
    {
        public int Id { get; set; }
        public string Text { get; set; } = "";
    }
}
