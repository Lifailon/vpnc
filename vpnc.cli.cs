using Newtonsoft.Json;

class CliProgram {
    static async Task Main(string[] args) {
        MainProgram.LoadConfig();
        
        if (args.Length == 0) {
            Console.WriteLine("vpnc [start|stop|restart|status|api]");
            return;
        }

        string command = args[0].ToLower();
        switch (command) {
            case "stop":
                if (MainProgram.config?.ProcessName == null) {
                    return;
                } else {
                    MainProgram.StopProcess(MainProgram.config.ProcessName);
                }
                break;

            case "start":
                if (MainProgram.config?.ExecPath == null) {
                    return;
                } else {
                    MainProgram.StartProcess(MainProgram.config.ExecPath);
                }
                break;

            case "restart":
                if (MainProgram.config?.ProcessName == null || MainProgram.config?.ExecPath == null) {
                    return;
                } else {
                    MainProgram.RestartProcess(MainProgram.config.ProcessName, MainProgram.config.ExecPath);
                }
                break;

            case "status":
                if (MainProgram.config?.InterfaceName == null || MainProgram.config?.ProcessName == null || MainProgram.config?.CheckInternetHost == null) {
                    return;
                } else {
                    var status = await MainProgram.StatusConnection(MainProgram.config.InterfaceName, MainProgram.config.ProcessName, MainProgram.config.CheckInternetHost);
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
                
            default:
                Console.WriteLine("Invalid parameter. Use: vpnc [start|stop|restart|status|api]");
                break;
        }
    }
}
