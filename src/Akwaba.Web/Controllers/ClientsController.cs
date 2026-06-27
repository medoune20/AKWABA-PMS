using Akwaba.Domain.Entites;
using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Web.Controllers;

public class ClientsController(AkwabaDbContext db) : ControleurApp(db)
{
    public async Task<IActionResult> Index(string? q)
    {
        var query = Db.Clients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(c => c.NomComplet.Contains(q) || (c.Telephone != null && c.Telephone.Contains(q)));
        ViewBag.Q = q;
        return View(await query.OrderBy(c => c.NomComplet).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Creer(string nomComplet, string? telephone, string? email, string? typePiece, string? numPiece)
    {
        if (!string.IsNullOrWhiteSpace(nomComplet))
        {
            Db.Clients.Add(new Client
            {
                NomComplet = nomComplet, Telephone = telephone, Email = email,
                TypePiece = typePiece, NumPiece = numPiece
            });
            await Db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
