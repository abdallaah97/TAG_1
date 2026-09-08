namespace Application.Common.Security
{
    public interface IPasswordPolicyFactory
    {
        IPasswordPolicy Create();
    }
}
