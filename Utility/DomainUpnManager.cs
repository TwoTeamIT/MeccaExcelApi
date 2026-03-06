using Microsoft.AspNetCore.Authentication;
using System.DirectoryServices.AccountManagement;

namespace DucatiMeccaExcelApi.Utility
{
    public static class DomainUpnManager
    {
        public static string GetUpnFromDomain(string samAccountName, SecurityOptions _options)
        {
            if (string.IsNullOrEmpty(samAccountName) || string.IsNullOrEmpty(_options.UpnDomain))
                return samAccountName;
            var parts = samAccountName.Split('\\');
            if (parts.Length != 2)
                return samAccountName;
            var upn = $"{parts[1]}@{_options.UpnDomain}";
            return upn;
        }

        public static string GetUpnFromActiveDirectory(string samAccountName, SecurityOptions _options)
        {
            // Gestisce sia "DOMINIO\utente" che "utente" semplice
            var parts = samAccountName.Split('\\');
            var accountName = parts.Length > 1 ? parts[1] : parts[0];

            if (string.IsNullOrWhiteSpace(accountName))
                return "Anonymous";

            using var ctx = new PrincipalContext(ContextType.Domain, _options.DomainLink);
            var user = UserPrincipal.FindByIdentity(ctx, accountName);
            return user?.UserPrincipalName ?? "Anonymous";
        }
    }
}
