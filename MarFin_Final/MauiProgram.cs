using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using MarFin_Final.Services;
using MarFin_Final.Database.Services;
using MarFin_Final.Data;

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
            builder.Services.AddScoped<DashboardService>();
            builder.Services.AddSingleton<RemoteDatabaseService>();
            builder.Services.AddSingleton<LocalDatabaseService>();


            return builder.Build();
        }
    }
}