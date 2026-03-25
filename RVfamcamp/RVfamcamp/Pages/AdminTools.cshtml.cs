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

    public void OnGet()
    {
        Users = _db.GetAllUsers();
    }

    public IActionResult OnPostUpdateRole(int id, string newRole)
    {
        _db.UpdateUserRole(id, newRole);
        return RedirectToPage();
    }
}