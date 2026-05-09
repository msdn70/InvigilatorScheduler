using System.ComponentModel.DataAnnotations;
using InvigilatorSchedulerStandard.Data;

namespace InvigilatorSchedulerStandard.Models;

public class GradesVm
{
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    public List<Grade> Items { get; set; } = new();
}
