using MarFin_Final.Models;
using Microsoft.EntityFrameworkCore;

namespace MarFin_Final.Data
{
    public partial class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        public DbSet<AppDocument> AppDocuments { get; set; } = null!;

        public DbSet<Customer> Customers { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("tbl_Customers");

                entity.HasKey(e => e.CustomerId);
                entity.Property(e => e.CustomerId).HasColumnName("customer_id").ValueGeneratedOnAdd();

                // Map all existing columns exactly
                entity.Property(e => e.SegmentId).HasColumnName("segment_id");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
                entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(30);
                entity.Property(e => e.CompanyName).HasColumnName("company_name").HasMaxLength(150);
                entity.Property(e => e.Address).HasColumnName("address");
                entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
                entity.Property(e => e.StateProvince).HasColumnName("state_province").HasMaxLength(100);
                entity.Property(e => e.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
                entity.Property(e => e.Country).HasColumnName("country").HasMaxLength(100);
                entity.Property(e => e.CustomerStatus).HasColumnName("customer_status").HasMaxLength(20);
                entity.Property(e => e.TotalRevenue).HasColumnName("total_revenue").HasColumnType("decimal(12,2)");
                entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(100);
                entity.Property(e => e.Notes).HasColumnName("notes");
                entity.Property(e => e.IsActive).HasColumnName("is_active");

                entity.Property(e => e.CreatedDate).HasColumnName("created_date");
                entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");

                // Ignore any computed/display properties that are not in the DB
                entity.Ignore(e => e.CustomerSegment);
                entity.Ignore(e => e.FullName);
                entity.Ignore(e => e.SegmentName);
                entity.Ignore(e => e.IsArchived);
                entity.Ignore(e => e.ArchivedDate);
                entity.Ignore(e => e.ArchivedBy);
                entity.Ignore(e => e.ModifiedBy);
            });

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.ToTable("tbl_Invoices");
                entity.HasKey(e => e.InvoiceId);
                entity.Property(e => e.InvoiceId).HasColumnName("invoice_id").ValueGeneratedOnAdd();
                entity.Property(e => e.CustomerId).HasColumnName("customer_id");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(50).IsRequired();
                entity.Property(e => e.InvoiceDate).HasColumnName("invoice_date");
                entity.Property(e => e.DueDate).HasColumnName("due_date");
                entity.Property(e => e.PaymentTerms).HasColumnName("payment_terms").HasMaxLength(50);
                entity.Property(e => e.Subtotal).HasColumnName("subtotal").HasColumnType("decimal(12,2)");
                entity.Property(e => e.TaxRate).HasColumnName("tax_rate").HasColumnType("decimal(5,2)");
                entity.Property(e => e.TaxAmount).HasColumnName("tax_amount").HasColumnType("decimal(12,2)");
                entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount").HasColumnType("decimal(12,2)");
                entity.Property(e => e.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(12,2)");
                entity.Property(e => e.PaymentStatus).HasColumnName("payment_status").HasMaxLength(20);
                entity.Property(e => e.Notes).HasColumnName("notes");
                entity.Property(e => e.PdfPath).HasColumnName("pdf_path").HasMaxLength(255);
                entity.Property(e => e.IsArchived).HasColumnName("is_archived");
                entity.Property(e => e.ArchivedDate).HasColumnName("archived_date");
                entity.Property(e => e.CreatedDate).HasColumnName("created_date");
                entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");

                entity.HasIndex(e => e.InvoiceNumber).IsUnique();

                entity.Ignore(i => i.CustomerName);
                entity.Ignore(i => i.CustomerEmail);
                entity.Ignore(i => i.CustomerCompany);
                entity.Ignore(i => i.CreatedByName);
                entity.Ignore(i => i.IsOverdue);
                entity.Ignore(i => i.StatusBadgeClass);
            });

            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                entity.ToTable("tbl_Invoice_Items");
                entity.HasKey(e => e.ItemId);
                entity.Property(e => e.ItemId).HasColumnName("item_id").ValueGeneratedOnAdd();
                entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
                entity.Property(e => e.ItemOrder).HasColumnName("item_order");
                entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(200).IsRequired();
                entity.Property(e => e.Quantity).HasColumnName("quantity").HasColumnType("decimal(10,2)");
                entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(12,2)");
                entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(12,2)");

                entity.HasOne(d => d.Invoice)
                      .WithMany(p => p.LineItems)
                      .HasForeignKey(d => d.InvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            // In OnModelCreating
            modelBuilder.Entity<AppDocument>(entity =>
            {
                entity.ToTable("tbl_Documents");

                entity.HasKey(e => e.DocumentId);
                entity.Property(e => e.DocumentId).HasColumnName("document_id").ValueGeneratedOnAdd();

                entity.Property(e => e.CustomerId).HasColumnName("customer_id");
                entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by").IsRequired();
                entity.Property(e => e.DocumentName).HasColumnName("document_name").HasMaxLength(200).IsRequired();
                entity.Property(e => e.DocumentType).HasColumnName("document_type").HasMaxLength(50).IsRequired();
                entity.Property(e => e.FilePath).HasColumnName("file_path").HasMaxLength(255).IsRequired();
                entity.Property(e => e.FileSize).HasColumnName("file_size");
                entity.Property(e => e.MimeType).HasColumnName("mime_type").HasMaxLength(100);
                entity.Property(e => e.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
                entity.Property(e => e.ArchivedDate).HasColumnName("archived_date");
                entity.Property(e => e.UploadDate).HasColumnName("upload_date").HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Customer)
                      .WithMany()
                      .HasForeignKey(d => d.CustomerId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.UploadedByUser)
                      .WithMany()
                      .HasForeignKey(d => d.UploadedBy)
                      .HasPrincipalKey(u => u.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Ignore(d => d.CustomerDisplayName);
                entity.Ignore(d => d.UploadedByName);
                entity.Ignore(d => d.FormattedFileSize);
                entity.Ignore(d => d.FormattedUploadDate);
            });


        }
    }
}