using MarFin_Final.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarFin_Final.Data;
using MarFin_Final.Services;
using Microsoft.AspNetCore.Components;

namespace MarFin_Final.Database.Services
{
    public class LocalDatabaseService
    {
        private readonly CustomerService _customerService;
        private readonly InvoiceService _invoiceService;
        private readonly UserService _userService;

        public LocalDatabaseService(CustomerService customerService, InvoiceService invoiceService, UserService userService)
        {
            _customerService = customerService;
            _invoiceService = invoiceService;
            _userService = userService;
        }

        public async Task<List<MarFin_Final.Models.Customer>> GetAllCustomersAsync()
        {
            try
            {
                // Use the existing sync method to get ALL customers (including archived/inactive)
                var customers = await Task.Run(() => _customerService.GetAllCustomersForSync());
                Console.WriteLine($"LocalDatabaseService: Fetched {customers?.Count ?? 0} customers from local database (including archived/inactive)");
                return customers ?? new List<Customer>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocalDatabaseService Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return new List<Customer>();
            }
        }

        public async Task<List<MarFin_Final.Models.Invoice>> GetAllInvoicesAsync()
        {
            try
            {
                var invoices = await _invoiceService.GetAllInvoicesAsync();
                Console.WriteLine($"LocalDatabaseService: Fetched {invoices?.Count ?? 0} invoices from local database");
                return invoices ?? new List<Invoice>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocalDatabaseService Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return new List<Invoice>();
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                var users = await _userService.GetAllUsersForSyncAsync();
                Console.WriteLine($"LocalDatabaseService: Fetched {users?.Count ?? 0} users from local database");
                return users ?? new List<User>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LocalDatabaseService Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return new List<User>();
            }
        }
    }
}