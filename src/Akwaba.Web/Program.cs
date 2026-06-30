using Akwaba.Application.Services;
using Akwaba.Domain.Interfaces;
using Akwaba.Infrastructure;
using Akwaba.Infrastructure.Persistence;
using Akwaba.Web.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Couches Application + Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Contexte tenant/utilisateur (claims)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ContexteTenant>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<ContexteTenant>());
builder.Services.AddScoped<IUtilisateurCourant>(sp => sp.GetRequiredService<ContexteTenant>());
builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppliUtilisateur>, FabriqueClaims>();

// Services applicatifs (use cases)
builder.Services.AddScoped<ServiceReservation>();
builder.Services.AddScoped<ServiceFolio>();
builder.Services.AddScoped<ServiceCaisse>();
builder.Services.AddScoped<ServiceTableauBord>();
builder.Services.AddScoped<ServiceTenant>();
builder.Services.AddScoped<ServicePos>();
builder.Services.AddScoped<ServiceHousekeeping>();
builder.Services.AddScoped<ServiceImport>();
builder.Services.AddScoped<ServiceRapports>();

// Cookies & redirection de connexion
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/Compte/Connexion";
    opt.AccessDeniedPath = "/Compte/AccesRefuse";
    opt.ExpireTimeSpan = TimeSpan.FromHours(8);
    opt.SlidingExpiration = true;
    opt.Cookie.SameSite = SameSiteMode.Lax;
    // Cookie isolé des autres applications du même domaine (lp2medoune.com)
    opt.Cookie.Name = ".Akwaba.Auth";
    var pb = builder.Configuration["PATH_BASE"] ?? builder.Configuration["PathBase"];
    if (!string.IsNullOrWhiteSpace(pb)) opt.Cookie.Path = pb;
});

// SSO Google (activé seulement si configuré)
var authBuilder = builder.Services.AddAuthentication();
var googleId = builder.Configuration["Authentication:Google:ClientId"];
var googleSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleId) && !string.IsNullOrWhiteSpace(googleSecret))
{
    authBuilder.AddGoogle(o => { o.ClientId = googleId; o.ClientSecret = googleSecret; });
}

builder.Services.AddControllersWithViews();

// Production : persistance des clés (cookies stables après redémarrage) + reverse proxy
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")));
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.UseForwardedHeaders();

// Préfixe d'URL optionnel (déploiement derrière un reverse proxy sous /gestionhotel par ex.)
var pathBase = builder.Configuration["PATH_BASE"] ?? builder.Configuration["PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
    app.UsePathBase(pathBase);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Accueil/Erreur");
    app.UseHsts();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Accueil}/{action=Index}/{id?}");

// Création de la base + seed de démonstration
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitialiserAsync(scope.ServiceProvider);
}

app.Run();
