using MarFin_Final.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarFin_Final.Data;

namespace MarFin_Final.Database.Services
{
    public class LocalDatabaseService
    {
        private readonly CustomerService _customerService;
        private readonly InvoiceService _invoiceService;

        public LocalDatabaseService()
        {
            _customerService = new CustomerService();
            _invoiceService = new InvoiceService();
        }

        public async Task<List<MarFin_Final.Models.Customer>> GetAllCustomersAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Use the sync method to get ALL customers (including archived/inactive)
                    var customers = _customerService.GetAllCustomersForSync();
                    Console.WriteLine($"LocalDatabaseService: Fetched {customers?.Count ?? 0} customers from local database (including archived/inactive)");
                    return customers ?? new List<Customer>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LocalDatabaseService Error: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    return new List<Customer>();
                }
            });
        }

        public async Task<List<MarFin_Final.Models.Invoice>> GetAllInvoicesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var invoices = _invoiceService.GetAllInvoices();
                    Console.WriteLine($"LocalDatabaseService: Fetched {invoices?.Count ?? 0} invoices from local database");
                    return invoices ?? new List<Invoice>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LocalDatabaseService Error: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    return new List<Invoice>();
                }
            });
        }
    }
}