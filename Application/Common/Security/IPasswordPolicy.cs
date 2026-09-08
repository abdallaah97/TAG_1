namespace Application.Common.Security
{
    public interface IPasswordPolicy
    {
        string Description { get; }

        bool IsValid(string password);
    }
}
