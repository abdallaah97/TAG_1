using System.Net;

namespace Application.Common.Exceptions
{
    // Base of every error that is safe to return to the caller.
    // Anything else bubbles up as a 500 without leaking internals.
    public class AppException : Exception
    {
        public AppException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }

    public class BadRequestException : AppException
    {
        public BadRequestException(string message) : base(message, HttpStatusCode.BadRequest) { }
    }

    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(message, HttpStatusCode.NotFound) { }

        public NotFoundException(string name, object key)
            : base($"{name} with id '{key}' was not found.", HttpStatusCode.NotFound) { }
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(message, HttpStatusCode.Conflict) { }
    }

    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message) : base(message, HttpStatusCode.Unauthorized) { }
    }

    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message) : base(message, HttpStatusCode.Forbidden) { }
    }
}
