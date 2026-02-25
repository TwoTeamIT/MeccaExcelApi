using DucatiMeccaExcelApi.Utility;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.FileProviders;
using Serilog;

namespace DucatiExcelApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // =======================
            // SERILOG CONFIGURATION
            // =======================
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()

                // LOG GENERALE
                .WriteTo.Console()
                .WriteTo.File(
                    "logs/log-.txt",
                    rollingInterval: RollingInterval.Day)

                // LOG SICUREZZA (solo dal middleware)
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e =>
                        e.Properties.ContainsKey("SourceContext") &&
                        e.Properties["SourceContext"].ToString().Contains("ValidateWindowsUserMiddleware"))
                    .WriteTo.File(
                        "logs/access-audit-.txt",
                        rollingInterval: RollingInterval.Day))
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            // =======================
            // Usa Serilog come host logger
            // =======================
            builder.Host.UseSerilog();

            // =======================
            // CONFIGURATION BINDING
            // =======================
            builder.Services.Configure<EndPointCacheConfig>(
                builder.Configuration.GetSection("EndPointCacheConfig"));

            builder.Services.Configure<SecurityOptions>(
                builder.Configuration.GetSection("Security"));

            // =======================
            // MVC / API
            // =======================
            builder.Services.AddControllers();

            // =======================
            // WINDOWS AUTHENTICATION
            // =======================
            builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
            builder.Services.AddAuthorization();

            // =======================
            // SWAGGER CONFIGURATION
            // =======================
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // =======================
            // LEGGE SE SWAGGER È ABILITATO
            // =======================
            var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger");

            if (swaggerEnabled)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ducati Excel API v1");

                    // Fondamentale per Windows Auth da browser
                    c.ConfigObject.AdditionalItems["withCredentials"] = true;
                });
            }

            // =======================
            // STATIC FILES
            // =======================
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.GetTempPath()),
                RequestPath = "/temp"
            });

            // =======================
            // AUTH PIPELINE (ordine critico)
            // =======================
            app.UseAuthentication();
            app.UseAuthorization();

            // =======================
            // MIDDLEWARE CUSTOM
            // =======================
            app.UseMiddleware<ValidateWindowsUserMiddleware>();

            // =======================
            // ROUTING
            // =======================
            app.MapControllers();

            try
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Applicazione avviata correttamente");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Errore fatale all'avvio dell'applicazione");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
