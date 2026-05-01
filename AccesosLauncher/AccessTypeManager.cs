using System.IO;

namespace AccesosLauncher
{
    public static class AccessTypeManager
    {
        public const string TypeLaboral = "Laboral";
        public const string TypePersonal = "Personal";
        public const string TypeAmbos = "Ambos";

        public static readonly string[] BaseTypes = [TypePersonal, TypeLaboral, TypeAmbos];

        private static List<CustomAccessType> _customTypes = [];

        public static void Initialize(IEnumerable<CustomAccessType>? customTypes)
        {
            _customTypes = customTypes?.OrderBy(t => t.Order).ToList() ?? [];
        }

        public static List<string> GetAllTypes()
        {
            var allTypes = new List<string>(BaseTypes);
            foreach (var custom in _customTypes.OrderBy(t => t.Order))
            {
                if (!allTypes.Contains(custom.Name, StringComparer.OrdinalIgnoreCase))
                {
                    allTypes.Add(custom.Name);
                }
            }
            return allTypes;
        }

        public static List<CustomAccessType> GetCustomTypesOrdered()
        {
            return [.. _customTypes.OrderBy(t => t.Order)];
        }

        public static int GetMaxOrder()
        {
            return _customTypes.Count != 0 ? _customTypes.Max(t => t.Order) : 0;
        }

        public static bool AddType(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (IsBaseType(name)) return false;
            if (_customTypes.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return false;

            var newOrder = GetMaxOrder() + 1;
            _customTypes.Add(new CustomAccessType { Name = name, Order = newOrder });
            return true;
        }

        public static bool RemoveType(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (IsBaseType(name)) return false;

            var removed = _customTypes.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (removed == null) return false;

            var removedOrder = removed.Order;
            _customTypes.Remove(removed);

            // Reorder remaining types
            foreach (var type in _customTypes.Where(t => t.Order > removedOrder))
            {
                type.Order--;
            }

            return true;
        }

        public static bool UpdateType(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return false;
            if (IsBaseType(oldName) || IsBaseType(newName)) return false;
            if (_customTypes.Any(t => t.Name.Equals(newName, StringComparison.OrdinalIgnoreCase))) return false;

            var existing = _customTypes.FirstOrDefault(t => t.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase));
            if (existing == null) return false;

            existing.Name = newName;
            return true;
        }

        public static bool IsBaseType(string name)
        {
            return BaseTypes.Any(bt => bt.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsCustomType(string name)
        {
            return _customTypes.Any(ct => ct.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static IReadOnlyList<string> GetCustomTypes() => _customTypes.Select(t => t.Name).ToList().AsReadOnly();

        public static string NormalizeForMarker(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return string.Empty;
            // Replace spaces and special chars with underscores
            var normalized = typeName.Trim();
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", "_");
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-zA-Z0-9_]", "");
            return normalized.ToLowerInvariant();
        }

        public static string GetMarkerForType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return string.Empty;

            // Base types use legacy markers
            if (typeName.Equals(TypePersonal, StringComparison.OrdinalIgnoreCase))
                return ".personal";
            if (typeName.Equals(TypeLaboral, StringComparison.OrdinalIgnoreCase))
                return ".laboral";
            if (typeName.Equals(TypeAmbos, StringComparison.OrdinalIgnoreCase))
                return ".mixta";

            // Custom types use .tipo_Nombre format
            return ".tipo_" + NormalizeForMarker(typeName);
        }

        public static void SetTypeForFolder(string folderPath, string typeName)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            // Clean up all existing marker files
            CleanupFolderMarkers(folderPath);

            if (string.IsNullOrWhiteSpace(typeName)) return;

            var marker = GetMarkerForType(typeName);
            if (string.IsNullOrEmpty(marker)) return;

            var markerPath = Path.Combine(folderPath, marker);
            try
            {
                File.Create(markerPath).Close();
            }
            catch
            {
                // Silently fail - don't crash the app
            }
        }

        public static void CleanupFolderMarkers(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return;

            try
            {
                // Remove legacy markers
                var personalPath = Path.Combine(folderPath, ".personal");
                var mixtaPath = Path.Combine(folderPath, ".mixta");
                var laboralPath = Path.Combine(folderPath, ".laboral");

                if (File.Exists(personalPath)) File.Delete(personalPath);
                if (File.Exists(mixtaPath)) File.Delete(mixtaPath);
                if (File.Exists(laboralPath)) File.Delete(laboralPath);

                // Remove custom type markers (.tipo_*)
                var files = Directory.GetFiles(folderPath, ".tipo_*");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Silently fail
            }
        }

        public static string? GetTypeFromFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return null;

            // Check custom type markers FIRST (before legacy markers)
            var tipoFiles = Directory.GetFiles(folderPath, ".tipo_*");
            foreach (var file in tipoFiles)
            {
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith(".tipo_", StringComparison.OrdinalIgnoreCase))
                {
                    var typeName = fileName[6..]; // Remove ".tipo_" prefix
                    // Validate this is a known custom type
                    if (IsCustomType(typeName))
                    {
                        return typeName;
                    }
                }
            }

            // Check legacy markers only if no custom marker found
            var personalPath = Path.Combine(folderPath, ".personal");
            var mixtaPath = Path.Combine(folderPath, ".mixta");
            var laboralPath = Path.Combine(folderPath, ".laboral");

            if (File.Exists(mixtaPath)) return TypeAmbos;
            if (File.Exists(personalPath)) return TypePersonal;
            if (File.Exists(laboralPath)) return TypeLaboral;

            // Default to Laboral (no markers = laboral)
            return TypeLaboral;
        }

        public static TipoCarpeta GetTipoCarpetaFromString(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return TipoCarpeta.Laboral;

            return typeName.ToLowerInvariant() switch
            {
                "personal" => TipoCarpeta.Personal,
                "laboral" => TipoCarpeta.Laboral,
                "ambos" => TipoCarpeta.Ambos,
                _ => TipoCarpeta.Laboral
            };
        }
    }
}