// ============================================================
// Program.cs – Einstiegspunkt der ASP.NET Core Web-API
// Konfiguriert: Oracle-Datenbankverbindung, CORS für Vue.js,
//               JSON-Serialisierung (Endlosschleifen verhindern),
//               OpenAPI/Scalar Dokumentation
// ============================================================

using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using csharp_webapi.Data;
using Oracle.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Datenbankkontext registrieren (Oracle EF Core) ───────────────────────────
// Die Verbindungszeichenfolge kommt aus appsettings.json → ConnectionStrings → OracleConnection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(
        builder.Configuration.GetConnectionString("OracleConnection"),
        ob => ob.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)
    ));

// ── JSON-Serialisierung konfigurieren ────────────────────────────────────────
// ReferenceHandler.IgnoreCycles: Verhindert StackOverflow bei zirkulären Referenzen
// (z.B. Kunde → Abrechnungen → Kunde → ...)
// PropertyNamingPolicy = camelCase: Stellt sicher dass JSON-Felder korrekt benannt sind
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ── CORS-Policy für Vue.js Frontend ─────────────────────────────────────────
// Erlaubt Anfragen von allen typischen Vue-CLI / Vite Ports
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueCorsPolicy", policy =>
    {
        policy
            // Vue CLI (Port 8080), Vite (Port 5173), lokale Dev-Varianten
            .WithOrigins(
                "http://localhost:8080",
                "http://localhost:5173",
                "http://localhost:3000",
                "http://127.0.0.1:8080",
                "http://127.0.0.1:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ── OpenAPI für Scalar API-Referenz ─────────────────────────────────────────
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Datenbank beim Start sicherstellen ───────────────────────────────────────
// EnsureCreated() erstellt die Tabellen wenn sie noch nicht existieren
// und führt die Seed-Daten aus AppDbContext.OnModelCreating aus.
// ACHTUNG: Für Produktionsumgebungen → Migrations verwenden!
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ── HTTP-Pipeline konfigurieren ──────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    // Scalar API-Referenz unter /scalar/v1 erreichbar
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// HTTPS-Weiterleitung (nur in Produktion sinnvoll; in Dev kann dies deaktiviert werden)
app.UseHttpsRedirection();

// CORS muss VOR UseAuthorization und MapControllers eingebunden werden!
app.UseCors("VueCorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run("http://0.0.0.0:5000");
