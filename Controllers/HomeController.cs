using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using InvigilatorSchedulerStandard.Data;
using InvigilatorSchedulerStandard.Models;
using Microsoft.AspNetCore.Authorization;

namespace InvigilatorSchedulerStandard.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var vm = new DashboardVm
        {
            GradesCount = await _db.Grades.CountAsync(),
            ExamsCount = await _db.ExamSessions.CountAsync(),
            TeachersCount = await _db.Teachers.CountAsync(),
            RestrictionsCount = await _db.TeacherRestrictedExamSessions.CountAsync()
        };
        return View(vm);
    }
}
