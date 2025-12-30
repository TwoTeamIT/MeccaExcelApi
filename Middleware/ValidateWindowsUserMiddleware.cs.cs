using DucatiMeccaExcelApi.Utility;
using Microsoft.Extensions.Options;
using Serilog;
using System.Runtime.ConstrainedExecution;

public class ValidateWindowsUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityOptions _options;
    private readonly Serilog.ILogger _securityLogger;

    public ValidateWindowsUserMiddleware(RequestDelegate next, IOptions<SecurityOptions> options, Serilog.ILogger securityLogger)
    {
        _next = next;
        _options = options.Value;
        _securityLogger = securityLogger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;

        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            _securityLogger.Warning("ACCESSO NEGATO - Utente non autenticato. Path: {Path}, IP: {IP}",
                context.Request.Path,
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Utente non autenticato.");
            return;
        }

        var name = user.Identity.Name ?? "";

        if (!name.StartsWith($"{_options.RequiredDomain}\\", StringComparison.OrdinalIgnoreCase))
        {
            _securityLogger.Warning("ACCESSO NEGATO - Dominio non autorizzato. Utente: {User}, AuthType: {AuthType}, Path: {Path}, IP: {IP}",
                name,
                user.Identity.AuthenticationType,
                context.Request.Path,
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Accesso negato: dominio non autorizzato.");
            return;
        }

        _securityLogger.Warning("ACCESSO Consentito - Dominio autorizzato. Utente: {User}, AuthType: {AuthType}, Path: {Path}, IP: {IP}",
                name,
                user.Identity.AuthenticationType,
                context.Request.Path,
                context.Connection.RemoteIpAddress);

        await _next(context);
    }
}

