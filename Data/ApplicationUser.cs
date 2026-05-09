using Microsoft.AspNetCore.Identity;

namespace InvigilatorSchedulerStandard.Data;

public class ApplicationUser : IdentityUser
{
    // Add custom properties here if needed
    public bool IsActive { get; set; } = true;
}

