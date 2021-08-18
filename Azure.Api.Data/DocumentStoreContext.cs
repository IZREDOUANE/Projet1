using Microsoft.EntityFrameworkCore;
using System;
using Azure.Api.Data.Models;

namespace Azure.Api.Data
{
    public class DocumentStoreContext : DbContext
    {
        public DocumentStoreContext(DbContextOptions<DocumentStoreContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Email>(m =>
            {
                m.HasKey(m => m.ID);

                m.HasMany(m => m.EmailDocuments)
                    .WithOne(ed => ed.EmailNavFk);

                m.Property(m => m.Title)
                    .IsUnicode(false)
                    .IsRequired(false);

                m.Property(m => m.Recipient)
                    .IsUnicode(false)
                    .IsRequired(true);

                m.Property(m => m.RecipientCc)
                    .IsUnicode(false)
                    .IsRequired(false);

                m.Property(m => m.Object)
                    .HasDefaultValue("")
                    .IsRequired(true);

                m.Property(m => m.Content)
                    .HasDefaultValue("")
                    .IsRequired(true);

                m.Property(m => m.SendingDate)
                    .HasDefaultValue(DateTime.Now)
                    .IsRequired(true);
            });

            modelBuilder.Entity<EmailDocument>(e =>
            {
                e.HasAlternateKey(ed => ed.Id);

                e.HasOne(ed => ed.EmailNavFk)
                    .WithMany(e => e.EmailDocuments)
                    .HasForeignKey(ed => ed.EmailID)
                    .IsRequired();
            });

            modelBuilder.Entity<User>(u =>
            {
                u.Property(u => u.GUID)
                    .HasDefaultValue(Guid.NewGuid());

                u.HasKey(u => u.GUID);

                u.HasMany(u => u.UserAccessFkNav)
                    .WithOne(a => a.UserFkNav)
                    .HasForeignKey(a => a.UserGUID)
                    .OnDelete(DeleteBehavior.Cascade);

                u.HasOne(u => u.UserPasswordFkNav)
                    .WithOne(p => p.UserFkNav)
                    .HasForeignKey<UserPassword>(p => p.UserGUID)
                    .OnDelete(DeleteBehavior.Cascade);

                u.Property(u => u.FirstName)
                    .IsUnicode(true)
                    .IsRequired(true);

                u.Property(u => u.LastName)
                    .IsUnicode(true)
                    .IsRequired(true);

                u.Property(u => u.PhoneNumber)
                    .IsRequired(false);

                u.Property(u => u.Login)
                    .IsRequired(true);

                u.Property(u => u.ConfirmEmailToken)
                    .IsRequired(false);
            });

            modelBuilder.Entity<UserPassword>(p =>
            {
                p.HasKey(p => p.ID);

                p.Property(p => p.HashedPassword)
                    .IsRequired(true);

                p.Property(p => p.SaltKey)
                    .IsRequired(true);

                p.HasOne(p => p.UserFkNav)
                    .WithOne(u => u.UserPasswordFkNav)
                    .HasForeignKey<UserPassword>(p => p.UserGUID)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserSalesforceLink>(s =>
            {
                s.HasKey(s => new { s.UserGUID, s.SalesforceAccountId });

                s.HasOne(s => s.UserNavFk)
                .WithMany(u => u.UserSfLinkFkNav)
                .OnDelete(DeleteBehavior.Cascade);

                s.Property(s => s.SalesforceAccountId)
                    .HasMaxLength(18);
            });

            modelBuilder.Entity<Admin>(a =>
            {
                a.HasKey(a => a.ID);

                a.HasMany(a => a.AdminAccessFkNav)
                    .WithOne(aa => aa.AdminFkNav)
                    .HasForeignKey(aa => aa.AdminID)
                    .OnDelete(DeleteBehavior.Cascade);

                a.Property(a => a.ADGUID)
                    .IsRequired(true);
            });

            modelBuilder.Entity<Document>(d =>
            {
                d.HasKey(d => d.GUID);

                d.HasOne(d => d.DocumentTypeFkNav)
                    .WithMany(t => t.Documents)
                    .HasForeignKey(d => d.FileCategory);

                d.HasMany(d => d.DocumentAccessFkNav)
                    .WithOne(a => a.DocumentFkNav)
                    .HasForeignKey(d => d.DocumentGUID)
                    .OnDelete(DeleteBehavior.Cascade);

                d.HasMany(d => d.DocumentVersionsFkNav)
                    .WithOne(v => v.DocumentFkNav)
                    .HasForeignKey(v => v.DocumentGUID)
                    .OnDelete(DeleteBehavior.Cascade);

                d.Property(d => d.GUID)
                    .IsRequired(true);

                d.Property(d => d.SfId)
                    .HasMaxLength(18)
                    .IsRequired(false);

                d.Property(d => d.FileName)
                    .IsRequired(true);
            });

            modelBuilder.Entity<DocumentVersion>(v =>
            {
                v.HasKey(v => v.BlobGUID);

                v.HasOne(v => v.DocumentFkNav)
                    .WithMany(d => d.DocumentVersionsFkNav)
                    .HasForeignKey(v => v.DocumentGUID)
                    .OnDelete(DeleteBehavior.Cascade);

                v.Property(d => d.FileExtension)
                    .IsRequired(true);

                v.Property(d => d.Date)
                    .HasDefaultValue(DateTime.Now)
                    .IsRequired(true);

                v.Property(d => d.FileSize)
                    .HasDefaultValue(0)
                    .IsRequired(true);

                v.Property(d => d.CheckSum)
                    .HasMaxLength(64);

                v.Property(d => d.ModifiedBy)
                    .HasDefaultValue("")
                    .IsRequired(true);
            });

            modelBuilder.Entity<DocumentType>(t =>
            {
                t.HasKey(t => t.ID);

                t.HasMany(t => t.Documents)
                    .WithOne(d => d.DocumentTypeFkNav)
                    .HasForeignKey(d => d.FileCategory);

                t.Property(t => t.Name)
                    .IsUnicode(true)
                    .IsRequired(true);

                t.Property(t => t.IsRequired)
                    .HasDefaultValue(false);

                t.Property(t => t.IsUnique)
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<DocumentAccess>(a =>
            {
                a.HasKey(a => new { a.AllowedSfId, a.DocumentGUID });

                a.HasOne(a => a.DocumentFkNav)
                    .WithMany(d => d.DocumentAccessFkNav)
                    .HasForeignKey(a => a.DocumentGUID)
                    .OnDelete(DeleteBehavior.Cascade);

                a.Property(a => a.DocumentGUID)
                    .IsRequired(true);

                a.Property(a => a.AllowedSfId)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<Platform>(p =>
            {
                p.HasKey(p => p.ID);

                p.HasMany(p => p.UserAccessFkNav)
                    .WithOne(u => u.PlatformFkNav)
                    .HasForeignKey(u => u.PlatformID)
                    .OnDelete(DeleteBehavior.Cascade);

                p.HasMany(p => p.AdminAccessFkNav)
                    .WithOne(a => a.PlatformFkNav)
                    .HasForeignKey(a => a.PlatformID)
                    .OnDelete(DeleteBehavior.Cascade);

                p.Property(p => p.Name)
                    .IsUnicode(false)
                    .IsRequired(true);

                p.Property(p => p.DomainName)
                    .IsUnicode(false)
                    .IsRequired(true);
            });

            modelBuilder.Entity<UserAccess>(u =>
            {
                u.HasKey(u => new { u.UserGUID, u.PlatformID });

                u.HasOne(u => u.UserFkNav)
                    .WithMany(us => us.UserAccessFkNav)
                    .HasForeignKey(u => u.UserGUID)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                u.HasOne(u => u.PlatformFkNav)
                    .WithMany(p => p.UserAccessFkNav)
                    .HasForeignKey(u => u.PlatformID)
                    .IsRequired();
            });

            modelBuilder.Entity<AdminAccess>(a =>
            {
                a.Property(a => a.IsSuperAdmin)
                    .IsRequired(true)
                    .HasDefaultValue(false);

                a.HasKey(a => new { a.AdminID, a.PlatformID });

                a.HasOne(a => a.AdminFkNav)
                    .WithMany(aa => aa.AdminAccessFkNav)
                    .HasForeignKey(a => a.AdminID)
                    .IsRequired();

                a.HasOne(a => a.PlatformFkNav)
                    .WithMany(p => p.AdminAccessFkNav)
                    .HasForeignKey(a => a.PlatformID)
                    .IsRequired();
            });
        }

        public virtual DbSet<DocumentType> DocumentTypes { get; set; }

        public virtual DbSet<User> Users { get; set; }

        public virtual DbSet<UserPassword> UserPasswords { get; set; }

        public virtual DbSet<Admin> Admins { get; set; }

        public virtual DbSet<Platform> Platforms { get; set; }

        public virtual DbSet<UserAccess> UserAccess { get; set; }

        public virtual DbSet<AdminAccess> AdminAccess { get; set; }

        public virtual DbSet<UserSalesforceLink> UserSalesforceLinks { get; set; }

        public virtual DbSet<MonitoringFlux> MonitoringFlux { get; set; }

        public virtual DbSet<Email> Emails { get; set; }

        public virtual DbSet<Document> Documents { get; set; }
        public virtual DbSet<DocumentVersion> DocumentVersions { get; set; }

        public virtual DbSet<EmailDocument> EmailsDocuments { get; set; }

        public virtual DbSet<RepriseDocument> RepriseDocuments { get; set; }
    }
}
