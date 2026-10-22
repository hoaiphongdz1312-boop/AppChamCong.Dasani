namespace NguyenHoaiPhong_1681.Middleware;

/// <summary>Lỗi nghiệp vụ, trả về client với status code và message tiếng Việt.</summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    /// <summary>Các field bổ sung đưa vào ProblemDetails (ví dụ retry).</summary>
    public Dictionary<string, object?>? Extra { get; init; }

    public AppException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}
