using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvigilatorSchedulerStandard.Data;
using InvigilatorSchedulerStandard.Models;
using Microsoft.AspNetCore.Authorization;

namespace InvigilatorSchedulerStandard.Controllers;

[Authorize]
public class ExamsController : Controller
{
    private readonly AppDbContext _db;
    public ExamsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = new ExamsVm
        {
            Grades = await _db.Grades.OrderBy(x => x.Name).ToListAsync(),
            Items = await _db.ExamSessions.OrderBy(x => x.Day).ThenBy(x => x.SessionIndex).ThenBy(x => x.GradeId).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExamsVm vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Grades = await _db.Grades.OrderBy(x => x.Name).ToListAsync();
            vm.Items = await _db.ExamSessions.OrderBy(x => x.Day).ThenBy(x => x.SessionIndex).ThenBy(x => x.GradeId).ToListAsync();
            return View("Index", vm);
        }

        _db.ExamSessions.Add(new ExamSession
        {
            Day = vm.Day.Trim(),
            SessionIndex = vm.SessionIndex,
            GradeId = vm.GradeId,
            SubjectName = vm.SubjectName.Trim(),
            InvigilatorsOverride = vm.InvigilatorsOverride
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.ExamSessions.FindAsync(id);
        if (e != null)
        {
            // delete restriction links first
            var links = await _db.TeacherRestrictedExamSessions.Where(x => x.ExamSessionId == id).ToListAsync();
            _db.TeacherRestrictedExamSessions.RemoveRange(links);

            _db.ExamSessions.Remove(e);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }
}
