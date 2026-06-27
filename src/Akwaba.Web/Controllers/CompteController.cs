using Akwaba.Application.Services;
using Akwaba.Infrastructure.Persistence;
using Akwaba.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Akwaba.Web.Controllers;

public class CompteController(
    SignInManager<AppliUtilisateur> signIn,
    UserManager<AppliUtilisateur> users,
    ServiceTenant tenants,
    ILogger<CompteController> logger) : Controller
{
    [HttpGet]
    public IActionResult Connexion(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new ConnexionVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Connexion(ConnexionVm vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(vm);

        var res = await signIn.PasswordSignInAsync(vm.Email, vm.MotDePasse, vm.SeSouvenir, lockoutOnFailure: false);
        if (res.Succeeded)
            return Redirect(returnUrl ?? Url.Action("Index", "Accueil")!);

        // Message volontairement générique (pas de fuite d'information)
        ModelState.AddModelError(string.Empty, "Identifiants invalides.");
        return View(vm);
    }

    [HttpGet]
    public IActionResult Inscription() => View(new InscriptionVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Inscription(InscriptionVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        try
        {
            var tenant = await tenants.SouscrireAsync(vm.NomHotel, vm.SousDomaine.Trim().ToLowerInvariant(), vm.Ville, vm.Telephone);
            var user = new AppliUtilisateur
            {
                UserName = vm.Email, Email = vm.Email, EmailConfirmed = true,
                NomComplet = vm.NomComplet, TenantId = tenant.Id
            };
            var res = await users.CreateAsync(user, vm.MotDePasse);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }
            await users.AddToRoleAsync(user, "GERANT");
            await signIn.SignInAsync(user, isPersistent: false);
            return RedirectToAction("EnAttente");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [Authorize]
    public IActionResult EnAttente() => View();

    [HttpGet]
    public IActionResult MotDePasseOublie() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MotDePasseOublie(string email)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is not null)
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var lien = Url.Action("Reinitialiser", "Compte", new { email, token }, Request.Scheme);
            // En production : envoi par email (SMTP). En dev : on journalise et on affiche le lien.
            logger.LogInformation("Lien de réinitialisation pour {Email} : {Lien}", email, lien);
            ViewBag.Lien = lien;
        }
        // Réponse identique que l'email existe ou non (anti-énumération).
        ViewBag.Message = "Si un compte existe pour cette adresse, un lien de réinitialisation a été généré.";
        return View();
    }

    [HttpGet]
    public IActionResult Reinitialiser(string email, string token) =>
        View(new ReinitialiserVm { Email = email, Token = token });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reinitialiser(ReinitialiserVm vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var user = await users.FindByEmailAsync(vm.Email);
        if (user is not null)
        {
            var res = await users.ResetPasswordAsync(user, vm.Token, vm.MotDePasse);
            if (res.Succeeded) { ViewBag.Ok = true; return View(vm); }
            foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
        }
        else ModelState.AddModelError(string.Empty, "Lien invalide.");
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deconnexion()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Connexion");
    }

    public IActionResult AccesRefuse() => View();
}
