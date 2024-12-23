using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;

class CliProgram {
    static async Task Main(string[] args) {
        MainProgram.LoadConfig();
        
        if (args.Length == 0) {
            Console.WriteLine("vpnc [start|stop|restart|status|api|tray|process]");
            return;
        }

        string command = args[0].ToLower();
        switch (command) {
            case "stop":
                if (MainProgram.config?.ProcessName == null) {
                    Console.WriteLine("Configuration parameters are empty: ProcessName");
                    return;
                } else {
                    MainProgram.StopProcess(MainProgram.config.ProcessName, true);
                }
                break;

            case "start":
                if (MainProgram.config?.ProcessPath == null) {
                    Console.WriteLine("Configuration parameters are empty: ProcessPath");
                    return;
                } else {
                    MainProgram.StartProcess(MainProgram.config.ProcessPath);
                }
                break;

            case "status":
                if (MainProgram.config?.InterfaceName == null || MainProgram.config?.ProcessName == null || MainProgram.config?.PingHost == null) {
                    Console.WriteLine("Configuration parameters are empty: InterfaceName or ProcessName or PingHost");
                    return;
                } else {
                    var status = await MainProgram.StatusConnection(MainProgram.config.InterfaceName, MainProgram.config.ProcessName, MainProgram.config.PingHost);
                    Console.WriteLine(JsonConvert.SerializeObject(status));
                }
                break;

            case "api":
                // Конфигурация API
                var builder = WebApplication.CreateBuilder(args);
                var port = MainProgram.config?.ApiPort;
                builder.WebHost.UseUrls($"http://*:{port}");

                // Добавление авторизации в интерфейс Swagger
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c => {
                    // Определяем схему безопасности для использования API ключа
                    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme {
                        Description = "API Key needed to access the endpoints. X-API-KEY: {apiKey}",
                        Name = "X-API-KEY",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "ApiKeyScheme"
                    });
                    // Добавляем требование безопасности для всех эндпоинтов API
                    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                        {
                            new OpenApiSecurityScheme {
                                Reference = new OpenApiReference {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "ApiKey"
                                },
                                In = ParameterLocation.Header
                            },
                            new List<string>()
                        }
                    });
                });
                var app = builder.Build();

                // Исключаем проверку API-ключа для Swagger UI
                app.Use(async (context, next) => {
                    var path = context.Request.Path.Value?.ToLower() ?? string.Empty; // Защищаем от null
                    // Пропускаем проверку API-ключа для маршрутов Swagger
                    if (path.Contains("/swagger")) {
                        await next();
                        return;
                    }
                    // Извлекаем API-ключ из заголовка
                    var apiKeyHeader = context.Request.Headers["X-API-KEY"].ToString();
                    // Проверяем валидность API-ключа
                    if (string.IsNullOrEmpty(apiKeyHeader) || apiKeyHeader != MainProgram.config?.ApiKey) {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("Unauthorized: Invalid API Key.");
                        return;
                    }
                    // Если ключ валидный, пропускаем запросы
                    await next();
                });

                // Включаем Swagger и Swagger UI
                app.UseSwagger();
                app.UseSwaggerUI();

                // Добавляем конфигурацию маршрутов API (endpoints)
                ApiProgram.ConfigureApi(app);

                await app.RunAsync();
                return;

            case "tray":
                TrayProgram.Main();
                return;

            case "process":
                string exec = Path.Combine(Environment.CurrentDirectory, "vpnc.exe");
                string arguments = "tray";
                if (!File.Exists(exec)) {
                    exec = "dotnet";
                    arguments = "run tray";
                }
                var trayProcess = new Process {
                    StartInfo = new ProcessStartInfo {
                        FileName = exec,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                trayProcess.Start();
                return;
                
            default:
                Console.WriteLine("Invalid parameter. Use: vpnc [start|stop|status|api|tray|process]");
                break;
        }
    }
}
