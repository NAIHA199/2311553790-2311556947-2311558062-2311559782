using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace LibraryAdvanced.Models;

public partial class LibraryAdvancedDbContext : DbContext
{
    public LibraryAdvancedDbContext()
    {
    }

    public LibraryAdvancedDbContext(DbContextOptions<LibraryAdvancedDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<LoanDetail> LoanDetails { get; set; }

    public virtual DbSet<LoanTicket> LoanTickets { get; set; }
    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.\\SQL2025;Database=LibraryAdvancedDB;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Books__3214EC07BE8C71CD");

            entity.Property(e => e.Author).HasMaxLength(150);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Category).WithMany(p => p.Books)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK_Books_Categories");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC07344EC216");

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<LoanDetail>(entity =>
        {
            entity.HasKey(e => new { e.LoanTicketId, e.BookId }).HasName("PK__LoanDeta__AA05E29C6002653B");

            entity.HasOne(d => d.Book).WithMany(p => p.LoanDetails)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK_Details_Books");

            entity.HasOne(d => d.LoanTicket).WithMany(p => p.LoanDetails)
                .HasForeignKey(d => d.LoanTicketId)
                .HasConstraintName("FK_Details_Tickets");
        });

        modelBuilder.Entity<LoanTicket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LoanTick__3214EC0782D8F766");

            entity.Property(e => e.BorrowDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.BorrowerName).HasMaxLength(150);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Borrowed");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
