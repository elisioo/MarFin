using System.Collections.Generic;
using System.Linq;

namespace MarFin_Final.Database.Services
{
    public static class RolePermissions
    {
        // Define all available permissions
        public static class Permissions
        {
            // Pages
            public const string ViewHome = "HOME";
            public const string ViewDashboard = "DASHBOARD";
            public const string ViewCustomers = "CUSTOMERS";
            public const string ViewSalesPipeline = "SALES";
            public const string ViewMarketing = "CAMPAIGNS";
            public const string ViewFinance = "FINANCE";
            public const string ViewInvoices = "INVOICES";
            public const string ViewTransactions = "TRANSACTIONS";
            public const string ViewReports = "REPORTS";
            public const string ViewDocuments = "DOCUMENTS";
            public const string ViewSettings = "SETTINGS";
            public const string ViewSegments = "SEGMENTS";
            public const string ViewInteractions = "INTERACTIONS";
            public const string ViewOpportunities = "OPPORTUNITIES";
        }

        // Define role permissions mapping
        private static readonly Dictionary<string, HashSet<string>> RolePermissionsMap = new()
        {
            ["Admin"] = new HashSet<string>
            {
                Permissions.ViewHome,
                Permissions.ViewDashboard,
                Permissions.ViewCustomers,
                Permissions.ViewSalesPipeline,
                Permissions.ViewMarketing,
                Permissions.ViewFinance,
                Permissions.ViewInvoices,
                Permissions.ViewTransactions,
                Permissions.ViewReports,
                Permissions.ViewDocuments,
                Permissions.ViewSettings,
                Permissions.ViewSegments,
                Permissions.ViewInteractions,
                Permissions.ViewOpportunities
            },
            ["Finance"] = new HashSet<string>
            {
                Permissions.ViewHome,
                Permissions.ViewDashboard, // Finance-specific dashboard
                Permissions.ViewFinance,
                Permissions.ViewInvoices,
                Permissions.ViewTransactions,
                Permissions.ViewReports // Finance reports only
            },
            ["Marketing"] = new HashSet<string>
            {
                Permissions.ViewHome,
                Permissions.ViewDashboard, // Marketing-specific dashboard
                Permissions.ViewCustomers,
                Permissions.ViewMarketing,
                Permissions.ViewSegments,
                Permissions.ViewReports // Marketing reports only
            },
            ["Sales Representative"] = new HashSet<string>
            {
                Permissions.ViewHome,
                Permissions.ViewDashboard, // Sales-specific dashboard
                Permissions.ViewCustomers,
                Permissions.ViewSalesPipeline,
                Permissions.ViewInteractions,
                Permissions.ViewOpportunities,
                Permissions.ViewReports // Sales reports only
            }
        };

        public static bool HasPermission(string role, string permission)
        {
            if (string.IsNullOrEmpty(role))
                return false;

            return RolePermissionsMap.TryGetValue(role, out var permissions)
                && permissions.Contains(permission);
        }

        public static HashSet<string> GetRolePermissions(string role)
        {
            return RolePermissionsMap.TryGetValue(role, out var permissions)
                ? permissions
                : new HashSet<string>();
        }

        public static bool CanAccessPage(string role, string page)
        {
            var pagePermissionMap = new Dictionary<string, string>
            {
                [""] = Permissions.ViewHome,
                ["dashboard"] = Permissions.ViewDashboard,
                ["customers"] = Permissions.ViewCustomers,
                ["customer-details"] = Permissions.ViewCustomers,
                ["sales-pipeline"] = Permissions.ViewSalesPipeline,
                ["marketing"] = Permissions.ViewMarketing,
                ["financial"] = Permissions.ViewFinance,
                ["reports"] = Permissions.ViewReports,
                ["documents"] = Permissions.ViewDocuments,
                ["settings"] = Permissions.ViewSettings
            };

            if (pagePermissionMap.TryGetValue(page.ToLower(), out var permission))
            {
                return HasPermission(role, permission);
            }

            return false;
        }
    }
}