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
    }
}
