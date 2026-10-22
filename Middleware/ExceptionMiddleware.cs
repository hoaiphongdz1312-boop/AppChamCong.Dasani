using Microsoft.AspNetCore.Mvc;

namespace NguyenHoaiPhong_1681.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            int status;
            string message;
            if (ex is AppException app)
            {
                status = app.StatusCode;
                message = app.Message;
            }
            else
            {
                status = StatusCodes.Status500InternalServerError;
                message = "Đã xảy ra lỗi hệ thống, vui lòng thử lại sau.";
                _logger.LogError(ex, "Unhandled exception");
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = message,
                Detail = _env.IsDevelopment() && ex is not AppException ? ex.ToString() : null,
                Instance = context.Request.Path
            };
            problem.Extensions["message"] = message;
            if (ex is AppException { Extra: { } extra })
                foreach (var (key, value) in extra) problem.Extensions[key] = value;

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
