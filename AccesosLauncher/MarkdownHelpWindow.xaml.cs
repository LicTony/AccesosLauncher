using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace AccesosLauncher
{
    public partial class MarkdownHelpWindow : Window
    {
        /// <summary>
        /// Evento que se dispara al hacer doble-click en una fila del DataGrid.
        /// Envía (sintaxisInicio, sintaxisFin) para envolver el texto seleccionado.
        /// </summary>
        public event Action<string, string>? SyntaxSelected;

        public MarkdownHelpWindow()
        {
            InitializeComponent();
            dgHelp.ItemsSource = GetMarkdownHelpItems();
        }

        private static List<MarkdownHelpItem> GetMarkdownHelpItems()
        {
            return new List<MarkdownHelpItem>
            {
                new("# Título",       "Encabezado nivel 1",  "# Mi Título",         "# ",       ""),
                new("## Título",      "Encabezado nivel 2",  "## Sección",           "## ",      ""),
                new("### Título",     "Encabezado nivel 3",  "### Subsección",       "### ",     ""),
                new("**texto**",      "Negrita",             "**importante**",       "**",       "**"),
                new("*texto*",        "Cursiva",             "*énfasis*",            "*",        "*"),
                new("~~texto~~",      "Tachado",             "~~eliminado~~",        "~~",       "~~"),
                new("- elemento",     "Lista con viñetas",   "- Item 1",             "- ",       ""),
                new("1. elemento",    "Lista numerada",      "1. Paso uno",          "1. ",      ""),
                new("`código`",       "Código inline",       "`variable`",           "`",        "`"),
                new("```lenguaje",    "Bloque de código",    "```csharp\ncódigo\n```","```\n",    "\n```"),
                new("| col | col |",  "Tabla",               "| A | B |\n|---|---|\n| 1 | 2 |", "| ", " |"),
                new("> texto",        "Cita / Blockquote",   "> Cita importante",    "> ",       ""),
                new("[texto](url)",   "Enlace",              "[Google](https://google.com)", "[", "](url)"),
                new("![alt](img)",    "Imagen",              "![Logo](logo.png)",    "![",       "](img)"),
                new("---",            "Línea horizontal",    "---",                  "---",      ""),
                new("***",            "Línea horizontal",    "***",                  "***",      ""),
            };
        }

        private void dgHelp_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgHelp.SelectedItem is MarkdownHelpItem item)
            {
                SyntaxSelected?.Invoke(item.SintaxisInicio, item.SintaxisFin);
            }
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// Modelo para cada fila del DataGrid de ayuda Markdown.
    /// </summary>
    internal sealed class MarkdownHelpItem
    {
        public string Sintaxis { get; }
        public string Descripcion { get; }
        public string Ejemplo { get; }
        public string SintaxisInicio { get; }
        public string SintaxisFin { get; }

        public MarkdownHelpItem(string sintaxis, string descripcion, string ejemplo,
                                string sintaxisInicio, string sintaxisFin)
        {
            Sintaxis = sintaxis;
            Descripcion = descripcion;
            Ejemplo = ejemplo;
            SintaxisInicio = sintaxisInicio;
            SintaxisFin = sintaxisFin;
        }
    }
}
