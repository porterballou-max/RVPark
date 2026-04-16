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

    public void OnGet()
    {
        Users = _db.GetAllUsers();
        LotTypes = _db.GetAllLotTypes();
    }

    public IActionResult OnPostUpdateRole(int id, string newRole)
    {
        _db.UpdateUserRole(id, newRole);
        TempData["Flash.Success"] = "User role updated successfully.";
        return RedirectToPage();
    }

    public IActionResult OnPostUpdatePrice(int lotTypeId, decimal newPrice)
    {
        _db.UpdateLotTypeBasePrice(lotTypeId, newPrice);
        TempData["Flash.Success"] = "Lot price updated successfully.";
        return RedirectToPage();
    }
}