using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public static class ApiProgram {
    private static readonly string logFilePath = "vpnc.log";

    public static void ConfigureApi(WebApplication app) {
        // Middleware для логирования всех запросов
        app.Use(async (context, next) => {
            // Получение IP-адреса клиента
            string clientIp = context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Unknown";
            // Получение всех заголовков в формате "ключ=значение"
            // var headers = string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"));
            string userAgent = context.Request.Headers["User-Agent"].ToString();
            userAgent = string.IsNullOrEmpty(userAgent) ? "Unknown Client" : userAgent;
            string path = context.Request.Headers["path"].ToString();
            path = string.IsNullOrEmpty(path) ? "Null" : path;
            string name = context.Request.Headers["name"].ToString();
            name = string.IsNullOrEmpty(name) ? "Null" : name;
            // Формирование сообщения лога
            string logMessage = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss}: {context.Request.Method} {context.Request.Path} <= {clientIp} ({userAgent}) [NAME={name}; PATH={path}]";
            // Запись в лог-файл
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            await next();
        });

        app.MapGet("/api/status", async (HttpContext httpContext) => {
            if (MainProgram.config?.InterfaceName == null || MainProgram.config?.ProcessName == null || MainProgram.config?.PingHost == null) {
                return Results.BadRequest("Configuration is missing");
            }
            var status = await MainProgram.StatusConnection(MainProgram.config.InterfaceName, MainProgram.config.ProcessName, MainProgram.config.PingHost);
            return Results.Ok(status);
        });

        app.MapPost("/api/start", (
            [FromHeader(Name = "path")] string? processPath) => {
                processPath ??= MainProgram.config?.ProcessPath;
                if (string.IsNullOrEmpty(processPath)) {
                    return Results.BadRequest("No process path in headers or configuration");
                }
                // Проверяем, что путь существует
                bool checkPath = File.Exists(processPath);
                if (! checkPath) {
                    return Results.BadRequest($"Path not found in the system: {processPath}");
                };
                // Запускаем процесс
                MainProgram.StartProcess(processPath);
                // Извлекаем имя процесса из пути
                var processName = Path.GetFileNameWithoutExtension(processPath);
                // Проверяем, что процесс запущен
                if (!MainProgram.CheckProcess(processName, true)) {
                    return Results.NotFound($"Process {processName} not found");
                }
                return Results.Ok($"Process {processName} started");
            }
        );

        app.MapPost("/api/stop", async (
            // Чтение параметров из заголовков запроса
            [FromHeader(Name = "name")] string? processName,
            [FromHeader(Name = "wildcard")] bool? wildcard) => {
                // Если processName не передан, берем из конфигурации
                processName ??= MainProgram.config?.ProcessName;
                // Если параметр в конфигурации не задан, возвращяем ошибку
                if (string.IsNullOrEmpty(processName)) {
                    return Results.BadRequest("No process name in headers or configuration");
                }
                // Проверяем, что процесс существует
                if (!MainProgram.CheckProcess(processName, wildcard ?? false)) {
                    return Results.NotFound($"Process {processName} not found{(wildcard == true ? " in wildcard mode" : "")}");
                }
                // Остановка процесса
                await MainProgram.StopProcessAsync(processName, wildcard ?? false);
                return Results.Ok($"Process {processName} {(wildcard == true ? "stopped in wildcard mode" : "stopped")}");
            }
        );
    }
}
