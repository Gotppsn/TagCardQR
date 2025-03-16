using System;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;

namespace CardTagManager.Services
{
    public class LdapAuthenticationService
    {
        private readonly string _domain;

        public LdapAuthenticationService(string domain)
        {
            _domain = domain ?? "thaiparkerizing";
        }

        public bool ValidateCredentials(string username, string password)
        {
            // Support test admin user
            if (username == "admin" && password == "admin")
            {
                return true;
            }

            try
            {
                // For actual LDAP authentication
                using (var context = new PrincipalContext(ContextType.Domain, _domain))
                {
                    return context.ValidateCredentials(username, password);
                }
            }
            catch (Exception ex)
            {
                // Log exception silently, production code should use proper logging
                Console.WriteLine($"LDAP Authentication error: {ex.Message}");
                return false;
            }
        }
    }
}