using MarFin_Final.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarFin_Final.Data
{
    public class InvoiceService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public InvoiceService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<bool> AddInvoiceAsync(Invoice invoice)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            invoice.InvoiceNumber = await GenerateInvoiceNumberAsync(context);
            invoice.CreatedDate = DateTime.Now;
            invoice.ModifiedDate = DateTime.Now;

            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var invoices = await context.Invoices
                .AsNoTracking()
                .Include(i => i.LineItems.OrderBy(item => item.ItemOrder))
                .Where(i => !i.IsArchived)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            await PopulateDisplayFieldsAsync(invoices, context);
            return invoices;
        }

        public async Task<List<Invoice>> GetInvoicesByCustomerIdAsync(int customerId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var invoices = await context.Invoices
                .AsNoTracking()
                .Include(i => i.LineItems.OrderBy(item => item.ItemOrder))
                .Where(i => !i.IsArchived && i.CustomerId == customerId)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            await PopulateDisplayFieldsAsync(invoices, context);
            return invoices;
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var invoice = await context.Invoices
                .Include(i => i.LineItems.OrderBy(item => item.ItemOrder))
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice != null)
            {
                await PopulateDisplayFieldsAsync(new List<Invoice> { invoice }, context);
            }

            return invoice;
        }

        public async Task<bool> UpdateInvoiceAsync(Invoice invoice)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            invoice.ModifiedDate = DateTime.Now;
            context.Invoices.Update(invoice);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ArchiveInvoiceAsync(int invoiceId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var invoice = await context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return false;

            invoice.IsArchived = true;
            invoice.ArchivedDate = DateTime.Now;
            await context.SaveChangesAsync();
            return true;
        }

        private async Task<string> GenerateInvoiceNumberAsync(AppDbContext context)
        {
            var last = await context.Invoices
                .OrderByDescending(i => i.InvoiceId)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int next = 1;
            if (last != null && last.StartsWith($"INV-{DateTime.Now.Year}-"))
            {
                var parts = last.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int num))
                    next = num + 1;
            }
            return $"INV-{DateTime.Now.Year}-{next:D3}";
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var lastInvoice = await context.Invoices
                .OrderByDescending(i => i.InvoiceId)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastInvoice) &&
                lastInvoice.StartsWith($"INV-{DateTime.Now.Year}-"))
            {
                var parts = lastInvoice.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int num))
                {
                    nextNumber = num + 1;
                }
            }

            return $"INV-{DateTime.Now.Year}-{nextNumber:D3}";
        }


        private async Task PopulateDisplayFieldsAsync(List<Invoice> invoices, AppDbContext context)
        {
            if (!invoices.Any()) return;

            var customerIds = invoices.Select(i => i.CustomerId).Distinct().ToList();

            var customers = await context.Customers
                .Where(c => customerIds.Contains(c.CustomerId))
                .ToDictionaryAsync(c => c.CustomerId, c => new
                {
                    FullName = $"{c.FirstName} {c.LastName}",
                    c.Email,
                    c.CompanyName
                });

            foreach (var invoice in invoices)
            {
                if (customers.TryGetValue(invoice.CustomerId, out var cust))
                {
                    invoice.CustomerName = cust.FullName;
                    invoice.CustomerEmail = cust.Email ?? string.Empty;
                    invoice.CustomerCompany = cust.CompanyName ?? string.Empty;
                }
                else
                {
                    invoice.CustomerName = "Unknown Customer";
                    invoice.CustomerCompany = "";
                }
            }
        }
        public async Task<List<Invoice>> GetArchivedInvoicesAsync()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var invoices = await context.Invoices
                .AsNoTracking()
                .Include(i => i.LineItems.OrderBy(item => item.ItemOrder))
                .Where(i => i.IsArchived)
                .OrderByDescending(i => i.ArchivedDate)
                .ToListAsync();

            await PopulateDisplayFieldsAsync(invoices, context);
            return invoices;
        }

        public async Task<bool> RestoreInvoiceAsync(int invoiceId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var invoice = await context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return false;

            invoice.IsArchived = false;
            invoice.ArchivedDate = null;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PermanentlyDeleteInvoiceAsync(int invoiceId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var invoice = await context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null) return false;

            context.Invoices.Remove(invoice);
            await context.SaveChangesAsync();
            return true;
        }
    }
}