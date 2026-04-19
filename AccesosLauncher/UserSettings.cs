namespace AccesosLauncher
{
    public class CustomAccessType
    {
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class UserSettings
    {
        public TipoCarpeta SelectedTipoCarpeta { get; set; }
        public double ProyectosSplitProportion { get; set; } = 0.6;
        public double? MainWindowWidth { get; set; }
        public double? MainWindowHeight { get; set; }
        public List<CustomAccessType> CustomAccessTypes { get; set; } = new();
    }
}
