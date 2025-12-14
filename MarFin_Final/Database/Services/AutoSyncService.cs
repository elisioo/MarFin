using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarFin_Final.Models;
using MarFin_Final.Services;

namespace MarFin_Final.Database.Services
{
    public class AutoSyncService : IDisposable
    {
        private readonly RemoteDatabaseService _remoteDbService;
        private readonly LocalDatabaseService _localDatabaseService;
        private readonly UserService _userService;
        private Timer? _timer;
        private bool _isSyncing;

        public bool AutoSyncEnabled => _timer != null;
        public int IntervalMinutes { get; private set; } = 15;
        public bool SyncCustomers { get; private set; } = true;
        public bool SyncInvoices { get; private set; } = true;

        public AutoSyncService(RemoteDatabaseService remoteDbService, LocalDatabaseService localDatabaseService, UserService userService)
        {
            _remoteDbService = remoteDbService;
            _localDatabaseService = localDatabaseService;
            _userService = userService;
        }

        public void ConfigureAutoSync(bool enabled, int intervalMinutes, bool syncCustomers, bool syncInvoices)
        {
            SyncCustomers = syncCustomers;
            SyncInvoices = syncInvoices;

            IntervalMinutes = intervalMinutes <= 0 ? 15 : intervalMinutes;

            if (enabled)
            {
                StartTimer();
            }
            else
            {
                StopTimer();
            }
        }

        private void StartTimer()
        {
            StopTimer();

            var minutes = IntervalMinutes <= 0 ? 15 : IntervalMinutes;
            var period = TimeSpan.FromMinutes(minutes);

            _timer = new Timer(async _ =>
            {
                await RunTimerSyncAsync();
            }, null, period, period);
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private async Task RunTimerSyncAsync()
        {
            if (_isSyncing)
            {
                return;
            }

            try
            {
                _isSyncing = true;
                await RunSyncOnceAsync(SyncCustomers, SyncInvoices);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AutoSyncService: Auto sync error: {ex.Message}");
            }
            finally
            {
                _isSyncing = false;
            }
        }

        public async Task<SyncResult> RunSyncOnceAsync(bool syncCustomers, bool syncInvoices)
        {
            var result = new SyncResult();

            try
            {
                var (isConnected, message) = await _remoteDbService.GetConnectionStatusAsync();
                if (!isConnected)
                {
                    result.IsSuccess = false;
                    result.Message = "Cannot connect to remote database. Please check your connection.";
                    return result;
                }

                if (!syncCustomers && !syncInvoices)
                {
                    result.IsSuccess = false;
                    result.Message = "Please select at least one data type (Customers or Invoices) to sync.";
                    return result;
                }

                try
                {
                    var remoteUsers = await _remoteDbService.GetAllRemoteUsersAsync();
                    if (remoteUsers != null && remoteUsers.Count > 0)
                    {
                        var syncedFromRemote = await _userService.SyncUsersFromRemoteAsync(remoteUsers);
                        Console.WriteLine($"AutoSyncService.RunSyncOnceAsync: Synced {syncedFromRemote} users from cloud to local.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AutoSyncService.RunSyncOnceAsync: Cloud-to-local user sync error: {ex.Message}");
                }

                try
                {
                    var users = await _localDatabaseService.GetAllUsersAsync();
                    if (users != null && users.Count > 0)
                    {
                        await _remoteDbService.SyncUsersToRemoteAsync(users);
                    }
                    else
                    {
                        Console.WriteLine("AutoSyncService.RunSyncOnceAsync: No local users found to sync.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AutoSyncService.RunSyncOnceAsync: User sync error: {ex.Message}");
                }

                var customers = await _localDatabaseService.GetAllCustomersAsync();
                var invoices = await _localDatabaseService.GetAllInvoicesAsync();

                var selectedCustomerCount = syncCustomers ? (customers?.Count ?? 0) : 0;
                var selectedInvoiceCount = syncInvoices ? (invoices?.Count ?? 0) : 0;

                if (selectedCustomerCount == 0 && selectedInvoiceCount == 0)
                {
                    result.IsSuccess = false;
                    result.Message = "No selected data found in local database to sync.";
                    return result;
                }

                var syncResult = await _remoteDbService.SyncAllDataAsync(
                    syncCustomers ? (customers ?? new List<Customer>()) : new List<Customer>(),
                    syncInvoices ? (invoices ?? new List<Invoice>()) : new List<Invoice>());

                return syncResult;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Sync failed: {ex.Message}";
                return result;
            }
        }

        public void Dispose()
        {
            StopTimer();
        }
    }
}
