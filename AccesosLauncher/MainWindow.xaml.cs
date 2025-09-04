using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
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
using Path = System.IO.Path;
using Timer = System.Timers.Timer;


namespace AccesosLauncher
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public static string BaseDir { get; set; } = "C:\\_Accesos";

        // Extensiones ejecutables: PATHEXT + .lnk + .url
        private static readonly HashSet<string> ExecExtensions = InitExecExtensions();

        public ObservableCollection<AppItem> Items { get; } = [];

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
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _groupedSource = (CollectionViewSource)FindResource("GroupedItems");
            _groupedSource.Filter += GroupedSource_Filter;

            EnsureBaseDir();
            LoadItems(); // async
            SetupWatcher();
            SetupTrayIcon();

            // Arranca en segundo plano
            HideToTray();
        }

        private void GroupedSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not AppItem item) { e.Accepted = false; return; }
            e.Accepted = MatchesSearch(item);
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
        private async void LoadItems()
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
                        var icon = IconHelper.GetIconImageSource(f, preferLarge: true);

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
                _debounceTimer.Elapsed += (_, __) => Dispatcher.Invoke(LoadItems);
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

            _notifyIcon?.Dispose();
            if (_source != null)
            {
                UnregisterHotKey(_hwnd, HOTKEY_ID);
                _source.RemoveHook(WndProc);
            }
            _watcher?.Dispose();
            _debounceTimer?.Dispose();
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                _reallyExit = true;
                Close();
            }
        }

        private void ItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: AppItem item })
            {
                try
                {
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

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnregisterHotKey(IntPtr hWnd, int id);
    }

    public class AppItem
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string RelativeDirectory { get; set; } = "";
        public ImageSource? Icon { get; set; }
    }

    
}