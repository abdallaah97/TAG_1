using Application.Common.Exceptions;

namespace Application.Services.UserService.Export
{
    public class UserExportStrategyFactory : IUserExportStrategyFactory
    {
        public IUserExportStrategy Create(string format)
        {
            return format?.ToLower() switch
            {
                "csv" => new CsvUserExportStrategy(),
                "json" => new JsonUserExportStrategy(),
                _ => throw new BadRequestException($"Unsupported format '{format}'. Use csv or json.")
            };
        }
    }
}
