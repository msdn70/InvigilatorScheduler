using ClosedXML.Excel;

namespace InvigilatorSchedulerStandard.Services;

public class ExcelExportService
{
    public byte[] BuildWorkbook(SolveResponse result)
    {
        using var wb = new XLWorkbook();

        var ws = wb.Worksheets.Add("Schedule");
        ws.Cell(1, 1).Value = "ExamId";
        ws.Cell(1, 2).Value = "Day";
        ws.Cell(1, 3).Value = "Session";
        ws.Cell(1, 4).Value = "Grade";
        ws.Cell(1, 5).Value = "Subject";
        ws.Cell(1, 6).Value = "Invigilators";
        ws.Cell(1, 7).Value = "Required";

        int r = 2;
        foreach (var e in result.Exams.OrderBy(x => x.Day).ThenBy(x => x.SessionIndex).ThenBy(x => x.GradeId))
        {
            ws.Cell(r, 1).Value = e.ExamSessionId;
            ws.Cell(r, 2).Value = e.Day;
            ws.Cell(r, 3).Value = e.SessionIndex;
            ws.Cell(r, 4).Value = e.GradeName;
            ws.Cell(r, 5).Value = e.SubjectName;
            ws.Cell(r, 6).Value = string.Join(" | ", e.Invigilators.Select(t => $"{t.Name} ({t.Subject})"));
            ws.Cell(r, 7).Value = e.RequiredInvigilators;
            r++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        var wb2 = wb.Worksheets.Add("Backups");
        wb2.Cell(1, 1).Value = "Day";
        wb2.Cell(1, 2).Value = "BackupCount";
        wb2.Cell(1, 3).Value = "Teachers";

        int r2 = 2;
        foreach (var b in result.Backups.OrderBy(x => x.Day))
        {
            wb2.Cell(r2, 1).Value = b.Day;
            wb2.Cell(r2, 2).Value = b.BackupCount;
            wb2.Cell(r2, 3).Value = string.Join(" | ", b.Teachers.Select(t => $"{t.Name} ({t.Subject})"));
            r2++;
        }

        wb2.Columns().AdjustToContents();
        wb2.SheetView.FreezeRows(1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
