using System.Diagnostics;
using System.Timers;

public class TrayProgram {
    // Переменная для хранения процесса API
    private Process? apiProcess;
    // Логическая переменная для отслеживания статуса API (запущен или остановлен)
    private bool isApiRunning = false;
    // Объект для отображения иконки в системном трее
    private NotifyIcon notifyIcon;
    // Контекстное меню, которое будет отображаться при клике правой кнопкой на иконку
    private ContextMenuStrip contextMenu = new ContextMenuStrip();
    private ToolStripMenuItem pingCheckItem;
    private ToolStripMenuItem vpnCheckItem;
    private ToolStripMenuItem toggleApiItem;
    private ToolStripMenuItem openSwaggerItem;
    private ToolStripMenuItem openConfigItem;
    private ToolStripMenuItem openLogItem;
    private ToolStripMenuItem startVPN;
    private ToolStripMenuItem stopVPN;
    private ToolStripMenuItem statusRegion;

    // Таймер для проверки интернет соединения
    private System.Timers.Timer pingTimer;
    private bool isPingCheckEnabled = false;
    // Таймер для проверки VPN соединения
    private System.Timers.Timer vpnTimer;
    private bool isVpnCheckEnabled = false;

    // Переменные для хранения иконок
    private Icon iconPingAvailable;
    private Icon iconPingNotAvailable;
    private Icon iconVPN;

    // Элементы интерфейса
    public TrayProgram() {
        pingCheckItem = new ToolStripMenuItem("Ping Monitoring", null, TogglePingCheck);
        vpnCheckItem = new ToolStripMenuItem("VPN Monitoring", null, ToggleVpnCheck);
        toggleApiItem = new ToolStripMenuItem("API", null, ToggleApi);
        openSwaggerItem = new ToolStripMenuItem("Open Swagger", null, OpenSwagger);
        openConfigItem = new ToolStripMenuItem("Open Configuration", null, OpenConfiguration);
        openLogItem = new ToolStripMenuItem("Open Log", null, OpenLogFile);
        startVPN = new ToolStripMenuItem("Start VPN", null, StartVPN);
        stopVPN = new ToolStripMenuItem("Stop VPN", null, StopVPN);
        statusRegion = new ToolStripMenuItem("Show Region", null, async (sender, e) => await ShowStatusConnectionAsync());

        // Конвертируем png в ico и сохраняем их в переменные
        string pngPingAvailable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\ping-available.png");
        using Bitmap bitmapPingAvailable = new Bitmap(pngPingAvailable);
        iconPingAvailable = Icon.FromHandle(bitmapPingAvailable.GetHicon());

        string pngPingNotAvailable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\ping-not-available.png");
        using Bitmap bitmapPingNotAvailable = new Bitmap(pngPingNotAvailable);
        iconPingNotAvailable = Icon.FromHandle(bitmapPingNotAvailable.GetHicon());

        string pngVPN = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\vpn.png");
        using Bitmap bitmapVPN = new Bitmap(pngVPN);
        iconVPN = Icon.FromHandle(bitmapVPN.GetHicon());

        // Устанавливаем начальную иконку
        notifyIcon = new NotifyIcon {
            Icon = iconPingAvailable,
            ContextMenuStrip = CreateContextMenu(),
            Visible = true
        };

        // Событие статуса при двойном клике на иконку
        notifyIcon.MouseDoubleClick += async (sender, args) => await ShowStatusConnectionAsync();

        // Устанавливаем таймеры для запуска функций при включении чекера (по умолчанию, 5 секунд)
        int pingTimeout = int.TryParse(MainProgram.config?.PingTimeout, out int pingTimeoutResult) ? pingTimeoutResult*1000 : 5000;
        pingTimer = new System.Timers.Timer(pingTimeout);
        pingTimer.Elapsed += PerformPingCheck;
        pingTimer.AutoReset = true;

        int vpnTimeout = int.TryParse(MainProgram.config?.VpnTimeout, out int vpnTimeoutResult) ? vpnTimeoutResult*1000 : 5000;
        vpnTimer = new System.Timers.Timer(vpnTimeout);
        vpnTimer.Elapsed += PerformVpnCheck;
        vpnTimer.AutoReset = true;

        // Автозапуск чекеров при запуске трея в зависимости от настроек в конфигурации
        bool pingStartup = MainProgram.config?.PingStartup ?? false;
        if (pingStartup) {
            pingCheckItem.Checked = true;
            isPingCheckEnabled = true;
            pingTimer.Start();
        };

        bool vpnStartup = MainProgram.config?.VpnStartup ?? false;
        if (vpnStartup) {
            vpnCheckItem.Checked = true;            
            isVpnCheckEnabled = true;
            vpnTimer.Start();
        };

        bool apiStartup = MainProgram.config?.ApiStartup ?? false;
        if (apiStartup) {
            toggleApiItem.Checked = true;
            StartApiProcess();
        };
    }

    // Создание контекстного меню
    private ContextMenuStrip CreateContextMenu() {
        // Кнопка выхода из приложения
        ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, ExitApplication);

        // Добавление кнопок в контекстное меню
        contextMenu.Items.AddRange(new ToolStripItem[] {
            pingCheckItem,
            vpnCheckItem,
            toggleApiItem,
            openSwaggerItem,
            openConfigItem,
            openLogItem,
            startVPN,
            stopVPN,
            statusRegion,
            new ToolStripSeparator(),
            exitItem
        });

        // Возвращаем созданное контекстное меню
        return contextMenu;
    }

    // Метод для остановки и запуска проверки интернета
    private void TogglePingCheck(object? sender, EventArgs? e) {
        isPingCheckEnabled = !isPingCheckEnabled;
        pingCheckItem.Checked = isPingCheckEnabled;
        if (isPingCheckEnabled) {
            pingTimer.Start();
        } else {
            pingTimer.Stop();
        }
    }

    // Переменная для отслеживания состояния интернета и отправки уведомлений
    private bool wasInternetUnavailable = false;
    private bool isStableConnectionCheck = false; // Переменная для отслеживания состояния проверки стабильного соединения
    
    // Метод для проверки Ping
    private void PerformPingCheck(object? sender, ElapsedEventArgs e) {
        string pingHost = MainProgram.config?.PingHost?.ToString() ?? "8.8.8.8";
        if (string.IsNullOrEmpty(pingHost)) return;
        bool isConnected = false;
        // Первичная проверка на подключение
        for (int i = 0; i < 3; i++) {
            var (internetStatus, responseTimeOut) = MainProgram.StatusPing(pingHost);
            // Debug
            // Console.WriteLine($"Ping status: {internetStatus}");
            if (internetStatus == "Connected") {
                isConnected = true;
                break;
            }
        }
        // Если ping недоступен
        if (!isConnected) {
            // Проверка, чтобы оповещение о недоступности отправлялось только один раз
            if (!wasInternetUnavailable) {
                notifyIcon.ShowBalloonTip(5000, "Internet connection", "Internet unavailable", ToolTipIcon.Warning);
                wasInternetUnavailable = true; // Установить флаг недоступности
            }
            pingTimer.Interval = 2000; // Устанавливаем интервал на 2 секунды
            isStableConnectionCheck = false; // Сбрасываем проверку стабильного соединения
            notifyIcon.Icon = iconPingNotAvailable;
        } else {
            // Если ping доступен и проверка стабильности не выполняется
            if (!isStableConnectionCheck) {
                isStableConnectionCheck = true; // Устанавливаем флаг проверки стабильности
                bool stableConnection = true;
                // Выполняем дополнительные проверки стабильности
                for (int i = 0; i < 2; i++) {
                    Thread.Sleep(2000); // Пауза 2 секунды между проверками
                    var (internetStatus, responseTimeOut) = MainProgram.StatusPing(pingHost);
                    if (internetStatus != "Connected") {
                        stableConnection = false;
                        break;
                    }
                }
                // Если соединение стабильно, отправляем уведомление о доступности интернета и изменяем иконку
                if (stableConnection) {
                    notifyIcon.ShowBalloonTip(5000, "Internet connection", "Internet available", ToolTipIcon.Info);
                    wasInternetUnavailable = false; // Сбрасываем переменную
                    int pingTimeout = int.TryParse(MainProgram.config?.PingTimeout, out int pingTimeoutResult) ? pingTimeoutResult*1000 : 5000;
                    pingTimer.Interval = pingTimeout;
                    // Проверяем интерфейс и изменяем иконку
                    string? interfaceName = MainProgram.config?.InterfaceName?.ToString();
                    if (string.IsNullOrEmpty(interfaceName)) return;
                    string interfaceStatus = MainProgram.StatusInterface(interfaceName);
                    if (interfaceStatus == "Up") {
                        notifyIcon.Icon = iconVPN;
                    } else {
                        notifyIcon.Icon = iconPingAvailable;
                    }
                }
            }
        }
    }

    // Метод для остановки и запуска проверки VPN
    private void ToggleVpnCheck(object? sender, EventArgs? e) {
        isVpnCheckEnabled = !isVpnCheckEnabled;
        vpnCheckItem.Checked = isVpnCheckEnabled;
        if (isVpnCheckEnabled) {
            vpnTimer.Start();
        } else {
            vpnTimer.Stop();
        }
    }

    private bool wasVpnUnavailable = false;

    // Метод для проверки работоспособности VPN и автоматического переподключения, если процесс не остановлен вручную
    private void PerformVpnCheck(object? sender, ElapsedEventArgs e) {
        string? interfaceName = MainProgram.config?.InterfaceName?.ToString();
        string? processName = MainProgram.config?.ProcessName?.ToString();
        string? processPath = MainProgram.config?.ProcessPath?.ToString();
        if (string.IsNullOrEmpty(interfaceName) || string.IsNullOrEmpty(processName) || string.IsNullOrEmpty(processPath)) return;
        Process[] processes = Process.GetProcessesByName(processName);
        if (processes.Length == 0) {
            if (notifyIcon.Icon == iconVPN || notifyIcon.Icon != iconPingNotAvailable) {
                notifyIcon.Icon = iconPingAvailable;
            }
            return;
        }
        string interfaceStatus = MainProgram.StatusInterface(interfaceName);
        // Debug
        // Console.WriteLine($"Interface status: {interfaceStatus}");
        if (interfaceStatus != "Up" && processes.Length != 0) {
            if (notifyIcon.Icon != iconPingNotAvailable) {
                notifyIcon.Icon = iconPingAvailable;
            }
            if (!wasVpnUnavailable) {
                notifyIcon.ShowBalloonTip(5000, "VPN connection", "VPN unavailable", ToolTipIcon.Warning);
                wasVpnUnavailable = true;
            }
            Task.Delay(2000);
            processes = Process.GetProcessesByName(processName);
            if (processes.Length != 0) {
                // Задержка для перезапуска процесса (по умолчанию, 2 минуты)
                int VpnRestartTimeout = int.TryParse(MainProgram.config?.VpnRestartTimeout, out int VpnRestartTimeoutResult) ? VpnRestartTimeoutResult*1000 : 120000;
                vpnTimer.Interval = VpnRestartTimeout;
                MainProgram.StopProcess(processName, true);
                MainProgram.StartProcess(processPath);
            }
        } else {
            if (notifyIcon.Icon != iconPingNotAvailable) {
                notifyIcon.Icon = iconVPN;
            }
            if (wasVpnUnavailable) {
                interfaceStatus = MainProgram.StatusInterface(interfaceName);
                if (interfaceStatus == "Up") {
                    notifyIcon.ShowBalloonTip(5000, "VPN connection", "VPN available", ToolTipIcon.Info);
                    wasVpnUnavailable = false;
                    int vpnTimeout = int.TryParse(MainProgram.config?.VpnTimeout, out int vpnTimeoutResult) ? vpnTimeoutResult*1000 : 5000;
                    vpnTimer.Interval = vpnTimeout;
                }
            }
        }
    }

    // Обработчик для переключения состояния API
    private void ToggleApi(object? sender, EventArgs? e) {
        // Проверка запуска API для остановки или запуска
        if (isApiRunning) {
            StopApiProcess();
        } else {
            StartApiProcess();
        }
        // Обновить статус кнопки после выполнения
        UpdateCheckedButton();
    }

    // Метод для запуска процесса API
    private void StartApiProcess() {
        // Проверяем, если процесс API не существует или завершён
        if (apiProcess == null || apiProcess.HasExited) {
            string exec = Path.Combine(Environment.CurrentDirectory, "vpnc.exe");
            string arguments = "api";
            if (!File.Exists(exec)) {
                exec = "dotnet";
                arguments = "run api";
            }
            // Создание нового процесса для запуска API
            apiProcess = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = exec,
                    Arguments = arguments,
                    // RedirectStandardOutput = true,
                    // RedirectStandardError = true,
                    // UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            // Включить отслеживание событий для завершения процесса
            apiProcess.EnableRaisingEvents = true;
            // Обработчик события завершения процесса
            apiProcess.Exited += (s, e) => {
                isApiRunning = false; // Обновить статус API на "остановлен"
                UpdateCheckedButton(); // Обновляем текст кнопки
            };

            // Запуск процесса API
            try {
                apiProcess.Start(); // Запуск процесса
                isApiRunning = true; // Обновляем статус
            } catch (Exception ex) {
                // Обработка ошибок при запуске API
                MessageBox.Show($"Failed to run API: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Метод для остановки процесса API
    private void StopApiProcess() {
        // Проверка на существование процесса и что он не завершен
        if (apiProcess != null && !apiProcess.HasExited) {
            apiProcess.Kill(); // Завершение процесса
            apiProcess.Dispose(); // Освобождение ресурсов процесса
            apiProcess = null; // Обнуляем переменную процесса
            isApiRunning = false; // Обновляем статус
        }
    }

    // Метод для обновления статуса работы API
    private void UpdateCheckedButton() {
        toggleApiItem.Checked = isApiRunning;
    }

    // Метод открытия URL с Swagger API
    private void OpenSwagger(object? sender, EventArgs? e) {
        // Получение порта из конфигурации
        string? port = MainProgram.config?.ApiPort?.ToString();
        if (!string.IsNullOrEmpty(port)) {
            string url = $"http://localhost:{port}/swagger";
            try {
                // Открытие URL в браузере
                Process.Start(new ProcessStartInfo {
                    FileName = url,
                    UseShellExecute = true
                });
            } catch (Exception ex) {
                MessageBox.Show($"Failed to open the browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        } else {
            MessageBox.Show("Port not specified in configuration.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // Обработчик открытия файла конфигурации
    private void OpenConfiguration(object? sender, EventArgs? e) {
        string configFilePath = "vpnc.json";
        // Проверка, что файл существует
        if (File.Exists(configFilePath)) {
            try {
                // Открытие файла в редакторе по умолчанию
                Process.Start(new ProcessStartInfo {
                    FileName = configFilePath,
                    UseShellExecute = true // Используем оболочку для открытия
                });
            } catch (Exception ex) {
                MessageBox.Show($"Failed to open configuration file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        } else {
            MessageBox.Show("Configuration file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OpenLogFile(object? sender, EventArgs? e) {
        string configFilePath = "vpnc.log";
        if (File.Exists(configFilePath)) {
            try {
                // Открытие файла в редакторе по умолчанию
                Process.Start(new ProcessStartInfo {
                    FileName = configFilePath,
                    UseShellExecute = true
                });
            } catch (Exception ex) {
                MessageBox.Show($"Failed to open log file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        } else {
            MessageBox.Show("Log file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // Методы для остановки и запуска процесса VPN
    private void StartVPN(object? sender, EventArgs? e) {
        string? processPath = MainProgram.config?.ProcessPath?.ToString();
        if (!string.IsNullOrEmpty(processPath)) {
            MainProgram.StartProcess(processPath);
        }
    }

    private void StopVPN(object? sender, EventArgs? e) {
        string? processName = MainProgram.config?.ProcessName?.ToString();
        if (!string.IsNullOrEmpty(processName)) {
            MainProgram.StopProcess(processName, true);
        }
    }

    private async Task ShowStatusConnectionAsync() {
        dynamic statusResult = await MainProgram.GetLocationInfo("Connected");
        // Debug
        // Console.WriteLine($"statusResult: {statusResult}");
        var statusText= $"Time Zone: {statusResult.timezone}\n" +
                        $"Region: {statusResult.region}\n" +
                        $"City: {statusResult.city}\n" +
                        $"External IP: {statusResult.ip}";
        notifyIcon.ShowBalloonTip(5000, $"Connection country: {statusResult.country}", statusText, ToolTipIcon.Info);
    }

    // Обработчик выхода из приложения
    private void ExitApplication(object? sender, EventArgs? e) {
        StopApiProcess(); // Остановить API перед выходом
        notifyIcon.Visible = false; // Скрыть иконку в трее
        Application.Exit(); // Завершить приложение
    }

    // Основной метод приложения
    [STAThread]
    public static void Main() {
        Application.EnableVisualStyles(); // Включение визуальных стилей
        Application.SetCompatibleTextRenderingDefault(false); // Установка совместимости с рендерингом текста
        new TrayProgram(); // Создание экземпляра класса TrayProgram
        Application.Run(); // Запуск цикла обработки событий
    }
}