using DucatiMeccaExcelApi.Utility;
using Microsoft.Extensions.Options;
using System.Security.Claims;

public class ValidateWindowsUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityOptions _options;
    private readonly ILogger<ValidateWindowsUserMiddleware> _logger;

    public ValidateWindowsUserMiddleware(
        RequestDelegate next,
        IOptions<SecurityOptions> options,
        ILogger<ValidateWindowsUserMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var identity = context.User?.Identity;

        // 🔹 1. Utente non autenticato → lascio passare
        if (identity == null || !identity.IsAuthenticated)
        {
            await _next(context);
            return;
        }

        var userName = identity.Name ?? string.Empty;
        var authType = identity.AuthenticationType ?? "Unknown";

        // 🔹 2. Recupero UPN dai claim, se presente
        var upn = context.User.Claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.Upn ||
            c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value;

        // 🔹 3. Validazione dominio
        bool isAuthorized =
            userName.StartsWith($"{_options.RequiredDomain}\\", StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(upn) &&
             upn.EndsWith($"@{_options.UpnDomain}", StringComparison.OrdinalIgnoreCase));

        if (!isAuthorized)
        {
            _logger.LogWarning(
                "ACCESSO NEGATO - Dominio non autorizzato. User: {User}, UPN: {UPN}, AuthType: {AuthType}, Path: {Path}, IP: {IP}",
                userName,
                upn ?? "(null)",
                authType,
                context.Request.Path,
                context.Connection.RemoteIpAddress
            );

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Accesso negato.");
            return;
        }

        // 🔹 4. Log accesso valido
        _logger.LogInformation(
            "ACCESSO CONSENTITO. User: {User}, UPN: {UPN}, AuthType: {AuthType}, Path: {Path}, IP: {IP}",
            userName,
            upn ?? "(null)",
            authType,
            context.Request.Path,
            context.Connection.RemoteIpAddress
        );

        // 🔹 5. Passaggio al middleware successivo
        await _next(context);
    }
}