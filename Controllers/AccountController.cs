using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InvigilatorSchedulerStandard.Data;
using InvigilatorSchedulerStandard.Models;

namespace InvigilatorSchedulerStandard.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _db;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                await _userManager.AddToRoleAsync(user, "User");
                
                // Seed default rules for the new user
                await SeedUserRules(user.Id);

                return RedirectToAction("Index", "Home");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View(model);
    }
    
    private async Task SeedUserRules(string userId)
    {
         var rules = new List<RuleConfig>
         {
            new RuleConfig
            {
                Code = RuleCodes.Settings,
                JsonValue = "{\"defaultInvigilatorsPerExam\":2,\"backupInvigilatorsPerDay\":1,\"randomSeed\":5,\"fairnessEnabled\":true}",
                UserId = userId
            },
            new RuleConfig
            {
                Code = RuleCodes.NoExamForBackupSameDay,
                JsonValue = "{\"enabled\":true}",
                UserId = userId
            },
            new RuleConfig
            {
                Code = RuleCodes.MaxSessionsPerTeacherPerDay,
                JsonValue = "{\"value\":2}",
                UserId = userId
            }
         };
         
         _db.RuleConfigs.AddRange(rules);
         await _db.SaveChangesAsync();
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVm model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Your account is disabled.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
