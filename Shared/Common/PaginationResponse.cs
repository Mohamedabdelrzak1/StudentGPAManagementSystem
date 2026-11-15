using System;
using System.Collections.Generic;
using System.Net;

namespace Shared.Common;

public class PaginationResponse<T>
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
    public bool Succeeded { get; set; } = true;
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public IReadOnlyList<T> Payload { get; set; } = new List<T>();
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public PaginationResponse() { }

    public PaginationResponse(IReadOnlyList<T> payload, int totalCount, int pageIndex, int pageSize, string? message = null)
    {
        Payload = payload;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
        Message = message;
        StatusCode = HttpStatusCode.OK;
        Succeeded = true;
    }

    public static PaginationResponse<T> Success(IReadOnlyList<T> payload, int totalCount, int pageIndex, int pageSize, string? message = null)
        => new(payload, totalCount, pageIndex, pageSize, message);
}
