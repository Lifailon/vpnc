using System.Diagnostics;
using System.Net.NetworkInformation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Config {
    public string? ProcessName { get; set; }
    public string? ProcessPath { get; set; }
    public string? InterfaceName { get; set; }
    public string? PingHost { get; set; }
    public string? ApiPort { get; set; }
    public string? PingTimeout { get; set; }
    public string? VpnTimeout { get; set; }
    public bool? PingStartup { get; set; }
    public bool? VpnStartup { get; set; }
    public bool? ApiStartup { get; set; }
}

public static class MainProgram {
    public static Config? config;

    // Функция загрузки конфигурационного файла
    public static void LoadConfig() {
        try {
            string json = File.ReadAllText("vpnc.json");
            config = JsonConvert.DeserializeObject<Config>(json);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error loading config: {ex.Message}");
            return;
        }
    }

    // Функция остановки процесса
    public static void StopProcess(string processName) {
        try {
            Process[] processes = Process.GetProcessesByName(processName);
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

    // Функция запуска процесса
    public static void StartProcess(string processPath) {
        try {
            ProcessStartInfo startInfo = new ProcessStartInfo {
                FileName = processPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(startInfo);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error starting process: {ex.Message}");
        }
    }

    // Функция вызова остановки и последующего запуска процесса
    public static void RestartProcess(string processName, string processPath) {
        StopProcess(processName);
        StartProcess(processPath);
    }

    // Функция проверка статуса процесса
    public static string StatusProcess(string processName) {
        var processes = Process.GetProcessesByName(processName);
        return processes.Length > 0 ? "Running" : "Not running";
    }

    // Функция получения uptime процесса
    public static string UptimeProcess(string processName) {
        var processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0) {
            return "N/A";
        } else {
            var startTime = processes[0].StartTime;
            var uptime = DateTime.Now - startTime;
            return $"{uptime.Days}.{uptime.Hours}:{uptime.Minutes}:{uptime.Seconds}";
        }        
    }

    // Функция получения uptime системы
    public static string UptimeSystem() {
        using var uptime = new PerformanceCounter("System", "System Up Time");
        uptime.NextValue();
        TimeSpan systemUptime = TimeSpan.FromSeconds(uptime.NextValue());
        return $"{systemUptime.Days}.{systemUptime.Hours}:{systemUptime.Minutes}:{systemUptime.Seconds}";
    }

    // Функция получения статуса сетевого адаптера
    public static string StatusInterface(string interfaceName) {
        string interfaceStatus = "Not found";
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces()) {
            if (ni.Name == interfaceName) {
                interfaceStatus = ni.OperationalStatus == OperationalStatus.Up ? "Up" : "Down";
                break;
            }
        }
        return interfaceStatus;
    }

    // Функция ICMP-запроса для проверки интернет-соединения
    public static (string internetStatus, string responseTimeOut) StatusPing(string PingHost) {
        using Ping ping = new Ping();
        string internetStatus = "Disconnected";
        string responseTimeOut = "N/A";

        PingReply reply = ping.Send(PingHost, 2000); // Timeout 2 seconds
        if (reply.Status == IPStatus.Success) {
            internetStatus = "Connected";
            responseTimeOut = $"{reply.RoundtripTime} ms";
        }

        return (internetStatus, responseTimeOut);
    }

    // HTTP-запрос для получения информации о местоположении
    public static async Task<dynamic> GetLocationInfo(string internetStatus) {

        string urlCheckRegion = "https://ipinfo.io/json";
        JObject locationInfo = new JObject();
        if (internetStatus == "Connected") {
            using HttpClient client = new HttpClient {
                Timeout = TimeSpan.FromSeconds(5) // Timeout 5 seconds
            };
            try {
                HttpResponseMessage response = await client.GetAsync(urlCheckRegion);
                response.EnsureSuccessStatusCode();
                string locationJson = await response.Content.ReadAsStringAsync();
                locationInfo = JObject.Parse(locationJson);
            }
            catch (Exception ex) {
                locationInfo["error"] = ex.Message;
            }
        } else {
            locationInfo["status"] = "Not available";
        }
        return locationInfo;
    }

    // Функция сбора всех метрик в формате json
    public static async Task<object> StatusConnection(string interfaceName, string processName, string PingHost) {
        // Собираем всю информацию из функций и возвращаем ответ в формате JSON
        string processStatus = StatusProcess(processName);
        string processUptime = UptimeProcess(processName);
        string systemUptime = UptimeSystem();
        string interfaceStatus = StatusInterface(interfaceName);
        var (internetStatus, responseTimeOut) = StatusPing(PingHost);
        JObject locationInfo = await GetLocationInfo(internetStatus);
        // Формируем ответ в формате JSON
        return new {
            ProcessName = processName,
            ProcessStatus = processStatus,
            ProcessUptime = processUptime,
            SystemUptime = systemUptime,
            InterfaceName = interfaceName,
            InterfaceStatus = interfaceStatus,
            PingAddress = PingHost,
            PingStatus = internetStatus,
            PingTimeout = responseTimeOut,
            Country = (string?)locationInfo?["country"] ?? "N/A",
            TimeZone = (string?)locationInfo?["timezone"] ?? "N/A",
            Region = (string?)locationInfo?["region"] ?? "N/A",
            City = (string?)locationInfo?["city"] ?? "N/A",
            ExternalIp = (string?)locationInfo?["ip"] ?? "N/A"
        };
    }
}