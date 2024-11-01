using System.Diagnostics;

public class TrayProgram {
    // Переменная для хранения процесса API
    private Process? apiProcess;
    // Логическая переменная для отслеживания статуса API (запущен или остановлен)
    private bool isApiRunning = false;
    // Объект для отображения иконки в системном трее
    private NotifyIcon notifyIcon;
    // Контекстное меню, которое будет отображаться при клике правой кнопкой на иконку
    private ContextMenuStrip contextMenu = new ContextMenuStrip();
    private ToolStripMenuItem toggleApiItem;
    private ToolStripMenuItem openSwaggerItem;
    private ToolStripMenuItem openConfigItem;


    // Элементы интерфейса
    public TrayProgram() {
        toggleApiItem = new ToolStripMenuItem("API", null, ToggleApi);
        openSwaggerItem = new ToolStripMenuItem("Swagger", null, OpenSwagger);
        openConfigItem = new ToolStripMenuItem("Configuration", null, OpenConfiguration);

        notifyIcon = new NotifyIcon {
            Icon = SystemIcons.Application,
            ContextMenuStrip = CreateContextMenu(),
            Visible = true
        };

        // Обработчик двойного щелчка по иконке
        // notifyIcon.DoubleClick += (s, e) => OpenSwagger();
    }

    // Создание контекстного меню
    private ContextMenuStrip CreateContextMenu() {
        // Кнопка выхода из приложения
        ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, ExitApplication);

        // Добавление кнопок в контекстное меню
        contextMenu.Items.AddRange(new ToolStripItem[] {
            toggleApiItem,
            openSwaggerItem,
            openConfigItem,
            new ToolStripSeparator(),
            exitItem
        });
        
        // Возвращаем созданное контекстное меню
        return contextMenu;
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