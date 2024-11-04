using System.Diagnostics;
using System.Timers;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    private ToolStripMenuItem toggleApiItem;
    private ToolStripMenuItem openSwaggerItem;
    private ToolStripMenuItem openConfigItem;

    // Таймер для периодической проверки соединения
    private System.Timers.Timer pingTimer;
    private bool isPingCheckEnabled = false;

    // Элементы интерфейса
    public TrayProgram() {
        pingCheckItem = new ToolStripMenuItem("Ping Monitoring", null, TogglePingCheck);
        toggleApiItem = new ToolStripMenuItem("API", null, ToggleApi);
        openSwaggerItem = new ToolStripMenuItem("Open Swagger", null, OpenSwagger);
        openConfigItem = new ToolStripMenuItem("Open Configuration", null, OpenConfiguration);

        notifyIcon = new NotifyIcon {
            Icon = SystemIcons.Application,
            ContextMenuStrip = CreateContextMenu(),
            Visible = true
        };

        // Интервал проверки 10 секунд
        pingTimer = new System.Timers.Timer(10000);
        pingTimer.Elapsed += PerformPingCheck;
        pingTimer.AutoReset = true;
    }

    // Создание контекстного меню
    private ContextMenuStrip CreateContextMenu() {
        // Кнопка выхода из приложения
        ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, ExitApplication);

        // Добавление кнопок в контекстное меню
        contextMenu.Items.AddRange(new ToolStripItem[] {
            pingCheckItem,
            toggleApiItem,
            openSwaggerItem,
            openConfigItem,
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
        string? pingHost = MainProgram.config?.PingHost?.ToString();
        if (string.IsNullOrEmpty(pingHost)) return;
    
        bool isConnected = false;
    
        // Первичная проверка на подключение
        for (int i = 0; i < 3; i++) {
            var (internetStatus, responseTimeOut) = MainProgram.StatusPing(pingHost);
            if (internetStatus == "Connected") {
                isConnected = true;
                break;
            }
        }
    
        // Если интернет недоступен
        if (!isConnected) {
            // Проверка, чтобы оповещение о недоступности отправлялось только один раз
            if (!wasInternetUnavailable) {
                notifyIcon.ShowBalloonTip(5000, "Internet connection", "Internet is unavailable", ToolTipIcon.Warning);
                wasInternetUnavailable = true; // Установить флаг недоступности
            }
            pingTimer.Interval = 2000; // Устанавливаем интервал на 2 секунды
            isStableConnectionCheck = false; // Сбрасываем проверку стабильного соединения
        } else {
            // Если интернет доступен и проверка стабильности не выполняется
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
    
                // Если соединение стабильно, отправляем уведомление о доступности интернета
                if (stableConnection) {
                    notifyIcon.ShowBalloonTip(5000, "Internet connection", "Internet is available", ToolTipIcon.Info);
                    wasInternetUnavailable = false; // Сбрасываем переменную
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
            // Создание нового процесса для запуска API
            apiProcess = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "dotnet",
                    Arguments = "run api",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
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

    // Метод для отображения сообщения с URL API
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
        string configFilePath = "vpnc.config.json"; // Путь к конфигурационному файлу
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