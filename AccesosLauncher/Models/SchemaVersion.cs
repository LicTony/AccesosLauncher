using System;

namespace AccesosLauncher.Models
{
    public class SchemaVersion
    {
        public int Version { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public DateTime AplicadaEn { get; set; }
    }
}
