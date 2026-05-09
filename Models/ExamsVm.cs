using System.ComponentModel.DataAnnotations;
using InvigilatorSchedulerStandard.Data;

namespace InvigilatorSchedulerStandard.Models;

public class ExamsVm
{
    [Required, StringLength(10)]
    public string Day { get; set; } = "Sun";

    [Range(1, 10)]
    public int SessionIndex { get; set; } = 1;

    [Range(1, int.MaxValue)]
    public int GradeId { get; set; }

    [Required, StringLength(100)]
    public string SubjectName { get; set; } = "";

    [Range(0, 10)]
    public int InvigilatorsOverride { get; set; } = 0;

    public List<Grade> Grades { get; set; } = new();
    public List<ExamSession> Items { get; set; } = new();
}
