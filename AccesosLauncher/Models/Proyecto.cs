using System;
using System.ComponentModel;

namespace AccesosLauncher.Models
{
    public class Proyecto : INotifyPropertyChanged
    {
        private int _id;
        private string _nombre = string.Empty;
        private string _descripcionCorta = string.Empty;
        private string _descripcionLarga = string.Empty;
        private string _activo = "S";
        private DateTime _fechaCreacion;
        private DateTime _fechaUltimoAcceso;
        private DateTime? _fechaEliminacion;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public string Nombre
        {
            get => _nombre;
            set { _nombre = value; OnPropertyChanged(nameof(Nombre)); }
        }

        public string DescripcionCorta
        {
            get => _descripcionCorta;
            set
            {
                _descripcionCorta = value;
                OnPropertyChanged(nameof(DescripcionCorta));
                OnPropertyChanged(nameof(DescripcionCortaTruncada));
            }
        }

        public string DescripcionLarga
        {
            get => _descripcionLarga;
            set { _descripcionLarga = value; OnPropertyChanged(nameof(DescripcionLarga)); }
        }

        public string Activo
        {
            get => _activo;
            set
            {
                _activo = value;
                OnPropertyChanged(nameof(Activo));
                OnPropertyChanged(nameof(ActivoIcono));
            }
        }

        public DateTime FechaCreacion
        {
            get => _fechaCreacion;
            set { _fechaCreacion = value; OnPropertyChanged(nameof(FechaCreacion)); }
        }

        public DateTime FechaUltimoAcceso
        {
            get => _fechaUltimoAcceso;
            set { _fechaUltimoAcceso = value; OnPropertyChanged(nameof(FechaUltimoAcceso)); }
        }

        public DateTime? FechaEliminacion
        {
            get => _fechaEliminacion;
            set
            {
                _fechaEliminacion = value;
                OnPropertyChanged(nameof(FechaEliminacion));
                OnPropertyChanged(nameof(EstaEliminado));
            }
        }

        public string DescripcionCortaTruncada =>
            DescripcionCorta?.Length > 50
                ? DescripcionCorta.Substring(0, 47) + "..."
                : (DescripcionCorta ?? string.Empty);

        public string ActivoIcono => Activo == "S" ? "✓" : string.Empty;

        public bool EstaEliminado => FechaEliminacion.HasValue;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
