namespace Application.Services.UserService.Export
{
    // FACTORY: turns the format the caller asked for into a strategy.
    public interface IUserExportStrategyFactory
    {
        IUserExportStrategy Create(string format);
    }
}
