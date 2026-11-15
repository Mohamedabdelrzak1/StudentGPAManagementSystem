using System.Collections.Generic;
using System.Net;

namespace Shared.Common;

public class Response<T>
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public bool Succeeded { get; set; } = true;
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public T? Payload { get; set; }

    public Response() { }

    public Response(T? payload, string? message = null, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Payload = payload;
        Message = message;
        StatusCode = statusCode;
        Succeeded = ((int)statusCode >= 200 && (int)statusCode < 300);
    }

    public static Response<T> Success(T? payload, string? message = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(payload, message, statusCode);

    public static Response<T> Failure(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, List<string>? errors = null)
        => new()
        {
            Succeeded = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors ?? new List<string>()
        };

    public static Response<T> Fail(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, List<string>? errors = null)
        => Failure(message, statusCode, errors);
}
