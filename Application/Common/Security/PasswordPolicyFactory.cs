using Microsoft.Extensions.Configuration;

namespace Application.Common.Security
{
    public class PasswordPolicyFactory : IPasswordPolicyFactory
    {
        private readonly IConfiguration _configuration;

        public PasswordPolicyFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IPasswordPolicy Create()
        {
            var name = _configuration["Security:PasswordPolicy"];

            return name switch
            {
                "Strong" => new StrongPasswordPolicy(),
                _ => new BasicPasswordPolicy()
            };
        }
    }
}
