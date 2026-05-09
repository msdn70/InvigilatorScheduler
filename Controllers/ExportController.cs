using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using InvigilatorSchedulerStandard.Data;
using InvigilatorSchedulerStandard.Services;
using Microsoft.AspNetCore.Authorization;

namespace InvigilatorSchedulerStandard.Controllers;

[Authorize]
public class ExportController : Controller
{
    private readonly InvigilatorSolveService _solver;
    private readonly ExcelExportService _excel;

    public ExportController(InvigilatorSolveService solver, ExcelExportService excel)
    {
        _solver = solver;
        _excel = excel;
    }

    [HttpGet("/export/schedule.xlsx")]
    public async Task<IActionResult> Schedule()
    {
        var result = await _solver.Solve();
        var bytes = _excel.BuildWorkbook(result);

        var fileName = $"InvigilatorSchedule_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
