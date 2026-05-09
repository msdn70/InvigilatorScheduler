using Google.OrTools.Sat;
using Microsoft.EntityFrameworkCore;
using InvigilatorSchedulerStandard.Data;

namespace InvigilatorSchedulerStandard.Services;

public class InvigilatorSolveService
{
    private readonly AppDbContext _db;
    private readonly RuleService _rules;

    public InvigilatorSolveService(AppDbContext db, RuleService rules)
    {
        _db = db;
        _rules = rules;
    }

    public async Task<SolveResponse> Solve()
    {
        var notes = new List<string>();

        var grades = await _db.Grades.AsNoTracking().ToListAsync();
        var gradeNameById = grades.ToDictionary(g => g.Id, g => g.Name);

        var teachersRaw = await _db.Teachers
            .AsNoTracking()
            .Include(t => t.RestrictedExamSessions)
            .ToListAsync();

        var examsRaw = await _db.ExamSessions
            .AsNoTracking()
            .OrderBy(e => e.Day).ThenBy(e => e.SessionIndex).ThenBy(e => e.GradeId)
            .ToListAsync();

        if (teachersRaw.Count == 0) return new SolveResponse("NoData", new(), new(), new() { "No teachers found." });
        if (examsRaw.Count == 0) return new SolveResponse("NoData", new(), new(), new() { "No exam sessions found." });

        var settings = await _rules.GetSettings();
        var noExamForBackup = await _rules.GetNoExamForBackupSameDay();
        var maxSessions = await _rules.GetMaxSessionsPerTeacherPerDay();

        // Randomization: shuffle ordering using seed to avoid same result always
        var rng = new Random(settings.RandomSeed);
        var teachers = teachersRaw.OrderBy(_ => rng.Next()).ToList();
        var exams = examsRaw.OrderBy(_ => rng.Next()).ToList();

        int T = teachers.Count;
        int E = exams.Count;

        var restrictedByTeacherId = teachers.ToDictionary(
            t => t.Id,
            t => new HashSet<int>(t.RestrictedExamSessions.Select(r => r.ExamSessionId))
        );

        var days = exams.Select(e => e.Day).Distinct().OrderBy(x => x).ToList();

        // OR-Tools CP-SAT model
        var model = new CpModel();

        // x[(ti, ei)] = teacher ti assigned to exam ei (BoolVar)
        // We create x only if allowed (not restricted).
        var x = new Dictionary<(int ti, int ei), BoolVar>();

        for (int ti = 0; ti < T; ti++)
        {
            var teacher = teachers[ti];
            for (int ei = 0; ei < E; ei++)
            {
                var exam = exams[ei];
                if (restrictedByTeacherId[teacher.Id].Contains(exam.Id)) continue; // forbidden
                x[(ti, ei)] = model.NewBoolVar($"x_t{teacher.Id}_e{exam.Id}");
            }
        }

        // Each exam must get exactly required invigilators
        for (int ei = 0; ei < E; ei++)
        {
            var exam = exams[ei];
            int required = exam.InvigilatorsOverride > 0 ? exam.InvigilatorsOverride : settings.DefaultInvigilatorsPerExam;

            var vars = new List<ILiteral>();
            for (int ti = 0; ti < T; ti++)
                if (x.TryGetValue((ti, ei), out var v)) vars.Add(v);

            if (vars.Count < required)
                notes.Add($"Exam #{exam.Id} has only {vars.Count} eligible teachers but needs {required}.");

            model.Add(LinearExpr.Sum(vars) == required);
        }

        // No overlaps: teacher cannot invigilate two exams in same (day, session)
        var slotGroups = exams
            .Select((e, idx) => new { e.Day, e.SessionIndex, Index = idx })
            .GroupBy(z => (z.Day, z.SessionIndex))
            .ToList();

        foreach (var slot in slotGroups)
        {
            var examIdxs = slot.Select(s => s.Index).ToList();
            for (int ti = 0; ti < T; ti++)
            {
                var vars = new List<ILiteral>();
                foreach (var ei in examIdxs)
                    if (x.TryGetValue((ti, ei), out var v)) vars.Add(v);
                model.Add(LinearExpr.Sum(vars) <= 1);
            }
        }

        // Max sessions per teacher per day
        foreach (var day in days)
        {
            var dayExamIdxs = exams.Select((e, idx) => new { e, idx })
                                   .Where(z => z.e.Day == day)
                                   .Select(z => z.idx)
                                   .ToList();

            for (int ti = 0; ti < T; ti++)
            {
                var vars = new List<ILiteral>();
                foreach (var ei in dayExamIdxs)
                    if (x.TryGetValue((ti, ei), out var v)) vars.Add(v);
                model.Add(LinearExpr.Sum(vars) <= maxSessions.Value);
            }
        }

        // Backups: b[(ti, day)] = teacher ti is backup on that day
        int backupN = settings.BackupInvigilatorsPerDay;
        var b = new Dictionary<(int ti, string day), BoolVar>();

        for (int ti = 0; ti < T; ti++)
            foreach (var day in days)
                b[(ti, day)] = model.NewBoolVar($"b_t{teachers[ti].Id}_d{day}");

        foreach (var day in days)
        {
            var vars = new List<ILiteral>();
            for (int ti = 0; ti < T; ti++) vars.Add(b[(ti, day)]);
            model.Add(LinearExpr.Sum(vars) == backupN);
        }

        // If enabled: backup teacher cannot invigilate any exam that day
        if (noExamForBackup.Enabled && backupN > 0)
        {
            foreach (var day in days)
            {
                var dayExamIdxs = exams.Select((e, idx) => new { e, idx })
                                       .Where(z => z.e.Day == day)
                                       .Select(z => z.idx)
                                       .ToList();
                int M = Math.Max(1, dayExamIdxs.Count);

                for (int ti = 0; ti < T; ti++)
                {
                    var dayVars = new List<ILiteral>();
                    foreach (var ei in dayExamIdxs)
                        if (x.TryGetValue((ti, ei), out var v)) dayVars.Add(v);

                    model.Add(LinearExpr.Sum(dayVars) <= (1 - b[(ti, day)]) * M);
                }
            }
        }

        // Optional fairness objective: minimize maximum load across teachers
        if (settings.FairnessEnabled)
        {
            var loads = new IntVar[T];
            for (int ti = 0; ti < T; ti++)
            {
                var vars = new List<ILiteral>();
                for (int ei = 0; ei < E; ei++)
                    if (x.TryGetValue((ti, ei), out var v)) vars.Add(v);

                loads[ti] = model.NewIntVar(0, E, $"load_t{teachers[ti].Id}");
                model.Add(loads[ti] == LinearExpr.Sum(vars));
            }

            var maxLoad = model.NewIntVar(0, E, "maxLoad");
            model.AddMaxEquality(maxLoad, loads);
            model.Minimize(maxLoad);
        }

        var solver = new CpSolver { StringParameters = "max_time_in_seconds:10" };
        var status = solver.Solve(model);

        if (status != CpSolverStatus.Feasible && status != CpSolverStatus.Optimal)
            return new SolveResponse("Infeasible", new(), new(), notes.Count > 0 ? notes : new() { "No feasible schedule." });

        // Build output
        var examAssignments = new List<ExamAssignment>();
        for (int ei = 0; ei < E; ei++)
        {
            var exam = exams[ei];
            int required = exam.InvigilatorsOverride > 0 ? exam.InvigilatorsOverride : settings.DefaultInvigilatorsPerExam;

            var invs = new List<TeacherInfo>();
            for (int ti = 0; ti < T; ti++)
            {
                if (x.TryGetValue((ti, ei), out var v) && solver.Value(v) == 1)
                {
                    var t = teachers[ti];
                    invs.Add(new TeacherInfo(t.Id, t.Name, t.Subject));
                }
            }

            examAssignments.Add(new ExamAssignment(
                exam.Id,
                exam.Day,
                exam.SessionIndex,
                exam.GradeId,
                gradeNameById.TryGetValue(exam.GradeId, out var gn) ? gn : $"Grade#{exam.GradeId}",
                exam.SubjectName,
                required,
                invs
            ));
        }

        var backups = new List<DayBackups>();
        foreach (var day in days)
        {
            var list = new List<TeacherInfo>();
            for (int ti = 0; ti < T; ti++)
            {
                if (solver.Value(b[(ti, day)]) == 1)
                {
                    var t = teachers[ti];
                    list.Add(new TeacherInfo(t.Id, t.Name, t.Subject));
                }
            }
            backups.Add(new DayBackups(day, backupN, list));
        }

        return new SolveResponse("Feasible", examAssignments, backups, notes);
    }
}
