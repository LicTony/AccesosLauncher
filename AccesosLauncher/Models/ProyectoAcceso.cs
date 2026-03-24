using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace AccesosLauncher.Models
{
    public class ProyectoAcceso : INotifyPropertyChanged
    {
        private int _id;
        private int _proyectoId;
        private int _orden;
        private string _accesoFullPath = string.Empty;
        private string _accesoNombre = string.Empty;
        private string _accesoTipo = string.Empty;
        private ImageSource? _icon;
        private DateTime? _fechaEliminacion;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        public int ProyectoId
        {
            get => _proyectoId;
            set { _proyectoId = value; OnPropertyChanged(nameof(ProyectoId)); }
        }

        public int Orden
        {
            get => _orden;
            set { _orden = value; OnPropertyChanged(nameof(Orden)); }
        }

        public string AccesoFullPath
        {
            get => _accesoFullPath;
            set
            {
                _accesoFullPath = value;
                OnPropertyChanged(nameof(AccesoFullPath));
                OnPropertyChanged(nameof(PathExiste));
            }
        }

        public string AccesoNombre
        {
            get => _accesoNombre;
            set { _accesoNombre = value; OnPropertyChanged(nameof(AccesoNombre)); }
        }

        public string AccesoTipo
        {
            get => _accesoTipo;
            set
            {
                _accesoTipo = value;
                OnPropertyChanged(nameof(AccesoTipo));
                OnPropertyChanged(nameof(PathExiste));
            }
        }

        public ImageSource? Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged(nameof(Icon));
            }
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

        public bool PathExiste
        {
            get
            {
                if (AccesoTipo == "Url" || AccesoTipo == Enums.ProyectoAccesoTipo.Url.ToString())
                    return true;

                if (File.Exists(AccesoFullPath))
                    return true;

                if (Directory.Exists(AccesoFullPath))
                    return true;

                return false;
            }
        }

        public bool EstaEliminado => FechaEliminacion.HasValue;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
