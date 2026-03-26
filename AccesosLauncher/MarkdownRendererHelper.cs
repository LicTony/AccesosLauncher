using System;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;

namespace AccesosLauncher
{
    /// <summary>
    /// Helper estático para renderizar Markdown a HTML y gestionar plantillas.
    /// </summary>
    public static class MarkdownRendererHelper
    {
        private static readonly MarkdownPipeline _pipeline;

        static MarkdownRendererHelper()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
        }

        /// <summary>
        /// Convierte Markdown a HTML usando Markdig con sintaxis highlighting.
        /// </summary>
        /// <param name="markdown">Contenido en formato Markdown</param>
        /// <returns>HTML renderizado</returns>
        public static string ToHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            return Markdown.ToHtml(markdown, _pipeline);
        }

        /// <summary>
        /// Detecta si el contenido contiene sintaxis Markdown.
        /// </summary>
        /// <param name="content">Contenido a analizar</param>
        /// <returns>true si parece ser Markdown</returns>
        public static bool IsMarkdown(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            // Caracteres comunes de Markdown
            var markdownIndicators = new[]
            {
                "# ",       // Headers
                "## ",      // Headers level 2
                "### ",     // Headers level 3
                "**",       // Bold
                "__",       // Bold alternativo
                "*",        // Italic (no al principio de línea)
                "_",        // Italic alternativo
                "```",      // Code blocks
                "`",        // Inline code
                "- ",       // Unordered list
                "+ ",       // Unordered list alternativo
                "* ",       // Unordered list otro
                "1. ",      // Ordered list
                "| ",       // Tables
                "> ",       // Blockquotes
                "---",      // Horizontal rule
                "***",      // Horizontal rule
            };

            int matchCount = 0;
            foreach (var indicator in markdownIndicators)
            {
                if (content.Contains(indicator))
                    matchCount++;
            }

            // Consideramos Markdown si hay al menos 1 indicador
            // El texto plano también se renderiza correctamente como Markdown
            return matchCount >= 1;
        }

        /// <summary>
        /// Genera plantilla HTML completa para WebView2.
        /// </summary>
        /// <param name="content">Contenido (Markdown o texto plano)</param>
        /// <param name="isEditing">Si es modo edición, incluye textarea</param>
        /// <returns>HTML completo con estilos</returns>
        public static string GetHtmlTemplate(string content, bool isEditing)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <meta http-equiv=\"X-UA-Compatible\" content=\"IE=Edge\">");
            html.AppendLine("    <link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.2.0/github-markdown.min.css\">");
            html.AppendLine("    <link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/github.min.css\">");
            html.AppendLine("    <script src=\"https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js\"></script>");
            html.AppendLine("    <style>");
            html.AppendLine("        html, body {");
            html.AppendLine("            background-color: #0d1117;");
            html.AppendLine("            margin: 0;");
            html.AppendLine("            padding: 0;");
            html.AppendLine("        }");
            html.AppendLine("        body {");
            html.AppendLine("            box-sizing: border-box;");
            html.AppendLine("            padding: 20px;");
            html.AppendLine("            background-color: #0d1117;");
            html.AppendLine("            color: #c9d1d9;");
            html.AppendLine("            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif;");
            html.AppendLine("            font-size: 14px;");
            html.AppendLine("            line-height: 1.5;");
            html.AppendLine("            min-height: 100vh;");
            html.AppendLine("        }");
            html.AppendLine("        .markdown-body {");
            html.AppendLine("            background-color: #0d1117;");
            html.AppendLine("            color: #c9d1d9;");
            html.AppendLine("        }");
            html.AppendLine("        .markdown-body > *:first-child {");
            html.AppendLine("            margin-top: 0;");
            html.AppendLine("        }");
            html.AppendLine("        #editor {");
            html.AppendLine("            width: 100%;");
            html.AppendLine("            height: 100%;");
            html.AppendLine("            background-color: #0d1117;");
            html.AppendLine("            color: #c9d1d9;");
            html.AppendLine("            border: 1px solid #30363d;");
            html.AppendLine("            border-radius: 6px;");
            html.AppendLine("            padding: 10px;");
            html.AppendLine("            font-family: 'Cascadia Code', 'Fira Code', Consolas, monospace;");
            html.AppendLine("            font-size: 14px;");
            html.AppendLine("            line-height: 1.5;");
            html.AppendLine("            resize: none;");
            html.AppendLine("            outline: none;");
            html.AppendLine("        }");
            html.AppendLine("        #editor:focus {");
            html.AppendLine("            border-color: #58a6ff;");
            html.AppendLine("        }");
            html.AppendLine("        code {");
            html.AppendLine("            background-color: rgba(110,118,129,0.4);");
            html.AppendLine("            padding: 0.2em 0.4em;");
            html.AppendLine("            border-radius: 6px;");
            html.AppendLine("            font-family: 'Cascadia Code', 'Fira Code', Consolas, monospace;");
            html.AppendLine("            font-size: 85%;");
            html.AppendLine("        }");
            html.AppendLine("        pre {");
            html.AppendLine("            background-color: #161b22;");
            html.AppendLine("            border-radius: 6px;");
            html.AppendLine("            padding: 16px;");
            html.AppendLine("            overflow: auto;");
            html.AppendLine("        }");
            html.AppendLine("        pre code {");
            html.AppendLine("            background-color: transparent;");
            html.AppendLine("            padding: 0;");
            html.AppendLine("        }");
            html.AppendLine("        table {");
            html.AppendLine("            border-collapse: collapse;");
            html.AppendLine("            width: 100%;");
            html.AppendLine("        }");
            html.AppendLine("        th, td {");
            html.AppendLine("            border: 1px solid #30363d;");
            html.AppendLine("            padding: 8px 12px;");
            html.AppendLine("        }");
            html.AppendLine("        th {");
            html.AppendLine("            background-color: #161b22;");
            html.AppendLine("        }");
            html.AppendLine("        blockquote {");
            html.AppendLine("            border-left: 4px solid #30363d;");
            html.AppendLine("            padding-left: 16px;");
            html.AppendLine("            color: #8b949e;");
            html.AppendLine("        }");
            html.AppendLine("        a {");
            html.AppendLine("            color: #58a6ff;");
            html.AppendLine("        }");
            html.AppendLine("        h1, h2, h3, h4, h5, h6 {");
            html.AppendLine("            color: #c9d1d9;");
            html.AppendLine("            border-bottom: 1px solid #30363d;");
            html.AppendLine("            padding-bottom: 0.3em;");
            html.AppendLine("        }");
            html.AppendLine("        .help-panel {");
            html.AppendLine("            background-color: #161b22;");
            html.AppendLine("            border: 1px solid #30363d;");
            html.AppendLine("            border-radius: 6px;");
            html.AppendLine("            padding: 15px;");
            html.AppendLine("            margin-bottom: 15px;");
            html.AppendLine("        }");
            html.AppendLine("        .help-panel h3 {");
            html.AppendLine("            margin-top: 0;");
            html.AppendLine("            border-bottom: none;");
            html.AppendLine("        }");
            html.AppendLine("        .help-row {");
            html.AppendLine("            display: flex;");
            html.AppendLine("            margin-bottom: 8px;");
            html.AppendLine("        }");
            html.AppendLine("        .help-syntax {");
            html.AppendLine("            color: #79c0ff;");
            html.AppendLine("            font-family: monospace;");
            html.AppendLine("            width: 150px;");
            html.AppendLine("        }");
            html.AppendLine("        .help-example {");
            html.AppendLine("            color: #8b949e;");
            html.AppendLine("        }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            if (isEditing)
            {
                html.AppendLine("    <div class=\"editor-container\">");
                html.AppendLine("        <div class=\"help-panel\">");
                html.AppendLine("            <h3>Ayuda de Markdown</h3>");
                html.AppendLine("            <div class=\"help-row\"><span class=\"help-syntax\"># Título</span><span class=\"help-example\">Encabezado nivel 1</span></div>");
                html.AppendLine("            <div class=\"help-row\"><span class=\"help-syntax\">## Título</span><span class=\"help-example\">Encabezado nivel 2</span></div>");
                html.AppendLine("            <div class=\"help-row\"><span class=\"help-syntax\">**texto**</span><span class=\"help-example\">Negrita</span></div>");
                html.AppendLine("            <div class=\"help-row\"><span class=\"help-syntax\">*texto*</span><span class=\"help-example\">Cursiva</span></div>");
                html.AppendLine("            <div class=\"help-row\"><span class=\"help-syntax\">- item</span><span class=\"help-example\">Lista con viñetas</span></div>");
                html.AppendLine("            <div class=\"help-row\"><span class=\"help-syntax\">1. item</span><span class=\"help-example\">Lista numerada</span></div>");
                html.AppendLine("            <div class=\"help-row\"><span class=\"help-syntax\">`código`</span><span class=\"help-example\">Código inline</span></div>");
                html.AppendLine("            <div class=\"help-row\"><span class=\"help-syntax\">```leng</span><span class=\"help-example\">Bloque de código</span></div>");
                html.AppendLine("            <div class=\"help-row\"><span class=\"help-syntax\">| col | col |</span><span class=\"help-example\">Tabla</span></div>");
                html.AppendLine("            <div class=\"help-row\"><span class=\"help-syntax\">&gt; cita</span><span class=\"help-example\">Cita</span></div>");
                html.AppendLine("            <div class=\"help-row\"><span class=\"help-syntax\">[text](url)</span><span class=\"help-example\">Enlace</span></div>");
                html.AppendLine("        </div>");
                html.AppendLine($"        <textarea id=\"editor\" placeholder=\"Escribe en Markdown...\">{EscapeHtml(content)}</textarea>");
                html.AppendLine("    </div>");
            }
            else
            {
                // Modo preview
                if (string.IsNullOrEmpty(content))
                {
                    content = "(Sin descripción)";
                }

                // Detectar si es Markdown o texto plano
                if (IsMarkdown(content))
                {
                    var renderedHtml = ToHtml(content);
                    html.AppendLine($"    <div class=\"markdown-body\">{renderedHtml}</div>");
                }
                else
                {
                    // Texto plano - convertir saltos de línea a <br>
                    var escapedHtml = EscapeHtml(content);
                    var plainHtml = escapedHtml.Replace("\n", "<br>");
                    html.AppendLine($"    <div class=\"markdown-body\"><p>{plainHtml}</p></div>");
                }
            }

            html.AppendLine("    <script>hljs.highlightAll();</script>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        /// <summary>
        /// Escapa caracteres HTML especiales.
        /// </summary>
        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        /// <summary>
        /// Mensaje de fallback cuando WebView2 no está disponible.
        /// </summary>
        public static string GetFallbackMessage()
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <style>");
            html.AppendLine("        body {");
            html.AppendLine("            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif;");
            html.AppendLine("            padding: 20px;");
            html.AppendLine("            background-color: #f6f8fa;");
            html.AppendLine("            color: #24292f;");
            html.AppendLine("        }");
            html.AppendLine("        .error {");
            html.AppendLine("            border: 1px solid #d73a49;");
            html.AppendLine("            border-radius: 6px;");
            html.AppendLine("            padding: 16px;");
            html.AppendLine("            background-color: #ffebe9;");
            html.AppendLine("        }");
            html.AppendLine("        h2 { color: #d73a49; }");
            html.AppendLine("        a { color: #0969da; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <div class=\"error\">");
            html.AppendLine("        <h2>WebView2 no disponible</h2>");
            html.AppendLine("        <p>Para visualizar descripciones con formato Markdown, se requiere Microsoft Edge WebView2 Runtime.</p>");
            html.AppendLine("        <p>Por favor, descarga e instala WebView2 desde:</p>");
            html.AppendLine("        <p><a href=\"https://developer.microsoft.com/microsoft-edge/webview2/\" target=\"_blank\">https://developer.microsoft.com/microsoft-edge/webview2/</a></p>");
            html.AppendLine("    </div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return html.ToString();
        }
    }
}
