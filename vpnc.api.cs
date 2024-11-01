using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public static class ApiProgram {
    public static void ConfigureApi(WebApplication app) {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapGet("/api/status", async (HttpContext httpContext) => {
            if (MainProgram.config?.InterfaceName == null || MainProgram.config?.ProcessName == null || MainProgram.config?.PingHost == null) {
                return Results.BadRequest("Configuration is missing.");
            }
            var status = await MainProgram.StatusConnection(MainProgram.config.InterfaceName, MainProgram.config.ProcessName, MainProgram.config.PingHost);
            return Results.Ok(status);
        });

        app.MapPost("/api/start", (HttpContext httpContext) => {
            if (MainProgram.config?.ExecPath == null) {
                return Results.BadRequest("No executable path found in configuration.");
            }
            MainProgram.StartProcess(MainProgram.config.ExecPath);
            return Results.Ok("Process started");
        });

        app.MapPost("/api/restart", (HttpContext httpContext) => {
            if (MainProgram.config?.ProcessName == null || MainProgram.config?.ExecPath == null) {
                return Results.BadRequest("No process name found in configuration.");
            }
            MainProgram.RestartProcess(MainProgram.config.ProcessName, MainProgram.config.ExecPath);
            return Results.Ok("Process restarted");
        });

        app.MapPost("/api/stop", (HttpContext httpContext) => {
            if (MainProgram.config?.ProcessName == null) {
                return Results.BadRequest("No process name found in configuration.");
            }
            MainProgram.StopProcess(MainProgram.config.ProcessName);
            return Results.Ok("Process stopped");
        });
    }
}
