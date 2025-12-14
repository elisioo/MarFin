using MarFin_Final.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MarFin_Final.Data
{
    public class AppDocumentService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public AppDocumentService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<AppDocument>> GetDocumentsAsync(
            string? searchTerm = null,
            string? category = null)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var query = context.AppDocuments
                .AsNoTracking()
                .Where(d => !d.IsArchived);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(d => d.DocumentName.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(category) && category != "all")
            {
                string type = category switch
                {
                    "contracts" => "Contract",
                    "invoices" => "Invoice",
                    "proposals" => "Proposal",
                    "other" => "Other",
                    _ => "Other"
                };
                query = query.Where(d => d.DocumentType == type);
            }

            var documents = await query
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();

            await PopulateDisplayFieldsAsync(documents, context);
            return documents;
        }

        public async Task<AppDocument?> GetDocumentByIdAsync(int id)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var doc = await context.AppDocuments.FirstOrDefaultAsync(d => d.DocumentId == id);
            if (doc != null)
                await PopulateDisplayFieldsAsync(new List<AppDocument> { doc }, context);
            return doc;
        }

        public async Task<bool> UploadDocumentAsync(AppDocument document, Stream fileStream, string fileName)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            document.DocumentName = fileName;
            document.UploadDate = DateTime.Now;
            document.FileSize = (int)fileStream.Length;
            document.MimeType = GetMimeType(fileName);

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid() + Path.GetExtension(fileName);
            var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs);
            }

            document.FilePath = "/uploads/" + uniqueFileName;

            context.AppDocuments.Add(document);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var doc = await context.AppDocuments.FindAsync(id);
            if (doc == null) return false;

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", doc.FilePath.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            context.AppDocuments.Remove(doc);
            await context.SaveChangesAsync();
            return true;
        }

        private async Task PopulateDisplayFieldsAsync(List<AppDocument> documents, AppDbContext context)
        {
            if (!documents.Any()) return;

            var customerIds = documents.Where(d => d.CustomerId.HasValue).Select(d => d.CustomerId!.Value).Distinct().ToList();
            var customers = await context.Customers
                .Where(c => customerIds.Contains(c.CustomerId))
                .ToDictionaryAsync(c => c.CustomerId, c => $"{c.FirstName} {c.LastName} ({c.CompanyName})");

            foreach (var doc in documents)
            {
                if (doc.CustomerId.HasValue && customers.TryGetValue(doc.CustomerId.Value, out var name))
                    doc.CustomerDisplayName = name;
                else
                    doc.CustomerDisplayName = "No Customer";
            }
        }

        private string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
        }
    }
}