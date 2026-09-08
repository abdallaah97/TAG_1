namespace Application.Common.Security
{
    public class StrongPasswordPolicy : IPasswordPolicy
    {
        public string Description => "At least 8 characters, one upper case letter, one digit and one symbol.";

        public bool IsValid(string password)
        {
            return password.Length >= 8
                && password.Any(char.IsUpper)
                && password.Any(char.IsDigit)
                && password.Any(c => !char.IsLetterOrDigit(c));
        }
    }
}
