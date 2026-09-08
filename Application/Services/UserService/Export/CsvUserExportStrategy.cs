using Application.Services.UserService.DTOs;
using System.Text;

namespace Application.Services.UserService.Export
{
    public class CsvUserExportStrategy : IUserExportStrategy
    {
        public string ContentType => "text/csv";

        public string FileName => "users.csv";

        public string Export(List<GetUserDto> users)
        {
            var builder = new StringBuilder();

            // Excel needs this marker at the start of the file to read it as UTF-8,
            // otherwise Arabic names come out as garbage.
            builder.Append('\uFEFF');

            builder.AppendLine("Id,Name,Email,Phone,IsActive,Roles");

            foreach (var user in users)
            {
                var roles = string.Join(" / ", user.Roles.Select(r => r.Name));

                builder.AppendLine($"{user.Id},{Quote(user.Name)},{user.Email},{user.PhoneNumber},{user.IsActive},{Quote(roles)}");
            }

            return builder.ToString();
        }

        // A value that may contain a comma has to be wrapped in quotes.
        private static string Quote(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
