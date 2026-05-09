using InvigilatorSchedulerStandard.Services;

namespace InvigilatorSchedulerStandard.Models;

public class SolverVm
{
    public int DefaultInvigilatorsPerExam { get; set; }
    public int BackupInvigilatorsPerDay { get; set; }
    public int RandomSeed { get; set; }
    public bool FairnessEnabled { get; set; }

    public SolveResponse? Result { get; set; }
    public string? Message { get; set; }
}
