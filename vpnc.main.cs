using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Config {
    public string? ProcessName { get; set; }
    public string? ExecPath { get; set; }
    public string? InterfaceName { get; set; }
    public string? CheckInternetHost { get; set; }
    public string? ApiPort { get; set; }
}

public static class MainProgram {
    public static Config? config;

    // Функция для загрузки конфигурационного файла
    public static void LoadConfig() {
        try {
            string json = File.ReadAllText("vpnc.config.json");
            config = JsonConvert.DeserializeObject<Config>(json);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error loading config: {ex.Message}");
            return;
        }
    }

    // Функция для остановки процесса
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

    // Функция для запуска процесса
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

    // Функция для вызова остановки и последующего запуска процесса
    public static void RestartProcess(string processName, string processPath) {
        StopProcess(processName);
        StartProcess(processPath);
    }

    // Функция для получения статуса
    public static async Task<object> StatusConnection(string interfaceName, string processName, string checkInternetHost) {
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
        string internetStatus = "Disconnected";
        string responseTimeOut = "N/A";
        PingReply reply = ping.Send(checkInternetHost, 2000); // Timeout 2 seconds
        if (reply.Status == IPStatus.Success) {
            internetStatus = "Connected";
            responseTimeOut = reply.RoundtripTime + " ms";
        }

        // HTTP-запрос для получения информации о местоположении
        string urlCheckRegion = "https://ipinfo.io/json";
        JObject? locationInfo = null;
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
                locationInfo = new JObject { ["error"] = ex.Message };
            }
        } else {
            locationInfo = new JObject { ["status"] = "Not available" };
        }

        // Ответ в формате JSON
        return new {
            ProcessName = processName,
            ProcessStatus = processStatus,
            InterfaceName = interfaceName,
            InterfaceStatus = interfaceStatus,
            InternetHost = checkInternetHost,
            InternetStatus = internetStatus,
            InternetTimeout = responseTimeOut,
            Country = (string?)locationInfo?["country"] ?? "N/A",
            TimeZone = (string?)locationInfo?["timezone"] ?? "N/A",
            Region = (string?)locationInfo?["region"] ?? "N/A",
            City = (string?)locationInfo?["city"] ?? "N/A",
            Location = (string?)locationInfo?["loc"] ?? "N/A",
            ExternalIp = (string?)locationInfo?["ip"] ?? "N/A"
        };
    }
}