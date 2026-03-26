using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Data.Sqlite;

namespace AccesosLauncher
{
    /// <summary>
    /// Ventana dedicada para edición de descripciones Markdown.
    /// </summary>
    public partial class MarkdownEditorWindow : Window
    {
        private string _originalContent;
        private readonly int _proyectoId;
        private readonly string _connectionString;
        private readonly DispatcherTimer _debounceTimer;
        private bool _isWebView2Initialized = false;
        private bool _isSaved = false; // Bandera para evitar popup de confirmación después de guardar

        /// <summary>
        /// Contenido modificado si el usuario guardó.
        /// </summary>
        public string? EditedContent { get; private set; }

        /// <summary>
        /// Constructor que recibe el contenido original, el ID del proyecto y la cadena de conexión.
        /// </summary>
        /// <param name="initialContent">Contenido Markdown a editar</param>
        /// <param name="proyectoId">ID del proyecto en la base de datos</param>
        /// <param name="connectionString">Cadena de conexión a la base de datos</param>
        public MarkdownEditorWindow(string initialContent, int proyectoId, string connectionString)
        {
            InitializeComponent();

            _originalContent = initialContent ?? string.Empty;
            _proyectoId = proyectoId;
            _connectionString = connectionString;

            // Configurar el timer de debounce (500ms para evitar parpadeo)
            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _debounceTimer.Tick += DebounceTimer_Tick;

            // Desactivar temporalmente el evento para cargar el contenido inicial
            txtEditor.TextChanged -= txtEditor_TextChanged;
            txtEditor.Text = _originalContent;
            txtEditor.TextChanged += txtEditor_TextChanged;
        }

        /// <summary>
        /// Evento Loaded de la ventana - inicializa WebView2.
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await InitializeWebView2Async();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al inicializar WebView2:\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Inicializa el WebView2 de forma asíncrona.
        /// </summary>
        private async System.Threading.Tasks.Task InitializeWebView2Async()
        {
            // Crear entorno de WebView2 con opciones de configuración
            var environment = await CoreWebView2Environment.CreateAsync();
            await webPreview.EnsureCoreWebView2Async(environment);
            
            _isWebView2Initialized = true;

            // Renderizar contenido inicial
            UpdatePreview(_originalContent);
        }

        /// <summary>
        /// Evento TextChanged del editor - activa el debounce.
        /// </summary>
        private void txtEditor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Reiniciar el timer (debounce de 300ms)
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        /// <summary>
        /// Timer Tick - renderiza el preview después de 300ms sin typing.
        /// </summary>
        private void DebounceTimer_Tick(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();
            UpdatePreview(txtEditor.Text);
        }

        /// <summary>
        /// Actualiza el preview de WebView2 con el contenido Markdown renderizado.
        /// </summary>
        private void UpdatePreview(string markdownContent)
        {
            if (!_isWebView2Initialized) return;

            try
            {
                var html = MarkdownRendererHelper.GetHtmlTemplate(markdownContent, isEditing: false);
                webPreview.NavigateToString(html);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al renderizar preview: {ex.Message}");
            }
        }

        /// <summary>
        /// Botón Guardar - persiste los cambios y cierra la ventana.
        /// </summary>
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var newContent = txtEditor.Text;

            // Verificar si hay cambios
            if (newContent != _originalContent)
            {
                try
                {
                    // Persistir a la base de datos
                    UpdateDescripcionInDatabase(newContent);
                    
                    // Guardar el contenido modificado
                    EditedContent = newContent;
                    
                    // Marcar como guardado para evitar popup de confirmación en OnClosing
                    _isSaved = true;
                    
                    // Cerrar con DialogResult.OK
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    _isSaved = false;
                    System.Windows.MessageBox.Show($"Error al guardar la descripción:\n{ex.Message}", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Sin cambios - cerrar sin guardar
                DialogResult = false;
                Close();
            }
        }

        /// <summary>
        /// Botón Cancelar - descarta cambios y cierra la ventana.
        /// </summary>
        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            CheckAndConfirmClose();
        }

        private MarkdownHelpWindow? _helpWindow;

        /// <summary>
        /// Botón de ayuda - muestra referencia rápida de Markdown en ventana separada.
        /// </summary>
        private void btnAyuda_Click(object sender, RoutedEventArgs e)
        {
            // Cerrar ayuda anterior si existe
            _helpWindow?.Close();

            // Crear y mostrar nueva ventana de ayuda
            _helpWindow = new MarkdownHelpWindow
            {
                Owner = this
            };
            _helpWindow.Show();
        }

        /// <summary>
        /// Manejo del cierre de ventana (X button, Alt+F4, etc.).
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Cerrar ventana de ayuda si está abierta
            _helpWindow?.Close();
            
            // Si ya se guardó, no mostrar confirmación
            if (_isSaved)
            {
                base.OnClosing(e);
                return;
            }

            // Verificar si hay cambios sin guardar antes de cerrar
            if (txtEditor.Text != _originalContent)
            {
                e.Cancel = true; // Cancelar el cierre inicialmente
                CheckAndConfirmClose();
            }
            
            base.OnClosing(e);
        }

        /// <summary>
        /// Actualiza la descripción del proyecto en la base de datos.
        /// </summary>
        private void UpdateDescripcionInDatabase(string newContent)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var query = @"UPDATE Proyecto 
                          SET descripcion_larga = @descripcion, 
                              fecha_ultimo_acceso = datetime('now', 'localtime')
                          WHERE id = @id AND fecha_eliminacion IS NULL";

            using var command = new SqliteCommand(query, connection);
            command.Parameters.AddWithValue("@descripcion", newContent);
            command.Parameters.AddWithValue("@id", _proyectoId);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Verifica si hay cambios sin guardar y pide confirmación.
        /// </summary>
        private void CheckAndConfirmClose()
        {
            if (txtEditor.Text != _originalContent)
            {
                var result = System.Windows.MessageBox.Show(
                    "Tienes cambios sin guardar. ¿Estás seguro de que deseas descartarlos?",
                    "Confirmar descartar cambios",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DialogResult = false;
                    Close();
                }
                // Si el usuario selecciona "No", la ventana permanece abierta
            }
            else
            {
                DialogResult = false;
                Close();
            }
        }
    }
}