using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.JSInterop.Infrastructure;

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
                    MainProgram.StopProcess(MainProgram.config.ProcessName);
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

            case "restart":
                if (MainProgram.config?.ProcessName == null || MainProgram.config?.ProcessPath == null) {
                    Console.WriteLine("Configuration parameters are empty: ProcessName or ProcessPath");
                    return;
                } else {
                    MainProgram.RestartProcess(MainProgram.config.ProcessName, MainProgram.config.ProcessPath);
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
                var builder = WebApplication.CreateBuilder(args);

                var port = MainProgram.config?.ApiPort;
                builder.WebHost.UseUrls($"http://*:{port}");

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                var app = builder.Build();
                app.UseSwagger();
                app.UseSwaggerUI();

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
                Console.WriteLine("Invalid parameter. Use: vpnc [start|stop|restart|status|api|tray|process]");
                break;
        }
    }
}
