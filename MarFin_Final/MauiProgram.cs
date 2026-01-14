using MarFin_Final.Data;
using MarFin_Final.Database.Services;
using MarFin_Final.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MarFin_Final
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Load configuration from embedded appsettings.json
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("MarFin_Final.appsettings.json");

            if (stream != null)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

                builder.Configuration.AddConfiguration(config);
            }
            //var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            //Service Registration

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

          
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddScoped<RoleService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<CustomerService>();
            builder.Services.AddScoped<CustomerSegmentService>();
            builder.Services.AddScoped<DashboardService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddSingleton<RemoteDatabaseService>();
            builder.Services.AddScoped<LocalDatabaseService>();
            builder.Services.AddScoped<AutoSyncService>();

            builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
            builder.Services.AddScoped<InvoiceService>();
            builder.Services.AddScoped<AppDocumentService>();



            builder.Services.AddBlazorWebViewDeveloperTools();



            return builder.Build();
        }
    }
}