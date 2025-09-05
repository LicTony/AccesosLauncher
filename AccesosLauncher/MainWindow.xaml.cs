using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualBasic;
using Path = System.IO.Path;
using Timer = System.Timers.Timer;
using Application = System.Windows.Application;


namespace AccesosLauncher
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static string BaseDir { get; set; } = App.Configuration["BaseDir"] ?? string.Empty;

        // Extensiones ejecutables: PATHEXT + .lnk + .url
        private static readonly HashSet<string> ExecExtensions = InitExecExtensions();

        public ObservableCollection<AppItem> Items { get; } = [];
        public ObservableCollection<LoggedAppItem> MostUsedItems { get; } = [];

        public ObservableCollection<SettingEntry> AppSettings { get; } = [];
        private bool _settingsLoaded;

        private CollectionViewSource? _groupedSource;
        private FileSystemWatcher? _watcher;
        private Timer? _debounceTimer;

        private NotifyIcon? _notifyIcon;
        private bool _reallyExit;

        private HwndSource? _source;
        private IntPtr _hwnd;

        private const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;

        private string _searchText = string.Empty;
        private readonly DatabaseHelper _databaseHelper;
        private TipoCarpeta _selectedTipoCarpeta;

        public TipoCarpeta SelectedTipoCarpeta
        {
            get => _selectedTipoCarpeta;
            set
            {
                if (_selectedTipoCarpeta != value)
                {
                    _selectedTipoCarpeta = value;
                    OnPropertyChanged(nameof(SelectedTipoCarpeta));
                    _groupedSource?.View?.Refresh();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    _groupedSource?.View?.Refresh();
                    OnPropertyChanged(nameof(ResultCountText));
                }
            }
        }

        public string ResultCountText
        {
            get
            {
                int count = string.IsNullOrWhiteSpace(_searchText)
                    ? Items.Count
                    : Items.Count(MatchesSearch);
                return $"Mostrando {count} elemento(s)";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            this.KeyDown += MainWindow_KeyDown;
            var connectionString = App.Configuration.GetConnectionString("Sqlite") ?? "";
            _databaseHelper = new DatabaseHelper(connectionString);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _hwnd = helper.Handle;
            _source = HwndSource.FromHwnd(_hwnd);
            _source.AddHook(WndProc);

            // Ctrl + Alt + T
            var vkT = (uint)KeyInterop.VirtualKeyFromKey(Key.T);
            if (!RegisterHotKey(_hwnd, HOTKEY_ID, MOD_CONTROL | MOD_ALT, vkT))
            {
                System.Windows.MessageBox.Show("No se pudo registrar la tecla rápida Ctrl+Alt+T (¿ya la usa otra app?).",
                    "Atajo global", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                ShowWindowFromTray();
                handled = true;
            }
            return IntPtr.Zero;
        }

        [SupportedOSPlatform("windows6.1")]
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.Configuration.GetSection("OpacityPercentage").Exists() && int.TryParse(App.Configuration["OpacityPercentage"], out int opacityPercentage))
            {
                if (opacityPercentage >= 0 && opacityPercentage <= 100)
                {
                    var opacity = opacityPercentage / 100.0;
                    var alpha = (byte)(opacity * 255);
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(alpha, 0, 0, 0));
                }
            }

            if (App.Configuration.GetSection("BorderThickness").Exists() && int.TryParse(App.Configuration["BorderThickness"], out int borderThickness))
            {
                if (borderThickness >= 0)
                {
                    MainBorder.BorderThickness = new Thickness(borderThickness);
                }
            }

            _groupedSource = (CollectionViewSource)FindResource("GroupedItems");
            _groupedSource.Filter += GroupedSource_Filter;

            EnsureBaseDir();
            _databaseHelper.InitializeDatabase();
            LoadUserSettings();
            await LoadItems(); // async
            SetupWatcher();
            SetupTrayIcon();

            // Arranca en segundo plano
            HideToTray();
        }

        private void GroupedSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not AppItem item) { e.Accepted = false; return; }

            var accepted = MatchesSearch(item);
            if (accepted)
            {
                accepted = MatchesTipoCarpeta(item);
            }

            e.Accepted = accepted;
        }

        private bool MatchesTipoCarpeta(AppItem item)
        {
            var directoryPath = Path.GetDirectoryName(item.FullPath);
            if (directoryPath == null) return true;

            var hasPersonalFile = File.Exists(Path.Combine(directoryPath, ".personal"));
            var hasMixtaFile = File.Exists(Path.Combine(directoryPath, ".mixta"));

            return SelectedTipoCarpeta switch
            {
                TipoCarpeta.Ambos => true,
                TipoCarpeta.Personal => hasPersonalFile || hasMixtaFile,
                TipoCarpeta.Laboral => !hasPersonalFile && !hasMixtaFile,
                _ => true
            };
        }

        private bool MatchesSearch(AppItem item)
        {
            var q = _searchText?.Trim();
            if (string.IsNullOrEmpty(q)) return true;
            var c = StringComparison.OrdinalIgnoreCase;
            return item.Name.Contains(q, c) ||
                   item.FullPath.Contains(q, c) ||
                   item.RelativeDirectory.Contains(q, c);
        }

        private static void EnsureBaseDir()
        {
            if (string.IsNullOrEmpty(BaseDir))
            {
                System.Windows.MessageBox.Show("La variable BaseDir no está configurada en appsettings.json.",
                    "Error de Configuración", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }
            try
            {
                if (!Directory.Exists(BaseDir))
                    Directory.CreateDirectory(BaseDir);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"No se pudo crear/abrir el directorio base:\n{BaseDir}\n\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [SupportedOSPlatform("windows6.1")]
        private async Task LoadItems()
        {
            try
            {
                var existed = Directory.Exists(BaseDir);
                var files = existed
                    ? Directory.EnumerateFiles(BaseDir, "*.*", SearchOption.AllDirectories)
                        .Where(f => IsLaunchable(f))
                    : [];

                var list = await Task.Run(() =>
                {
                    var tmp = new List<AppItem>();
                    foreach (var f in files)
                    {
                        var dir = Path.GetDirectoryName(f) ?? BaseDir;
                        var rel = Path.GetRelativePath(BaseDir, dir);
                        if (string.IsNullOrWhiteSpace(rel) || rel == "." || rel == @"\")
                            rel = "Raiz";

                        var name = Path.GetFileNameWithoutExtension(f);
                        var icon = IconHelper.GetIconImageSource(f);

                        tmp.Add(new AppItem
                        {
                            Name = name,
                            FullPath = f,
                            RelativeDirectory = rel,
                            Icon = icon
                        });
                    }
                    return tmp;
                });

                Items.Clear();
                foreach (var it in list) Items.Add(it);

                _groupedSource?.View?.Refresh();
                OnPropertyChanged(nameof(ResultCountText));

                ResizeItemsInGroups();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al leer accesos:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool IsLaunchable(string path)
        {
            var ext = Path.GetExtension(path);
            return !string.IsNullOrEmpty(ext) && ExecExtensions.Contains(ext);
        }

        private static HashSet<string> InitExecExtensions()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".lnk",
                ".url" // atajos de Internet: se abren con el navegador predeterminado
            };
            var pathext = Environment.GetEnvironmentVariable("PATHEXT") ?? "";
            foreach (var raw in pathext.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var e = raw.StartsWith('.') ? raw : '.' + raw;
                set.Add(e);
            }
            return set;
        }

        [SupportedOSPlatform("windows6.1")]
        private void SetupWatcher()
        {
            try
            {
                _watcher = new FileSystemWatcher(BaseDir)
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                    Filter = "*.*"
                };
                _watcher.Created += OnFsChanged;
                _watcher.Deleted += OnFsChanged;
                _watcher.Renamed += OnFsChanged;
                _watcher.Changed += OnFsChanged;

                _debounceTimer = new Timer(400) { AutoReset = false };
                _debounceTimer.Elapsed += async (_, __) => await Dispatcher.InvokeAsync(LoadItems);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"No se pudo vigilar la carpeta:\n{ex.Message}",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnFsChanged(object sender, FileSystemEventArgs e)
        {
            var ext = Path.GetExtension(e.FullPath);
            if (!string.IsNullOrEmpty(ext) && (ExecExtensions.Contains(ext) || e.ChangeType == WatcherChangeTypes.Renamed))
            {
                _debounceTimer?.Stop();
                _debounceTimer?.Start();
            }
        }

        [SupportedOSPlatform("windows6.1")]
        private void SetupTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon("AccesosLauncher.ico"),
                Visible = true,
                Text = "Accesos Launcher (Ctrl+Alt+T)"
            };

            var menu = new ContextMenuStrip();
            var openItem = new ToolStripMenuItem("Abrir (Ctrl+Alt+T)");
            openItem.Click += (_, __) => ShowWindowFromTray();
            var exitItem = new ToolStripMenuItem("Salir");
            exitItem.Click += (_, __) =>
            {
                _reallyExit = true;
                _notifyIcon!.Visible = false;
                Close();
            };
            menu.Items.Add(openItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);
            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (_, __) => ShowWindowFromTray();
        }

        private void ShowWindowFromTray()
        {
            Show();
            ShowInTaskbar = true;
            WindowState = WindowState.Maximized;
            Activate();

            // Llevar al frente
            Topmost = true;
            Topmost = false;

            SearchBox.Focus();
            SearchBox.SelectAll();
        }

        private void HideToTray()
        {
            Hide();
            ShowInTaskbar = false;
        }

        [SupportedOSPlatform("windows6.1")]
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_reallyExit)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }

            SaveUserSettings();
            _notifyIcon?.Dispose();
            if (_source != null)
            {
                UnregisterHotKey(_hwnd, HOTKEY_ID);
                _source.RemoveHook(WndProc);
            }
            _watcher?.Dispose();
            _debounceTimer?.Dispose();
        }

        private void LoadUserSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppContext.BaseDirectory, "usersettings.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<UserSettings>(json);
                    SelectedTipoCarpeta = settings?.SelectedTipoCarpeta ?? TipoCarpeta.Ambos;
                }
                else
                {
                    SelectedTipoCarpeta = TipoCarpeta.Ambos;
                }
            }
            catch (Exception)
            {
                SelectedTipoCarpeta = TipoCarpeta.Ambos;
            }
        }

        private void SaveUserSettings()
        {
            try
            {
                var settingsPath = Path.Combine(AppContext.BaseDirectory, "usersettings.json");
                var settings = new UserSettings { SelectedTipoCarpeta = this.SelectedTipoCarpeta };
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                HideToTray();
            }
            if (e.Key == Key.Tab)
            {
                e.Handled = true;
                MainTabControl.SelectedIndex = (MainTabControl.SelectedIndex + 1) % MainTabControl.Items.Count;
            }
            if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                var values = (TipoCarpeta[])Enum.GetValues(typeof(TipoCarpeta));
                var currentIndex = (int)SelectedTipoCarpeta;
                var nextIndex = (currentIndex + 1) % values.Length;
                SelectedTipoCarpeta = values[nextIndex];
            }
        }

        private void ItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: AppItem item })
            {
                try
                {
                    _databaseHelper.LogAccess(item.FullPath);
                    var psi = new ProcessStartInfo(item.FullPath)
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(item.FullPath) ?? BaseDir
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"No se pudo abrir:\n{item.FullPath}\n\n{ex.Message}",
                        "Error al abrir", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ResizeItemsInGroups()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_groupedSource?.View == null) return;

                foreach (var group in _groupedSource.View.Groups)
                {
                    if (ListViewItems.ItemContainerGenerator.ContainerFromItem(group) is not GroupItem groupItem) continue;

                    var wrapPanel = FindVisualChild<WrapPanel>(groupItem);
                    if (wrapPanel == null) continue;

                    double maxWidth = 0;
                    double maxHeight = 0;

                    var children = new List<FrameworkElement>();
                    for (int i = 0; i < VisualTreeHelper.GetChildrenCount(wrapPanel); i++)
                    {
                        if (VisualTreeHelper.GetChild(wrapPanel, i) is FrameworkElement child)
                        {
                            child.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                            maxWidth = Math.Max(maxWidth, child.DesiredSize.Width);
                            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
                            children.Add(child);
                        }
                    }

                    foreach (var child in children)
                    {
                        child.Width = maxWidth;
                        child.Height = maxHeight;
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                {
                    return t;
                }

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { DataContext: AppItem item })
            {
                var result = System.Windows.MessageBox.Show($"¿Está seguro de que desea eliminar el acceso directo '{item.Name}'?\n\n{item.FullPath}",
                    "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        File.Delete(item.FullPath);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"No se pudo eliminar el archivo:\n{item.FullPath}\n\n{ex.Message}",
                            "Error al eliminar", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void RenameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { DataContext: AppItem item })
            {
                string newName = Interaction.InputBox("Ingrese el nuevo nombre:", "Renombrar", item.Name);

                if (!string.IsNullOrWhiteSpace(newName) && newName != item.Name)
                {
                    try
                    {
                        string directory = Path.GetDirectoryName(item.FullPath) ?? "";
                        string extension = Path.GetExtension(item.FullPath);
                        string newFullPath = Path.Combine(directory, newName + extension);

                        File.Move(item.FullPath, newFullPath);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"No se pudo renombrar el archivo:\n{item.FullPath}\n\n{ex.Message}",
                            "Error al renombrar", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.TabControl)
            {
                if (MainTabControl.SelectedIndex == 1) // "Más Usados" tab
                {
                    LoadMostUsedItems();
                }
                else if (MainTabControl.SelectedIndex == 2 && !_settingsLoaded) // "Configuracion" tab
                {
                    LoadSettings();
                }
            }
        }

        private void LoadMostUsedItems()
        {
            try
            {
                var items = _databaseHelper.GetTopUsedItems(10);
                MostUsedItems.Clear();
                foreach (var item in items)
                {
                    MostUsedItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al cargar los elementos más usados:\n{ex.Message}",
                    "Error de Base de Datos", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearDatabase_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show("¿Está seguro de que desea borrar todo el historial de uso?",
                "Confirmar Limpieza", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _databaseHelper.ClearLog();
                    LoadMostUsedItems();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"No se pudo limpiar el historial:\n{ex.Message}",
                        "Error de Base de Datos", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadSettings()
        {
            try
            {
                AppSettings.Clear();
                var jsonString = File.ReadAllText("appsettings.json");
                using (JsonDocument document = JsonDocument.Parse(jsonString))
                {
                    FlattenJson(document.RootElement);
                }
                _settingsLoaded = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al cargar la configuración:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FlattenJson(JsonElement element, string prefix = "")
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        FlattenJson(property.Value, $"{prefix}{property.Name}:");
                    }
                    break;
                case JsonValueKind.Array:
                    AppSettings.Add(new SettingEntry { Key = prefix.TrimEnd(':'), Value = element.ToString() });
                    break;
                default:
                    AppSettings.Add(new SettingEntry { Key = prefix.TrimEnd(':'), Value = element.ToString() });
                    break;
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newSettings = new Dictionary<string, object>();
                foreach (var setting in AppSettings)
                {
                    var keys = setting.Key.Split(':');
                    var currentDict = newSettings;
                    for (int i = 0; i < keys.Length - 1; i++)
                    {
                        if (!currentDict.TryGetValue(keys[i], out object? value) || value is not Dictionary<string, object>)
                        {
                            value = new Dictionary<string, object>();
                            currentDict[keys[i]] = value;
                        }
                        currentDict = (Dictionary<string, object>)currentDict[keys[i]];
                    }
                    
                    if (int.TryParse(setting.Value, out int intValue))
                    {
                        currentDict[keys.Last()] = intValue;
                    }
                    else if (double.TryParse(setting.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double doubleValue))
                    {
                        currentDict[keys.Last()] = doubleValue;
                    }
                    else if (bool.TryParse(setting.Value, out bool boolValue))
                    {
                        currentDict[keys.Last()] = boolValue;
                    }
                    else
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(setting.Value);
                            currentDict[keys.Last()] = doc.RootElement.Clone();
                        }
                        catch (JsonException)
                        {
                            currentDict[keys.Last()] = setting.Value;
                        }
                    }
                }

                var jsonString = JsonSerializer.Serialize(newSettings, JsonOptions);
                File.WriteAllText("appsettings.json", jsonString);

                System.Windows.MessageBox.Show("Configuración guardada con éxito. Algunos cambios pueden requerir reiniciar la aplicación.", "Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al guardar la configuración:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class SettingEntry : INotifyPropertyChanged
    {
        public string Key { get; set; } = string.Empty;

        private string _value = string.Empty;
        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}