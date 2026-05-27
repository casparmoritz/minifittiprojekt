// ============================================================
// AppDbContext.cs – Entity Framework Core Datenbankkontext
// Konfiguriert alle Tabellen, Beziehungen und Seed-Daten
// ============================================================

using Microsoft.EntityFrameworkCore;
using csharp_webapi.Models;

namespace csharp_webapi.Data
{
    public class AppDbContext : DbContext
    {
        // Konstruktor: bekommt die Datenbankoptionen per Dependency Injection
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // ── DbSets = je eine Eigenschaft pro Oracle-Tabelle ──────────────────
        public DbSet<TblBank>       Banken      { get; set; }
        public DbSet<TblKonto>      Konten      { get; set; }
        public DbSet<TblKunde>      Kunden      { get; set; }
        public DbSet<TblAbo>        Abos        { get; set; }
        public DbSet<TblErmaessigte> Ermaessigte { get; set; }
        public DbSet<TblKundenAbo>  KundenAbos  { get; set; }
        public DbSet<TblAbrechnung> Abrechnungen { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Beziehungen konfigurieren ────────────────────────────────────

            // TblKonto → TblBank (N:1)
            // Löschen einer Bank ist nicht erlaubt solange Konten darauf verweisen
            modelBuilder.Entity<TblKonto>()
                .HasOne(k => k.Bank)
                .WithMany(b => b.Konten)
                .HasForeignKey(k => k.Bic)
                .OnDelete(DeleteBehavior.Restrict);

            // TblKunde → TblKonto (N:1)
            // Wird eine IBAN gelöscht, wird beim Kunden IBAN auf NULL gesetzt
            modelBuilder.Entity<TblKunde>()
                .HasOne(k => k.Konto)
                .WithMany(ko => ko.Kunden)
                .HasForeignKey(k => k.Iban)
                .OnDelete(DeleteBehavior.SetNull);

            // TblKundenAbo → TblKunde (N:1)
            modelBuilder.Entity<TblKundenAbo>()
                .HasOne(ka => ka.Kunde)
                .WithMany()
                .HasForeignKey(ka => ka.Kundennr)
                .OnDelete(DeleteBehavior.Cascade);

            // TblKundenAbo → TblAbo (N:1)
            modelBuilder.Entity<TblKundenAbo>()
                .HasOne(ka => ka.Abo)
                .WithMany()
                .HasForeignKey(ka => ka.Abonr)
                .OnDelete(DeleteBehavior.Restrict);

            // TblAbrechnung → TblKunde (N:1, nullable)
            modelBuilder.Entity<TblAbrechnung>()
                .HasOne(a => a.Kunde)
                .WithMany(k => k.Abrechnungen)
                .HasForeignKey(a => a.Kundennr)
                .OnDelete(DeleteBehavior.Cascade);

            // TblAbrechnung → TblAbo (N:1)
            modelBuilder.Entity<TblAbrechnung>()
                .HasOne(a => a.Abo)
                .WithMany(ab => ab.Abrechnungen)
                .HasForeignKey(a => a.Abonr)
                .OnDelete(DeleteBehavior.Restrict);

            // TblAbrechnung → TblErmaessigte (N:1, nullable)
            modelBuilder.Entity<TblAbrechnung>()
                .HasOne(a => a.Ermaessigte)
                .WithMany(e => e.Abrechnungen)
                .HasForeignKey(a => a.Ermid)
                .OnDelete(DeleteBehavior.Restrict);

            // TblAbrechnung → TblKundenAbo (N:1, nullable)
            modelBuilder.Entity<TblAbrechnung>()
                .HasOne(a => a.KundenAbo)
                .WithMany(ka => ka.Abrechnungen)
                .HasForeignKey(a => a.Kundenabonr)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Seed-Daten: Werden beim ersten Start in die Oracle-DB eingetragen ──

            // Banken
            modelBuilder.Entity<TblBank>().HasData(
                new TblBank { Bic = "SPKADEFFFXX", BankName = "Sparkasse" },
                new TblBank { Bic = "DEUTDEDDFXX", BankName = "Deutsche Bank" }
            );

            // Konten
            modelBuilder.Entity<TblKonto>().HasData(
                new TblKonto { Iban = "DE89370400440532013000", Bic = "SPKADEFFFXX" },
                new TblKonto { Iban = "DE22100700000123456789", Bic = "DEUTDEDDFXX" }
            );

            // Kunden
            modelBuilder.Entity<TblKunde>().HasData(
                new TblKunde { Kundennr = 1, Vorname = "Max",   Nachname = "Mustermann", Iban = "DE89370400440532013000" },
                new TblKunde { Kundennr = 2, Vorname = "Erika", Nachname = "Musterfrau", Iban = "DE22100700000123456789" }
            );

            // Abo-Typen
            modelBuilder.Entity<TblAbo>().HasData(
                new TblAbo
                {
                    Abonr       = 1,
                    Kuendigsfrist = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    Kurs        = true,
                    Getraenke   = true,
                    Grundpreis  = 49.99m,
                    Laufzeit    = "1-0"
                },
                new TblAbo
                {
                    Abonr       = 2,
                    Kuendigsfrist = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    Kurs        = false,
                    Getraenke   = false,
                    Grundpreis  = 19.99m,
                    Laufzeit    = "0-6"
                }
            );

            // Ermäßigungen
            modelBuilder.Entity<TblErmaessigte>().HasData(
                new TblErmaessigte { Ermid = 1, Ermaessigungssatz = 0.20m }, // 20% z.B. Student
                new TblErmaessigte { Ermid = 2, Ermaessigungssatz = 0.00m }  // 0%  = keine Ermäßigung
            );

            // KundenAbos (Laufende Verträge)
            modelBuilder.Entity<TblKundenAbo>().HasData(
                new TblKundenAbo
                {
                    Kundenabonr = 1,
                    Kundennr    = 1,
                    Abonr       = 1,
                    Startdatum  = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Enddatum    = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    Status      = "AKTIV"
                },
                new TblKundenAbo
                {
                    Kundenabonr = 2,
                    Kundennr    = 2,
                    Abonr       = 2,
                    Startdatum  = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Enddatum    = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                    Status      = "AKTIV"
                }
            );

            // Abrechnungen für Mai 2026
            modelBuilder.Entity<TblAbrechnung>().HasData(
                new TblAbrechnung
                {
                    Abrechnungsnr    = 1,
                    Kundennr         = 1,
                    Abonr            = 1,
                    Ermid            = 1,
                    Kundenabonr      = 1,
                    Rechnungsbetrag  = 39.99m, // 49.99 * (1 - 0.20) = 39.992
                    Abrechnungsmonat = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new TblAbrechnung
                {
                    Abrechnungsnr    = 2,
                    Kundennr         = 2,
                    Abonr            = 2,
                    Ermid            = 2,
                    Kundenabonr      = 2,
                    Rechnungsbetrag  = 19.99m,
                    Abrechnungsmonat = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
