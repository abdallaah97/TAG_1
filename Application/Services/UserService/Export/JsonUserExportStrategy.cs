using Application.Services.UserService.DTOs;
using System.Text.Json;

namespace Application.Services.UserService.Export
{
    public class JsonUserExportStrategy : IUserExportStrategy
    {
        public string ContentType => "application/json";

        public string FileName => "users.json";

        public string Export(List<GetUserDto> users)
        {
            return JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
