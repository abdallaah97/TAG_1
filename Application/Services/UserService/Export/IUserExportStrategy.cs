using Application.Services.UserService.DTOs;

namespace Application.Services.UserService.Export
{
    // STRATEGY: one way of writing users into a file. The service that fetched the users
    // never finds out which format came out.
    public interface IUserExportStrategy
    {
        string ContentType { get; }

        string FileName { get; }

        string Export(List<GetUserDto> users);
    }

    public record ExportFile(byte[] Content, string ContentType, string FileName);
}
