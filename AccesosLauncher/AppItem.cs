using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AccesosLauncher
{
    public class AppItem : INotifyPropertyChanged
    {
        private ImageSource? _icon;

        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string RelativeDirectory { get; set; } = "";
        public ImageSource? Icon
        {
            get => _icon;
            set
            {
                if (_icon != value)
                {
                    _icon = value;
                    OnPropertyChanged(nameof(Icon));
                }
            }
        }
        public bool IsEmptyFolder { get; set; } = false;
        public string ItemType { get; set; } = "File"; // "File", "Url", "Folder"

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
