using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using InvigilatorSchedulerStandard.Data;

namespace InvigilatorSchedulerStandard.Services;

public class RuleService
{
    private readonly AppDbContext _db;
    public RuleService(AppDbContext db) => _db = db;

    public sealed class SettingsRule
    {
        public int DefaultInvigilatorsPerExam { get; set; } = 2;
        public int BackupInvigilatorsPerDay { get; set; } = 0;
        public int RandomSeed { get; set; } = 1;
        public bool FairnessEnabled { get; set; } = true;
    }

    public sealed class EnabledRule { public bool Enabled { get; set; } = true; }
    public sealed class MaxRule { public int Value { get; set; } = 2; }

    public async Task<SettingsRule> GetSettings()
    {
        var row = await _db.RuleConfigs.AsNoTracking().FirstOrDefaultAsync(r => r.Code == RuleCodes.Settings);
        if (row == null) return new SettingsRule();
        return Deserialize(row.JsonValue, new SettingsRule());
    }

    public async Task SaveSettings(SettingsRule s)
    {
        var row = await _db.RuleConfigs.FirstOrDefaultAsync(r => r.Code == RuleCodes.Settings);
        if (row == null)
        {
            row = new RuleConfig { Code = RuleCodes.Settings, JsonValue = "{}" };
            _db.RuleConfigs.Add(row);
        }

        row.JsonValue = JsonSerializer.Serialize(new
        {
            defaultInvigilatorsPerExam = s.DefaultInvigilatorsPerExam,
            backupInvigilatorsPerDay = s.BackupInvigilatorsPerDay,
            randomSeed = s.RandomSeed,
            fairnessEnabled = s.FairnessEnabled
        });

        await _db.SaveChangesAsync();
    }

    public async Task<EnabledRule> GetNoExamForBackupSameDay()
    {
        var row = await _db.RuleConfigs.AsNoTracking().FirstOrDefaultAsync(r => r.Code == RuleCodes.NoExamForBackupSameDay);
        if (row == null) return new EnabledRule { Enabled = true };
        return Deserialize(row.JsonValue, new EnabledRule { Enabled = true });
    }

    public async Task<MaxRule> GetMaxSessionsPerTeacherPerDay()
    {
        var row = await _db.RuleConfigs.AsNoTracking().FirstOrDefaultAsync(r => r.Code == RuleCodes.MaxSessionsPerTeacherPerDay);
        if (row == null) return new MaxRule { Value = 2 };
        return Deserialize(row.JsonValue, new MaxRule { Value = 2 });
    }

    private static T Deserialize<T>(string json, T fallback)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? fallback;
        }
        catch { return fallback; }
    }
}
