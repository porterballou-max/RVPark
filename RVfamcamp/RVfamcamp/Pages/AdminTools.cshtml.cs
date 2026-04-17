using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RVfamcamp.Models;
using RVfamcamp.Services;

//This acts as a total block to admin tools. If someone without "Admin" role tries to access this page, they will be denied.
[Authorize(Roles = "Admin")]
public class AdminToolsModel : PageModel
{
    private readonly DatabaseStatements _db;

    public AdminToolsModel(DatabaseStatements db)
    {
        _db = db;
    }

    public List<UserAccount> Users { get; set; } = new();
    public List<LotType> LotTypes { get; set; } = new();

    // BindProperty(SupportsGet = true) allows these to be set via the URL/Search bar
    [BindProperty(SupportsGet = true)]
    public string? UserSearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? RoleFilter { get; set; }

    public void OnGet()
    {
        // Fetch all users, then filter them in memory (or update your SQL to filter)
        var allUsers = _db.GetAllUsers();

        Users = allUsers.Where(u =>
            (string.IsNullOrEmpty(UserSearch) ||
             u.FirstName.Contains(UserSearch, StringComparison.OrdinalIgnoreCase) ||
             u.LastName.Contains(UserSearch, StringComparison.OrdinalIgnoreCase) ||
             u.Email.Contains(UserSearch, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(RoleFilter) || u.Role == RoleFilter)
        ).ToList();

        LotTypes = _db.GetAllLotTypes();
    }

    public IActionResult OnPostUpdateRole(int id, string newRole)
    {
        _db.UpdateUserRole(id, newRole);
        return RedirectToPage();
    }

    public IActionResult OnPostUpdatePrice(int lotTypeId, decimal newPrice)
    {
        _db.UpdateLotTypeBasePrice(lotTypeId, newPrice);
        return RedirectToPage();
    }
}