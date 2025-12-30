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
            // --- Logger per gli endpoint ---
            var endpointLogger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // --- Logger per il middleware / access denied ---
            var securityLogger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.File("logs/access-denied-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            // --- Usa Serilog per i controller / endpoint ---
            builder.Host.UseSerilog((ctx, lc) => lc
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            );

            // --- Bind configurazioni ---
            builder.Services.Configure<EndPointCacheConfig>(
                builder.Configuration.GetSection("EndPointCacheConfig"));

            builder.Services.Configure<SecurityOptions>(
                builder.Configuration.GetSection("Security"));

            // --- Servizi principali ---
            builder.Services.AddControllers();
            builder.Services.AddAuthentication(IISDefaults.AuthenticationScheme);
            builder.Services.AddAuthorization();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseHttpsRedirection();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.GetTempPath()),
                RequestPath = "/temp"
            });

            app.UseAuthentication();

            // Passiamo il logger dedicato al middleware
            app.UseMiddleware<ValidateWindowsUserMiddleware>(securityLogger);

            app.UseAuthorization();

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
