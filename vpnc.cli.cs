using System.Diagnostics;
using System.Net.NetworkInformation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Config {
    public string? ProcessName { get; set; }
    public string? ExecPath { get; set; }
    public string? InterfaceName { get; set; }
    public string? CheckInternetHost { get; set; }
}

class Program {
    static Config? config;

    static async Task Main(string[] args) {
        LoadConfig();
        if (args.Length == 0) {
            Console.WriteLine("vpnc [start|stop|status]");
            return;
        }
        string command = args[0].ToLower();
        switch (command) {
            case "stop":
                if (config?.ProcessName == null) {
                    return;
                } else {
                    StopProcess(config.ProcessName);
                }
                break;
            case "start":
                if (config?.ExecPath == null) {
                    return;
                } else {
                    StartProcess(config.ExecPath);
                }
                break;
            case "status":
                if (config?.InterfaceName == null || config?.ProcessName == null || config?.CheckInternetHost == null) {
                    return;
                } else {
                    await StatusConnection(config.InterfaceName, config.ProcessName, config.CheckInternetHost);
                }
                break;
            default:
                Console.WriteLine("Invalid parameter");
                break;
        }
    }

    static void LoadConfig() {
        try {
            string json = File.ReadAllText("vpnc.config.json");
            config = JsonConvert.DeserializeObject<Config>(json);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error loading config: {ex.Message}");
            return;
        }
    }

    static void StopProcess(string processName) {
        try {
            // Формируем массив из найденных процессов по имени
            Process[] processes = Process.GetProcessesByName(processName);
            // Выходим, если процесс не найден
            if (processes.Length == 0) {
                Console.WriteLine("Process not running");
                return;
            }
            foreach (Process process in processes) {
                process.Kill();
                process.WaitForExit();
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Error stopping process: {ex.Message}");
        }
    }

    static void StartProcess(string processPath) {
        try {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = processPath,
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error starting process: {ex.Message}");
        }
    }

    static async Task StatusConnection(string interfaceName, string processName, string checkInternetHost) {
        try {
            // Проверка статуса процесса
            string processStatus;
            var processes = Process.GetProcessesByName(processName);
            processStatus = processes.Length > 0 ? "Running" : "Not running";

            // Проверка статуса сетевого адаптера
            string interfaceStatus = "Not found";
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces()) {
                if (ni.Name == interfaceName) {
                    interfaceStatus = ni.OperationalStatus == OperationalStatus.Up ? "Up" : "Down";
                    break;
                }
            }

            // ICMP-запрос для проверки интернет-соединения
            using Ping ping = new Ping();
            string internetStatus;
            try {
                PingReply reply = ping.Send(checkInternetHost, 2000); // Timeout 2 секунды
                internetStatus = (reply.Status == IPStatus.Success) ? "Connected" : "Disconnected";
            }
            catch {
                internetStatus = "Disconnected";
            }

            // HTTP-запрос для получения информации о местоположении
            string urlCheckRegion = "https://ipinfo.io/json";
            using HttpClient client = new HttpClient();
            JObject? locationInfo = null;
            if (internetStatus == "Connected") {
                try {
                    HttpResponseMessage response = await client.GetAsync(urlCheckRegion);
                    response.EnsureSuccessStatusCode();
                    string locationJson = await response.Content.ReadAsStringAsync();
                    // Десериализация в JObject
                    locationInfo = JObject.Parse(locationJson);
                }
                catch (Exception ex) {
                    locationInfo = new JObject { ["error"] = ex.Message };
                }
            } else {
                locationInfo = new JObject { ["status"] = "Not available" };
            }

            // Вывод в формате JSON
            var status = new {
                ProcessName = processName,
                ProcessStatus = processStatus,
                InterfaceName = interfaceName,
                InterfaceStatus = interfaceStatus,
                InternetStatus = internetStatus,
                Country = (string?)locationInfo?["country"] ?? "N/A",
                Region = (string?)locationInfo?["region"] ?? "N/A",
                City = (string?)locationInfo?["city"] ?? "N/A",
                TimeZone = (string?)locationInfo?["timezone"] ?? "N/A",
                Location = (string?)locationInfo?["loc"] ?? "N/A",
                ExternalIp = (string?)locationInfo?["ip"] ?? "N/A"
            };
            Console.WriteLine(JsonConvert.SerializeObject(status));
        }
        catch (Exception ex) {
            Console.WriteLine($"Error getting status: {ex.Message}");
        }
    }
}