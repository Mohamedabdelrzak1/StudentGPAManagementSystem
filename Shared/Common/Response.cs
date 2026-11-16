using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using Shared.Dto;

namespace Shared.Common;

public class ValidationErrorDetail
{
    public string Field { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public ValidationErrorDetail() { }

    public ValidationErrorDetail(string field, IEnumerable<string> errors)
    {
        Field = field;
        Errors = errors.ToList();
    }

    public ValidationErrorDetail(string field, string error)
    {
        Field = field;
        Errors = new List<string> { error };
    }
}

public class Response<T>
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public bool Succeeded { get; set; } = true;
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public List<ValidationErrorDetail>? ValidationErrors { get; set; }
    public T? Payload { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TotalSemesters { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SemesterDto? CurrentSemester { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CompletedSemestersCount { get; set; }

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

    public static Response<T> Failure(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, IEnumerable<string>? errors = null)
        => new()
        {
            Succeeded = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors?.ToList() ?? new List<string> { message },
            Payload = default
        };

    public static Response<T> Fail(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, IEnumerable<string>? errors = null)
        => Failure(message, statusCode, errors);

    public static Response<T> ValidationFailure(string message, List<ValidationErrorDetail> validationErrors)
        => new()
        {
            Succeeded = false,
            StatusCode = HttpStatusCode.BadRequest,
            Message = message,
            ValidationErrors = validationErrors,
            Errors = validationErrors.SelectMany(v => v.Errors).ToList(),
            Payload = default
        };
}

public class ApiResponse<T> : Response<T>
{
    public ApiResponse() { }
    public ApiResponse(T? payload, string? message = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        : base(payload, message, statusCode) { }

    public static new ApiResponse<T> Success(T? payload, string? message = null, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(payload, message, statusCode);

    public static new ApiResponse<T> Failure(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, IEnumerable<string>? errors = null)
        => new()
        {
            Succeeded = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors?.ToList() ?? new List<string> { message },
            Payload = default
        };
}
