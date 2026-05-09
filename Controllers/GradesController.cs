using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvigilatorSchedulerStandard.Data;
using InvigilatorSchedulerStandard.Models;
using Microsoft.AspNetCore.Authorization;

namespace InvigilatorSchedulerStandard.Controllers;

[Authorize]
public class GradesController : Controller
{
    private readonly AppDbContext _db;
    public GradesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = new GradesVm { Items = await _db.Grades.OrderBy(x => x.Name).ToListAsync() };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GradesVm vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Items = await _db.Grades.OrderBy(x => x.Name).ToListAsync();
            return View("Index", vm);
        }

        _db.Grades.Add(new Grade { Name = vm.Name.Trim() });
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var g = await _db.Grades.FindAsync(id);
        if (g != null)
        {
            _db.Grades.Remove(g);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }
}
