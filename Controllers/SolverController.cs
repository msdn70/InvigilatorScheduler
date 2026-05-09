using Microsoft.AspNetCore.Mvc;
using InvigilatorSchedulerStandard.Models;
using InvigilatorSchedulerStandard.Services;
using Microsoft.AspNetCore.Authorization;

namespace InvigilatorSchedulerStandard.Controllers;

[Authorize]
public class SolverController : Controller
{
    private readonly RuleService _rules;
    private readonly InvigilatorSolveService _solver;

    public SolverController(RuleService rules, InvigilatorSolveService solver)
    {
        _rules = rules;
        _solver = solver;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var s = await _rules.GetSettings();
        return View(new SolverVm
        {
            DefaultInvigilatorsPerExam = s.DefaultInvigilatorsPerExam,
            BackupInvigilatorsPerDay = s.BackupInvigilatorsPerDay,
            RandomSeed = s.RandomSeed,
            FairnessEnabled = s.FairnessEnabled
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SolverVm vm)
    {
        await _rules.SaveSettings(new RuleService.SettingsRule
        {
            DefaultInvigilatorsPerExam = vm.DefaultInvigilatorsPerExam,
            BackupInvigilatorsPerDay = vm.BackupInvigilatorsPerDay,
            RandomSeed = vm.RandomSeed,
            FairnessEnabled = vm.FairnessEnabled
        });

        vm.Message = "Settings saved.";
        return View("Index", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Solve(SolverVm vm)
    {
        await _rules.SaveSettings(new RuleService.SettingsRule
        {
            DefaultInvigilatorsPerExam = vm.DefaultInvigilatorsPerExam,
            BackupInvigilatorsPerDay = vm.BackupInvigilatorsPerDay,
            RandomSeed = vm.RandomSeed,
            FairnessEnabled = vm.FairnessEnabled
        });

        vm.Result = await _solver.Solve();
        vm.Message = "Solved.";
        return View("Index", vm);
    }
}
