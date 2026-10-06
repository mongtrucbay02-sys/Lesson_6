using System.Diagnostics;

namespace Lesson6.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    // Các action nhận id trên URL: /SinhVien/Details/{id}, /SinhVien/Edit/{id}, /SinhVien/Delete/{id}
    private static readonly string[] IdActions = { "details", "edit", "delete" };

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var method = context.Request.Method;
        var path = context.Request.Path.ToString();
        var stopwatch = Stopwatch.StartNew();

        // Chức năng 1: ghi log request
        Console.WriteLine($"[{time}] Method: {method} - Path: {path}");

        // Chức năng 3: chặn id không hợp lệ (0, -1, ...)
        if (IsInvalidStudentId(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Student id không hợp lệ");

            stopwatch.Stop();
            Console.WriteLine($"Status Code: {context.Response.StatusCode} - {stopwatch.ElapsedMilliseconds} ms");
            return; // không gọi _next => request không vào Controller
        }

        // Chuyển request sang middleware kế tiếp (cuối cùng là Controller)
        await _next(context);

        // Chức năng 2: ghi status code sau khi xử lý xong
        stopwatch.Stop();
        Console.WriteLine($"Status Code: {context.Response.StatusCode} - {stopwatch.ElapsedMilliseconds} ms");
    }

    private static bool IsInvalidStudentId(PathString path)
    {
        var segments = path.Value?.Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments is not { Length: 3 }) return false;
        if (!segments[0].Equals("SinhVien", StringComparison.OrdinalIgnoreCase)) return false;
        if (!IdActions.Contains(segments[1], StringComparer.OrdinalIgnoreCase)) return false;

        return int.TryParse(segments[2], out var id) && id <= 0;
    }
}