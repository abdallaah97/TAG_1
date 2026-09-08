namespace Application.Common.Security
{
    public class BasicPasswordPolicy : IPasswordPolicy
    {
        public string Description => "At least 6 characters.";

        public bool IsValid(string password)
        {
            return password.Length >= 6;
        }
    }
}
