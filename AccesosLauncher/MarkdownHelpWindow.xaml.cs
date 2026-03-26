using System.Windows;

namespace AccesosLauncher
{
    public partial class MarkdownHelpWindow : Window
    {
        public MarkdownHelpWindow()
        {
            InitializeComponent();
            
            txtHelp.Text = @"Referencia rapida de Markdown:

# Titulo           -> Encabezado nivel 1
## Titulo          -> Encabezado nivel 2
### Titulo         -> Encabezado nivel 3

**texto**          -> Negrita
*texto*            -> Cursiva
~~texto~~          -> Tachado

- elemento        -> Lista con vinetas
1. elemento       -> Lista numerada

`codigo`           -> Codigo inline
```
lenguaje          -> Bloque de codigo
codigo
```

| columna | columna | -> Tabla
|---|---|

> texto           -> Cita / Blockquote

[texto](url)      -> Enlace
![alt](imagen)    -> Imagen

---               -> Linea horizontal
***               -> Linea horizontal";
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
