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
using System.Reflection;
using DragDropEffects = System.Windows.DragDropEffects;
using MessageBox = System.Windows.MessageBox;



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
        public ObservableCollection<KeyboardShortcut> KeyboardShortcuts { get; } = [];
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

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                VersionLabel.Content = $"Accesos Launcher Ver. {version.Major}.{version.Minor}.{version.Build}";
            }
            DataContext = this;
            this.KeyDown += MainWindow_KeyDown;
            var connectionString = App.Configuration.GetConnectionString("Sqlite") ?? "";
            _databaseHelper = new DatabaseHelper(connectionString);
            LoadKeyboardShortcuts();
            
            // Initialize drag and drop variables
            _draggedItem = null;
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
                
                // Get all directories first, including empty ones (but excluding hidden ones)
                var directories = existed 
                    ? Directory.EnumerateDirectories(BaseDir, "*", SearchOption.AllDirectories)
                        .Where(dir => !IsHidden(dir))
                    : [];
                
                // Get all launchable files (but excluding those in hidden directories)
                var files = existed
                    ? Directory.EnumerateFiles(BaseDir, "*.*", SearchOption.AllDirectories)
                        .Where(f => IsLaunchable(f) && !IsInHiddenDirectory(f))
                    : [];

                var list = await Task.Run(() =>
                {
                    var tmp = new List<AppItem>();
                    
                    // Add launchable files
                    foreach (var f in files)
                    {
                        var dir = Path.GetDirectoryName(f) ?? BaseDir;
                        var rel = Path.GetRelativePath(BaseDir, dir);
                        if (string.IsNullOrWhiteSpace(rel) || rel == "." || rel == @"")
                            rel = "Raiz";

                        var name = Path.GetFileNameWithoutExtension(f);
                        var icon = IconHelper.GetIconImageSource(f);

                        tmp.Add(new AppItem
                        {
                            Name = name,
                            FullPath = f,
                            RelativeDirectory = rel,
                            Icon = icon,
                            IsEmptyFolder = false
                        });
                    }
                    
                    // Add empty directories (but excluding hidden ones)
                    foreach (var dir in directories)
                    {
                        // Check if this directory is already represented by files
                        var relDir = Path.GetRelativePath(BaseDir, dir);
                        bool hasFilesInDir = tmp.Any(item => item.RelativeDirectory == relDir);
                        
                        // If directory is empty and not already represented, add a placeholder
                        if (!hasFilesInDir)
                        {
                            var dirName = Path.GetFileName(dir);
                            // Create a placeholder item for empty directory
                            tmp.Add(new AppItem
                            {
                                Name = dirName,
                                FullPath = dir,
                                RelativeDirectory = relDir,
                                Icon = IconHelper.GetFolderIcon(),
                                IsEmptyFolder = true
                            });
                        }
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
                System.Windows.MessageBox.Show($"Error al leer accesos:{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool IsHidden(string path)
        {
            try
            {
                var attributes = File.GetAttributes(path);
                return attributes.HasFlag(FileAttributes.Hidden);
            }
            catch
            {
                // If we can't get attributes, assume it's not hidden
                return false;
            }
        }

        private static bool IsInHiddenDirectory(string filePath)
        {
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                while (!string.IsNullOrEmpty(dir) && dir != Path.GetPathRoot(dir))
                {
                    if (IsHidden(dir))
                        return true;
                    dir = Path.GetDirectoryName(dir);
                }
                return false;
            }
            catch
            {
                // If we can't check directories, assume it's not in a hidden directory
                return false;
            }
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

        private static bool IsLaunchable(string path)
        {
            var ext = Path.GetExtension(path);
            return !string.IsNullOrEmpty(ext) && ExecExtensions.Contains(ext);
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
            // Check if we're in the middle of a drag operation
            if (_draggedItem != null)
            {
                // If we were dragging, don't execute the click
                System.Diagnostics.Debug.WriteLine("ItemButton_Click: Drag operation detected, not executing click");
                _draggedItem = null;
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("ItemButton_Click: Executing normal click");
            
            if (sender is FrameworkElement { Tag: AppItem item })
            {
                // Handle empty folder items
                if (item.IsEmptyFolder)
                {
                    try
                    {
                        // Open the folder in Windows Explorer
                        Process.Start("explorer.exe", item.FullPath);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"No se pudo abrir la carpeta:\n{item.FullPath}\n\n{ex.Message}",
                            "Error al abrir carpeta", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // Handle regular file items
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
                        System.Windows.MessageBox.Show($@"No se pudo abrir:
{item.FullPath}

{ex.Message}", "Error al abrir", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            
            // Ensure _draggedItem is cleared after normal click
            _draggedItem = null;
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
                    if (ListViewItems.ItemContainerGenerator.ContainerFromItem(group) is not System.Windows.Controls.GroupItem groupItem) continue;

                    var wrapPanel = FindVisualChildOfType<System.Windows.Controls.WrapPanel>(groupItem);
                    if (wrapPanel == null) continue;

                    double maxWidth = 0;
                    double maxHeight = 0;

                    var children = new List<System.Windows.FrameworkElement>();
                    for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(wrapPanel); i++)
                    {
                        if (System.Windows.Media.VisualTreeHelper.GetChild(wrapPanel, i) is System.Windows.FrameworkElement child)
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

        private async void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { DataContext: AppItem item })
            {
                var result = System.Windows.MessageBox.Show($"¿Está seguro de que desea eliminar el acceso directo '{item.Name}'?\n\n{item.FullPath}",
                    "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var fileInfo = new FileInfo(item.FullPath);
                        if (fileInfo.Exists && fileInfo.IsReadOnly)
                        {
                            fileInfo.IsReadOnly = false;
                        }

                        File.Delete(item.FullPath);
                        await LoadItems();
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

        private void RenameFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu contextMenu && 
                contextMenu.PlacementTarget is TextBlock textBlock)
            {
                // Obtener el nombre de la carpeta del texto del TextBlock
                string folderName = textBlock.Text;
                
                // Encontrar la ruta completa de la carpeta
                string folderPath = Path.Combine(BaseDir, folderName);
                
                // Para carpetas en subdirectorios, necesitamos obtener la ruta relativa
                // Vamos a buscar en los grupos para encontrar la ruta correcta
                var groupedView = _groupedSource?.View;
                if (groupedView != null)
                {
                    foreach (CollectionViewGroup group in groupedView.Groups.Cast<CollectionViewGroup>())
                    {
                        if (group.Name.ToString() == folderName)
                        {
                            // Para el grupo "Raiz", la ruta es BaseDir
                            if (folderName == "Raiz")
                            {
                                folderPath = BaseDir;
                            }
                            else
                            {
                                folderPath = Path.Combine(BaseDir, folderName);
                            }
                            break;
                        }
                    }
                }

                string newName = Interaction.InputBox("Ingrese el nuevo nombre para la carpeta:", "Renombrar Carpeta", folderName);

                if (!string.IsNullOrWhiteSpace(newName) && newName != folderName)
                {
                    try
                    {
                        string parentDirectory = Path.GetDirectoryName(folderPath) ?? BaseDir;
                        string newFolderPath = Path.Combine(parentDirectory, newName);
                        
                        // Verificar que la nueva ruta no exista
                        if (Directory.Exists(newFolderPath))
                        {
                            System.Windows.MessageBox.Show($"Ya existe una carpeta con el nombre '{newName}'.", 
                                "Error al renombrar", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        Directory.Move(folderPath, newFolderPath);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"No se pudo renombrar la carpeta:\n{folderPath}\n\n{ex.Message}",
                            "Error al renombrar", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OpenFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu contextMenu && 
                contextMenu.PlacementTarget is TextBlock textBlock)
            {
                // Obtener el nombre de la carpeta del texto del TextBlock
                string folderName = textBlock.Text;
                
                // Determinar la ruta de la carpeta
                string folderPath = folderName == "Raiz" ? BaseDir : Path.Combine(BaseDir, folderName);

                try
                {
                    // Abrir la carpeta en el Explorador de Windows
                    Process.Start("explorer.exe", folderPath);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"No se pudo abrir la carpeta:\n{folderPath}\n\n{ex.Message}",
                        "Error al abrir carpeta", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

       


        private async void CreateFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Get the folder name from the context menu
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.PlacementTarget is TextBlock textBlock)
            {
                string folderName = textBlock.Text;

                // Ask the user for the new folder name
                string newFolderName = Microsoft.VisualBasic.Interaction.InputBox("Ingrese el nombre de la nueva carpeta:", "Crear carpeta", "");

                if (!string.IsNullOrWhiteSpace(newFolderName))
                {
                    try
                    {
                        // Validate the folder name (check for invalid characters)
                        var invalidChars = Path.GetInvalidFileNameChars();
                        if (newFolderName.IndexOfAny(invalidChars) >= 0)
                        {
                            System.Windows.MessageBox.Show("El nombre de la carpeta contiene caracteres no válidos.",
                                "Error al crear carpeta", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        string newFolderPath;
                        if (folderName == "Raiz")
                        {
                            // Creating a folder in the root directory
                            newFolderPath = Path.Combine(BaseDir, newFolderName);
                        }
                        else
                        {
                            // Creating a folder in a subdirectory
                            string parentPath = Path.Combine(BaseDir, folderName);
                            newFolderPath = Path.Combine(parentPath, newFolderName);
                        }

                        // Check if the directory already exists
                        if (Directory.Exists(newFolderPath))
                        {
                            System.Windows.MessageBox.Show($"Ya existe una carpeta con el nombre '{newFolderName}'.",
                                "Error al crear carpeta", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // Create the directory
                        Directory.CreateDirectory(newFolderPath);

                        // Refresh the view to show the new folder
                        await LoadItems();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"No se pudo crear la carpeta:\n{ex.Message}",
                            "Error al crear carpeta", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void DeleteFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Get the folder name from the context menu
            if (sender is MenuItem menuItem &&
                menuItem.Parent is ContextMenu contextMenu &&
                contextMenu.PlacementTarget is TextBlock textBlock)
            {
                string folderName = textBlock.Text;

                // Don't allow deletion of the root folder
                if (folderName == "Raiz")
                {
                    System.Windows.MessageBox.Show("No se puede eliminar la carpeta raíz.",
                        "Eliminar carpeta", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if the folder is empty
                string folderPath = Path.Combine(BaseDir, folderName);
                if (Directory.Exists(folderPath))
                {
                    try
                    {
                        var files = Directory.GetFiles(folderPath);
                        var subdirs = Directory.GetDirectories(folderPath);
                        
                        if (files.Length > 0 || subdirs.Length > 0)
                        {
                            // Confirm with the user for non-empty folder
                            var result = System.Windows.MessageBox.Show($"La carpeta '{folderName}' no está vacía.¿Está seguro de que desea eliminar la carpeta y todos sus contenidos?",
                                "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                            if (result != MessageBoxResult.Yes)
                            {
                                return;
                            }
                        }
                        else
                        {
                            // Confirm with the user for empty folder
                            var result = System.Windows.MessageBox.Show($"¿Está seguro de que desea eliminar la carpeta vacía '{folderName}'?",
                                "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (result != MessageBoxResult.Yes)
                            {
                                return;
                            }
                        }

                        // Force remove read-only attributes
                        var directoryInfo = new DirectoryInfo(folderPath);
                        foreach (var info in directoryInfo.GetFileSystemInfos("*", SearchOption.AllDirectories))
                        {
                            info.Attributes = FileAttributes.Normal;
                        }

                        // Delete the directory and all its contents
                        Directory.Delete(folderPath, true);

                        // Refresh the view to reflect the changes
                        await LoadItems();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"No se pudo eliminar la carpeta: {ex.Message}",
                            "Error al eliminar carpeta", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show($"La carpeta '{folderName}' no existe.",
                        "Error al eliminar carpeta", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        

        private void OpenAllFilesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && 
                menuItem.Parent is ContextMenu contextMenu && 
                contextMenu.PlacementTarget is TextBlock textBlock)
            {
                // Obtener el nombre de la carpeta del texto del TextBlock
                string folderName = textBlock.Text;
                
                // Determinar la ruta de la carpeta
                string folderPath = folderName == "Raiz" ? BaseDir : Path.Combine(BaseDir, folderName);

                try
                {
                    // Obtener todos los archivos ejecutables en la carpeta
                    var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(f => IsLaunchable(f));

                    // Abrir todos los archivos
                    foreach (var file in files)
                    {
                        try
                        {
                            var psi = new ProcessStartInfo(file)
                            {
                                UseShellExecute = true,
                                WorkingDirectory = Path.GetDirectoryName(file) ?? BaseDir
                            };
                            Process.Start(psi);
                        }
                        catch (Exception ex)
                        {
                            // Registrar el error pero continuar con los demás archivos
                            System.Diagnostics.Debug.WriteLine($"No se pudo abrir el archivo {file}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"No se pudo acceder a la carpeta:\n{folderPath}\n\n{ex.Message}",
                        "Error al abrir archivos", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FolderContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            var contextMenu = sender as ContextMenu;
            if (contextMenu == null) return;

            contextMenu.Opened -= FolderContextMenu_Opened;
            contextMenu.Opened += FolderContextMenu_Opened;

            var typeSubmenu = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(item => "Tipo".Equals(item.Header));
            if (typeSubmenu != null)
            {
                foreach (MenuItem item in typeSubmenu.Items)
                {
                    item.Click -= FolderTypeMenuItem_Click;
                    item.Click += FolderTypeMenuItem_Click;
                }
            }
        }

        private void FolderContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            var contextMenu = sender as ContextMenu;
            if (contextMenu?.PlacementTarget is not TextBlock textBlock) return;

            var typeSubmenu = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(item => "Tipo".Equals(item.Header) || (item.Header is string s && s.EndsWith("Tipo")));
            if (typeSubmenu == null) return;

            string folderName = textBlock.Text;
            string folderPath = folderName == "Raiz" ? BaseDir : Path.Combine(BaseDir, folderName);

            if (!Directory.Exists(folderPath))
            {
                typeSubmenu.IsEnabled = false;
                return;
            }

            typeSubmenu.IsEnabled = true;

            var hasPersonalFile = File.Exists(Path.Combine(folderPath, ".personal"));
            var hasMixtaFile = File.Exists(Path.Combine(folderPath, ".mixta"));

            TipoCarpeta currentType;
            if (hasMixtaFile)
                currentType = TipoCarpeta.Ambos;
            else if (hasPersonalFile)
                currentType = TipoCarpeta.Personal;
            else
                currentType = TipoCarpeta.Laboral;

            foreach (MenuItem item in typeSubmenu.Items)
            {
                if (item.Tag is string typeName)
                {
                    item.Header = typeName;
                    if (typeName == currentType.ToString())
                    {
                        item.Header = $"✓ {typeName}";
                    }
                }
            }
        }

        private void FolderTypeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem clickedItem ||
                clickedItem.Tag is not string selectedTypeStr ||
                !Enum.TryParse(selectedTypeStr, out TipoCarpeta selectedType)) return;

            if (clickedItem.Parent is not MenuItem parentMenu ||
                parentMenu.Parent is not ContextMenu contextMenu ||
                contextMenu.PlacementTarget is not TextBlock textBlock) return;

            string folderName = textBlock.Text;
            string folderPath = folderName == "Raiz" ? BaseDir : Path.Combine(BaseDir, folderName);

            if (!Directory.Exists(folderPath)) return;

            var personalFilePath = Path.Combine(folderPath, ".personal");
            var mixtaFilePath = Path.Combine(folderPath, ".mixta");

            try
            {
                // Clean up existing files first
                if (File.Exists(personalFilePath)) File.Delete(personalFilePath);
                if (File.Exists(mixtaFilePath)) File.Delete(mixtaFilePath);

                // Create new files based on selection
                if (selectedType == TipoCarpeta.Personal)
                {
                    File.Create(personalFilePath).Close(); // Create and immediately close
                }
                else if (selectedType == TipoCarpeta.Ambos)
                {
                    File.Create(mixtaFilePath).Close(); // Create and immediately close
                }
                // For Laboral, we just needed to delete the files, which we already did.

                // Refresh the main filter to apply changes
                _groupedSource?.View?.Refresh();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"No se pudo cambiar el tipo de la carpeta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // Usar Dispatcher para asegurar que los controles estén completamente cargados
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    switch (MainTabControl.SelectedIndex)
                    {
                        case 0: // "Accesos" tab
                            SearchBox.Focus();
                            break;
                        case 1: // "Más Usados" tab
                            LoadMostUsedItems();
                            // Enfocar la grilla después de cargar los datos
                            ListViewItems.Focus();
                            break;
                        case 2: // "Configuracion" tab
                            if (!_settingsLoaded)
                            {
                                LoadSettings();
                            }
                            // Enfocar la grilla de configuración
                            if (MainTabControl.SelectedContent is DockPanel dockPanel)
                            {
                                var dataGrid = FindVisualChild<DataGrid>(dockPanel);
                                dataGrid?.Focus();
                            }
                            break;
                        case 3: // "Acerca de" tab
                            // No se requiere acción especial para esta pestaña
                            break;
                    }
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
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

        private void LoadKeyboardShortcuts()
        {
            KeyboardShortcuts.Add(new KeyboardShortcut { Shortcut = "Ctrl + Alt + T", Description = "Mostrar/ocultar la ventana principal de la aplicación" });
            KeyboardShortcuts.Add(new KeyboardShortcut { Shortcut = "Tecla de Windows + Shift + Flecha Izq. o Der.", Description = "Mover la ventana principal de la aplicación a otro monitor" });
            KeyboardShortcuts.Add(new KeyboardShortcut { Shortcut = "Escape", Description = "Ocultar la ventana y minimizar a la bandeja del sistema" });
            KeyboardShortcuts.Add(new KeyboardShortcut { Shortcut = "Ctrl + T", Description = "Cambiar entre tipos de carpeta (Ambos, Personal, Laboral)" });
            KeyboardShortcuts.Add(new KeyboardShortcut { Shortcut = "Tab", Description = "Navegar entre las pestañas principales (Accesos, Más Usados, Configuración, Acerca de)" });
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

        private void ToggleWindowState_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            HideToTray();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var psi = new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true };
            Process.Start(psi);
        }

        [SupportedOSPlatform("windows6.1")]
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadItems();
        }

        private void CreateRootDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            string newFolderName = Interaction.InputBox("Ingrese el nombre de la nueva carpeta en la raíz:", "Crear Directorio en Raíz", "");

            if (string.IsNullOrWhiteSpace(newFolderName))
            {
                return; // User cancelled or entered empty name
            }

            try
            {
                // Validate folder name
                var invalidChars = Path.GetInvalidFileNameChars();
                if (newFolderName.IndexOfAny(invalidChars) >= 0)
                {
                    System.Windows.MessageBox.Show("El nombre de la carpeta contiene caracteres no válidos.",
                        "Error al crear carpeta", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string newFolderPath = Path.Combine(BaseDir, newFolderName);

                if (Directory.Exists(newFolderPath))
                {
                    System.Windows.MessageBox.Show($"Ya existe una carpeta con el nombre '{newFolderName}' en la raíz.",
                        "Error al crear carpeta", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Directory.CreateDirectory(newFolderPath);
                
                // The FileSystemWatcher will automatically detect the change and refresh the list.
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"No se pudo crear la carpeta:\n{ex.Message}",
                    "Error al crear carpeta", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Drag and Drop Implementation

        private System.Windows.Point _startPoint;
        private AppItem? _draggedItem;

        private void ItemButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Store the mouse position for later use
            _startPoint = e.GetPosition(null);
            
            // Store the dragged item only when we actually start dragging
            // For now, just store the sender to identify the source
            if (sender is System.Windows.Controls.Button button && button.Tag is AppItem item)
            {
                // We'll set _draggedItem in the PreviewMouseMove event when we detect actual movement
                System.Diagnostics.Debug.WriteLine($"ItemButton_PreviewMouseLeftButtonDown: Preparing for potential drag for {item.Name}");
            }
        }

        private void ItemButton_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Check if the left mouse button is pressed
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                // Get the current mouse position
                System.Windows.Point mousePos = e.GetPosition(null);
                System.Windows.Vector diff = _startPoint - mousePos;

                System.Diagnostics.Debug.WriteLine($"ItemButton_PreviewMouseMove: StartPoint=({_startPoint.X}, {_startPoint.Y}), Current=({mousePos.X}, {mousePos.Y}), Diff=({diff.X}, {diff.Y})");
                System.Diagnostics.Debug.WriteLine($"ItemButton_PreviewMouseMove: Thresholds=({SystemParameters.MinimumHorizontalDragDistance}, {SystemParameters.MinimumVerticalDragDistance})");

                // Check if the mouse has moved more than the system drag threshold
                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // Only set _draggedItem when we actually start dragging
                    if (_draggedItem == null && sender is System.Windows.Controls.Button button && button.Tag is AppItem item)
                    {
                        _draggedItem = item;
                        System.Diagnostics.Debug.WriteLine($"ItemButton_PreviewMouseMove: Initiating drag for {item.Name}");
                        // Initiate the drag-and-drop operation
                        System.Windows.DataObject data = new("AppItem", _draggedItem);
                        System.Windows.DragDrop.DoDragDrop((System.Windows.DependencyObject)sender, data, System.Windows.DragDropEffects.Move);
                        e.Handled = true;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ItemButton_PreviewMouseMove: Movement below threshold");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"ItemButton_PreviewMouseMove: LeftButton={e.LeftButton}, _draggedItem={_draggedItem != null}");
            }
        }

        private void ListView_Drop(object sender, System.Windows.DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ListView_Drop: Drop event triggered");
            
            // Handle internal item reordering (moving between directories)
            if (e.Data.GetDataPresent("AppItem") && _draggedItem != null)
            {
                System.Diagnostics.Debug.WriteLine($"ListView_Drop: Handling internal move for {_draggedItem.Name}");
                
                // Get the dropped item
                if (e.Data.GetData("AppItem") is AppItem droppedItem)
                {
                    // Get the target directory (where the item was dropped)
                    System.Windows.Controls.ListViewItem? targetListViewItem = FindAncestor<System.Windows.Controls.ListViewItem>((System.Windows.DependencyObject)e.OriginalSource);
                    
                    if (targetListViewItem != null && targetListViewItem.Content is AppItem targetItem)
                    {
                        System.Diagnostics.Debug.WriteLine($"ListView_Drop: Moving {droppedItem.Name} to directory of {targetItem.Name}");
                        // Move the item to the target directory
                        MoveItemToDirectory(droppedItem, targetItem.RelativeDirectory);
                    }
                    else
                    {
                        // Check if dropped on a group header (directory name)
                        var groupItem = FindAncestor<System.Windows.Controls.GroupItem>((System.Windows.DependencyObject)e.OriginalSource);
                        if (groupItem != null)
                        {
                            // For group items, we need to find the ContentPresenter that holds the header
                            var headerPresenter = FindVisualChildOfType<System.Windows.Controls.ContentPresenter>(groupItem);
                            if (headerPresenter != null && headerPresenter.Content is System.Windows.Data.CollectionViewGroup group)
                            {
                                string targetDirectory = group.Name.ToString()??"";
                                System.Diagnostics.Debug.WriteLine($"ListView_Drop: Moving {droppedItem.Name} to directory {targetDirectory}");
                                MoveItemToDirectory(droppedItem, targetDirectory);
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"ListView_Drop: No target directory found, moving {droppedItem.Name} to root");
                            // If dropped on the ListView itself (not on an item), move to root
                            MoveItemToDirectory(droppedItem, "Raiz");
                        }
                    }
                }
                
                _draggedItem = null;
                e.Handled = true;
                return;
            }

            // Handle files dropped from outside the application (copying from real directories)
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                System.Diagnostics.Debug.WriteLine("ListView_Drop: Handling external files");
                
                if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
                {
                    // Get the target directory (where the files were dropped)
                    string targetDirectory = "Raiz"; // Default to root
                    
                    // Check if dropped on a group header (directory name)
                    var groupItem = FindAncestor<System.Windows.Controls.GroupItem>((System.Windows.DependencyObject)e.OriginalSource);
                    if (groupItem != null)
                    {
                        // For group items, we need to find the ContentPresenter that holds the header
                        var headerPresenter = FindVisualChildOfType<System.Windows.Controls.ContentPresenter>(groupItem);
                        if (headerPresenter != null && headerPresenter.Content is System.Windows.Data.CollectionViewGroup group)
                        {
                            targetDirectory = group.Name.ToString() ?? "";
                            System.Diagnostics.Debug.WriteLine($"ListView_Drop: Target directory from group header is {targetDirectory}");
                        }
                    }
                    else
                    {
                        // Check if dropped on a list item
                        System.Windows.Controls.ListViewItem? targetListViewItem = FindAncestor<System.Windows.Controls.ListViewItem>((System.Windows.DependencyObject)e.OriginalSource);
                        if (targetListViewItem != null && targetListViewItem.Content is AppItem targetItem)
                        {
                            targetDirectory = targetItem.RelativeDirectory;
                            System.Diagnostics.Debug.WriteLine($"ListView_Drop: Target directory from list item is {targetDirectory}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"ListView_Drop: No specific target directory found, using root");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"ListView_Drop: Final target directory is {targetDirectory}");
                    
                    foreach (string file in files)
                    {
                        System.Diagnostics.Debug.WriteLine($"ListView_Drop: Processing file {file}");
                        
                        string extension = System.IO.Path.GetExtension(file).ToLower();
                        if (extension == ".lnk" || extension == ".url")
                        {
                            // Copy the file to the target directory
                            CopyFileToDirectory(file, targetDirectory);
                        }
                        else
                        {
                            // For other file types, create a shortcut (.lnk) to the file
                            CreateShortcutToDirectory(file, targetDirectory);
                        }
                    }
                    
                    // Refresh the grouped view
                    _groupedSource?.View?.Refresh();
                    ResizeItemsInGroups();
                }
                
                e.Handled = true;
            }
        }

        private void ListView_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ListView_DragEnter: Drag enter event triggered");
            
            // Check if the data being dragged is a file
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                System.Diagnostics.Debug.WriteLine("ListView_DragEnter: External files detected");
                
                // Check if all files are .lnk or .url
                if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
                {
                    bool allValid = files.All(file => 
                    {
                        string extension = System.IO.Path.GetExtension(file).ToLower();
                        System.Diagnostics.Debug.WriteLine($"ListView_DragEnter: Checking file {file} with extension {extension}");
                        return extension == ".lnk" || extension == ".url";
                    });
                    
                    e.Effects = allValid ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
                    System.Diagnostics.Debug.WriteLine($"ListView_DragEnter: Setting effects to {e.Effects}");
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                    System.Diagnostics.Debug.WriteLine("ListView_DragEnter: No valid files found");
                }
            }
            // Check if the data being dragged is an AppItem (internal drag)
            else if (e.Data.GetDataPresent("AppItem"))
            {
                System.Diagnostics.Debug.WriteLine("ListView_DragEnter: Internal AppItem detected");
                e.Effects = System.Windows.DragDropEffects.Move;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
                System.Diagnostics.Debug.WriteLine("ListView_DragEnter: No valid data detected");
            }
            
            e.Handled = true;
        }

        private void MoveItemToDirectory(AppItem item, string targetDirectory)
        {
            try
            {
                // If the target directory is the same as the current directory, do nothing
                if (String.IsNullOrEmpty(targetDirectory))
                {
                    System.Diagnostics.Debug.WriteLine($"MoveItemToDirectory: targetDirectory IsNullOrEmpty ");
                    return;
                }



                // Get the current directory of the item
                string currentDirectory = item.RelativeDirectory;
                
                // If the target directory is the same as the current directory, do nothing
                if (currentDirectory == targetDirectory)
                {
                    System.Diagnostics.Debug.WriteLine($"MoveItemToDirectory: Item {item.Name} is already in directory {targetDirectory}");
                    return;
                }
                
                // Determine the full paths
                string currentPath = item.FullPath;
                string fileName = System.IO.Path.GetFileName(currentPath);
                string targetPath;
                
                if (targetDirectory == "Raiz")
                {
                    targetPath = System.IO.Path.Combine(BaseDir, fileName);
                }
                else
                {
                    string targetDirPath = System.IO.Path.Combine(BaseDir, targetDirectory);
                    // Create the target directory if it doesn't exist
                    if (!System.IO.Directory.Exists(targetDirPath))
                    {
                        System.IO.Directory.CreateDirectory(targetDirPath);
                    }
                    targetPath = System.IO.Path.Combine(targetDirPath, fileName);
                }
                
                // Move the file
                System.IO.File.Move(currentPath, targetPath);
                
                // Update the item in the collection
                item.RelativeDirectory = targetDirectory;
                item.FullPath = targetPath;
                
                // Refresh the grouped view
                _groupedSource?.View?.Refresh();
                ResizeItemsInGroups();
                
                System.Diagnostics.Debug.WriteLine($"MoveItemToDirectory: Moved {item.Name} from {currentDirectory} to {targetDirectory}");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MoveItemToDirectory: Error moving {item.Name} - {ex.Message}");
                System.Windows.MessageBox.Show($"Error al mover el archivo:\n{ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void CopyFileToDirectory(string sourcePath, string targetDirectory)
        {
            try
            {
                string fileName = System.IO.Path.GetFileName(sourcePath);
                string targetPath;
                
                if (targetDirectory == "Raiz")
                {
                    targetPath = System.IO.Path.Combine(BaseDir, fileName);
                }
                else
                {
                    string targetDirPath = System.IO.Path.Combine(BaseDir, targetDirectory);
                    // Create the target directory if it doesn't exist
                    if (!System.IO.Directory.Exists(targetDirPath))
                    {
                        System.IO.Directory.CreateDirectory(targetDirPath);
                    }
                    targetPath = System.IO.Path.Combine(targetDirPath, fileName);
                }
                
                // Copy the file
                System.IO.File.Copy(sourcePath, targetPath, true);
                
                // Create a new AppItem for the copied file
                string name = System.IO.Path.GetFileNameWithoutExtension(targetPath);
                System.Windows.Media.Imaging.BitmapImage icon = (System.Windows.Media.Imaging.BitmapImage)IconHelper.GetIconImageSource(targetPath);
                
                AppItem newItem = new()
                {
                    Name = name,
                    FullPath = targetPath,
                    RelativeDirectory = targetDirectory,
                    Icon = icon
                };
                
                // Add the new item to the collection
                Items.Add(newItem);
                
                System.Diagnostics.Debug.WriteLine($"CopyFileToDirectory: Copied {fileName} to {targetDirectory}");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CopyFileToDirectory: Error copying {sourcePath} - {ex.Message}");
                System.Windows.MessageBox.Show($"Error al copiar el archivo:\n{ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void CreateShortcutToDirectory(string sourcePath, string targetDirectory)
        {
            try
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(sourcePath) + ".lnk";
                string targetPath;
                
                if (targetDirectory == "Raiz")
                {
                    targetPath = System.IO.Path.Combine(BaseDir, fileName);
                }
                else
                {
                    string targetDirPath = System.IO.Path.Combine(BaseDir, targetDirectory);
                    // Create the target directory if it doesn't exist
                    if (!System.IO.Directory.Exists(targetDirPath))
                    {
                        System.IO.Directory.CreateDirectory(targetDirPath);
                    }
                    targetPath = System.IO.Path.Combine(targetDirPath, fileName);
                }

                // Create a shortcut to the file
                CreateShortcut(sourcePath, targetPath);
                
                // Create a new AppItem for the shortcut
                string name = System.IO.Path.GetFileNameWithoutExtension(targetPath);
                System.Windows.Media.Imaging.BitmapImage icon = (System.Windows.Media.Imaging.BitmapImage)IconHelper.GetIconImageSource(targetPath);

                AppItem newItem = new()
                {
                    Name = name,
                    FullPath = targetPath,
                    RelativeDirectory = targetDirectory,
                    Icon = icon
                };
                
                // Add the new item to the collection
                Items.Add(newItem);
                
                System.Diagnostics.Debug.WriteLine($"CreateShortcutToDirectory: Created shortcut for {sourcePath} in {targetDirectory}");
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateShortcutToDirectory: Error creating shortcut for {sourcePath} - {ex.Message}");
                System.Windows.MessageBox.Show($"Error al crear el acceso directo:\n{ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        
        private static void CreateShortcut(string sourcePath, string shortcutPath)
        {
            try
            {
                // Use Windows Script Host to create the shortcut
                Type? t = Type.GetTypeFromProgID("WScript.Shell");
                if (t is null)
                {
                    System.Diagnostics.Debug.WriteLine($"CreateShortcut: Error creating shortcut from {sourcePath} to {shortcutPath} - Type? t = Type.GetTypeFromProgID(\"WScript.Shell\")");
                    throw new System.Exception($"No se pudo crear el acceso directo: {shortcutPath}");
                }

                dynamic? shell = Activator.CreateInstance(t);
                if (shell is null) 
                {
                    System.Diagnostics.Debug.WriteLine($"CreateShortcut: Error creating shortcut from {sourcePath} to {shortcutPath} -  dynamic? shell = Activator.CreateInstance(t);");
                    throw new System.Exception($"No se pudo crear el acceso directo: {sourcePath}");
                }


                var shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = sourcePath;
                shortcut.Save();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateShortcut: Error creating shortcut from {sourcePath} to {shortcutPath} - {ex.Message}");
                throw new System.Exception($"No se pudo crear el acceso directo: {ex.Message}", ex);
            }
        }

        // Helper method to find the ancestor of a specific type
        private static T? FindAncestor<T>(System.Windows.DependencyObject current) where T : System.Windows.DependencyObject
        {
            do
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }
        
        // Helper method to find a visual child of a specific type
        private static T? FindVisualChildOfType<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                
                var childOfChild = FindVisualChildOfType<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        #endregion
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

    public class KeyboardShortcut
    {
        public string Shortcut { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}