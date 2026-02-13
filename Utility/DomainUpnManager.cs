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
            //using var ctx = new PrincipalContext(
            //                 ContextType.Domain,
            //                 _options.DomainLink);

            //var user = UserPrincipal.FindByIdentity(ctx, samAccountName.Split('\\')[1]);

            //string upn = user?.UserPrincipalName;
            string upn = "simona.guastella@ducati.com"; // Placeholder for testing without AD access

            return upn ?? "Anonymous";
        }
    }
}
