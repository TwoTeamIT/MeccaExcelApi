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
            // LOGGER ENDPOINT
            // =======================
            var endpointLogger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // =======================
            // LOGGER SICUREZZA
            // =======================
            var securityLogger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.File("logs/access-audit-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            // =======================
            // SERILOG HOST
            // =======================
            builder.Host.UseSerilog((ctx, lc) => lc
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            );

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
            // SWAGGER
            // =======================
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // =======================
            // SWAGGER UI (with credentials)
            // =======================
            //if (app.Environment.IsDevelopment())
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ducati Excel API v1");

                // Fondamentale per Windows Auth da browser
                c.ConfigObject.AdditionalItems["withCredentials"] = true;
            });

            // =======================
            // STATIC FILES
            // =======================
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.GetTempPath()),
                RequestPath = "/temp"
            });

            // =======================
            // AUTH PIPELINE (ORDINE CRITICO)
            // =======================
            app.UseAuthentication();
            app.UseAuthorization();

            // =======================
            // MIDDLEWARE CUSTOM
            // =======================
            app.UseMiddleware<ValidateWindowsUserMiddleware>(securityLogger);

            // =======================
            // ROUTING
            // =======================
            app.MapControllers();

            try
            {
                endpointLogger.Information("Applicazione avviata correttamente");
                app.Run();
            }
            catch (Exception ex)
            {
                endpointLogger.Fatal(ex, "Errore fatale all'avvio dell'applicazione");
            }
            finally
            {
                endpointLogger.Dispose();
                securityLogger.Dispose();
            }
        }
    }
}
