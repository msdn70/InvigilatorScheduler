using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvigilatorSchedulerStandard.Data;

namespace InvigilatorSchedulerStandard.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User);
        // List all users except the current admin to prevent self-lockout risk
        var users = await _userManager.Users
            .Where(u => u.Id != currentUserId)
            .Select(u => new UserManagementVm
            {
                Id = u.Id,
                Email = u.Email,
                IsActive = u.IsActive
            })
            .ToListAsync();
            
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        return RedirectToAction(nameof(Index));
    }
}

public class UserManagementVm
{
    public string Id { get; set; } = "";
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}
