using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvigilatorSchedulerStandard.Data;
using InvigilatorSchedulerStandard.Models;
using Microsoft.AspNetCore.Authorization;

namespace InvigilatorSchedulerStandard.Controllers;

[Authorize]
public class TeachersController : Controller
{
    private readonly AppDbContext _db;
    public TeachersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var exams = await _db.ExamSessions.AsNoTracking()
            .OrderBy(x => x.Day).ThenBy(x => x.SessionIndex).ThenBy(x => x.GradeId)
            .ToListAsync();

        var grades = await _db.Grades.AsNoTracking().ToDictionaryAsync(g => g.Id, g => g.Name);

        var teachers = await _db.Teachers.AsNoTracking()
            .Include(t => t.RestrictedExamSessions)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var vm = new TeachersVm
        {
            ExamPickList = exams.Select(e => new TeachersVm.ExamPickRow
            {
                Id = e.Id,
                Text = $"#{e.Id} | {e.Day} | S{e.SessionIndex} | {grades.GetValueOrDefault(e.GradeId, $"Grade#{e.GradeId}")} | {e.SubjectName}"
            }).ToList(),

            Items = teachers.Select(t => new TeachersVm.TeacherRow
            {
                Id = t.Id,
                Name = t.Name,
                Subject = t.Subject,
                RestrictedIds = t.RestrictedExamSessions.Select(r => r.ExamSessionId).OrderBy(x => x).ToList()
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeachersVm vm)
    {
        if (!ModelState.IsValid)
            return await Index(); // reload with lists

        var teacher = new Teacher { Name = vm.Name.Trim(), Subject = vm.Subject.Trim() };
        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync();

        var ids = (vm.RestrictedExamSessionIds ?? new()).Distinct().ToList();
        foreach (var examId in ids)
            _db.TeacherRestrictedExamSessions.Add(new TeacherRestrictedExamSession { TeacherId = teacher.Id, ExamSessionId = examId });

        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.Teachers.FindAsync(id);
        if (t != null)
        {
            var links = await _db.TeacherRestrictedExamSessions.Where(x => x.TeacherId == id).ToListAsync();
            _db.TeacherRestrictedExamSessions.RemoveRange(links);

            _db.Teachers.Remove(t);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }
}
