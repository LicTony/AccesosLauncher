
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace AccesosLauncher
{
    public class DatabaseHelper(string connectionString)
    {
        private readonly string _connectionString = connectionString;

        public void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS access_log (
                    id INTEGER PRIMARY KEY,
                    full_path TEXT NOT NULL UNIQUE,
                    name TEXT NOT NULL,
                    last_access_time DATETIME NOT NULL,
                    access_count INTEGER NOT NULL DEFAULT 1
                );";
            command.ExecuteNonQuery();

            var dbPath = connectionString.Replace("Data Source=", "").Trim();
            var migrationRunner = new MigrationRunner(_connectionString, dbPath);
            migrationRunner.RunMigrations();
        }

        public void LogAccess(string fullPath)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO access_log (full_path, name, last_access_time, access_count)
                VALUES (@full_path, @name, datetime('now', 'localtime'), 1)
                ON CONFLICT(full_path) DO UPDATE SET
                last_access_time = datetime('now', 'localtime'),
                access_count = access_count + 1;";
            command.Parameters.AddWithValue("@full_path", fullPath);
            command.Parameters.AddWithValue("@name", Path.GetFileNameWithoutExtension(fullPath));
            command.ExecuteNonQuery();
        }

        public List<LoggedAppItem> GetTopUsedItems(int limit)
        {
            var items = new List<LoggedAppItem>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT name, last_access_time, access_count
                FROM access_log
                ORDER BY access_count DESC
                LIMIT @limit;";
            command.Parameters.AddWithValue("@limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new LoggedAppItem
                {
                    Name = reader.GetString(0),
                    LastAccessTime = reader.GetDateTime(1),
                    AccessCount = reader.GetInt32(2)
                });
            }
            return items;
        }

        public void ClearLog()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM access_log;";
            command.ExecuteNonQuery();
        }

        public List<Models.Proyecto> GetProyectos(bool filterActive, string? searchTerms)
        {
            var proyectos = new List<Models.Proyecto>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var whereClauses = new List<string> { "fecha_eliminacion IS NULL" };

            if (filterActive)
            {
                whereClauses.Add("activo = 'S'");
            }

            if (!string.IsNullOrWhiteSpace(searchTerms))
            {
                var terms = searchTerms.Split('%', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in terms)
                {
                    whereClauses.Add("(nombre LIKE @term OR descripcion_corta LIKE @term)");
                }
            }

            var whereClause = string.Join(" AND ", whereClauses);

            var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT id, nombre, descripcion_corta, descripcion_larga, activo,
                       fecha_creacion, fecha_ultimo_acceso, fecha_eliminacion
                FROM Proyecto
                WHERE {whereClause}
                ORDER BY fecha_ultimo_acceso DESC;";

            if (!string.IsNullOrWhiteSpace(searchTerms))
            {
                var terms = searchTerms.Split('%', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in terms)
                {
                    command.Parameters.AddWithValue("@term", $"%{term}%");
                }
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                proyectos.Add(new Models.Proyecto
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    DescripcionCorta = reader.GetString(2),
                    DescripcionLarga = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Activo = reader.GetString(4),
                    FechaCreacion = reader.GetDateTime(5),
                    FechaUltimoAcceso = reader.GetDateTime(6),
                    FechaEliminacion = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                });
            }
            return proyectos;
        }

        public int CreateProyecto(string nombre, string descripcionCorta, string? descripcionLarga)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Proyecto (nombre, descripcion_corta, descripcion_larga)
                VALUES (@nombre, @descripcion_corta, @descripcion_larga);
                SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@nombre", nombre);
            command.Parameters.AddWithValue("@descripcion_corta", descripcionCorta);
            command.Parameters.AddWithValue("@descripcion_larga", descripcionLarga ?? (object)DBNull.Value);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public void UpdateProyecto(int id, string nombre, string descripcionCorta, string? descripcionLarga)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Proyecto
                SET nombre = @nombre,
                    descripcion_corta = @descripcion_corta,
                    descripcion_larga = @descripcion_larga
                WHERE id = @id AND fecha_eliminacion IS NULL;";
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@nombre", nombre);
            command.Parameters.AddWithValue("@descripcion_corta", descripcionCorta);
            command.Parameters.AddWithValue("@descripcion_larga", descripcionLarga ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }

        public void DeleteProyecto(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                var cmdProyecto = connection.CreateCommand();
                cmdProyecto.Transaction = transaction;
                cmdProyecto.CommandText = @"
                    UPDATE Proyecto
                    SET fecha_eliminacion = datetime('now', 'localtime')
                    WHERE id = @id AND fecha_eliminacion IS NULL;";
                cmdProyecto.Parameters.AddWithValue("@id", id);
                cmdProyecto.ExecuteNonQuery();

                var cmdAccesos = connection.CreateCommand();
                cmdAccesos.Transaction = transaction;
                cmdAccesos.CommandText = @"
                    UPDATE ProyectoAcceso
                    SET fecha_eliminacion = datetime('now', 'localtime')
                    WHERE proyecto_id = @proyecto_id AND fecha_eliminacion IS NULL;";
                cmdAccesos.Parameters.AddWithValue("@proyecto_id", id);
                cmdAccesos.ExecuteNonQuery();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void ActivateProyecto(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Proyecto SET activo = 'S' WHERE id = @id AND fecha_eliminacion IS NULL;";
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        public void DeactivateProyecto(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Proyecto SET activo = 'N' WHERE id = @id AND fecha_eliminacion IS NULL;";
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        public List<Models.ProyectoAcceso> GetProyectoAccesos(int proyectoId)
        {
            var accesos = new List<Models.ProyectoAcceso>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT id, proyecto_id, orden, acceso_full_path, acceso_nombre, acceso_tipo, fecha_eliminacion
                FROM ProyectoAcceso
                WHERE proyecto_id = @proyecto_id AND fecha_eliminacion IS NULL
                ORDER BY orden ASC;";
            command.Parameters.AddWithValue("@proyecto_id", proyectoId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                accesos.Add(new Models.ProyectoAcceso
                {
                    Id = reader.GetInt32(0),
                    ProyectoId = reader.GetInt32(1),
                    Orden = reader.GetInt32(2),
                    AccesoFullPath = reader.GetString(3),
                    AccesoNombre = reader.GetString(4),
                    AccesoTipo = reader.GetString(5),
                    FechaEliminacion = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
                });
            }
            return accesos;
        }

        public int AddProyectoAcceso(int proyectoId, string accesoFullPath, string accesoNombre, string accesoTipo)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var maxOrdenCmd = connection.CreateCommand();
            maxOrdenCmd.CommandText = "SELECT COALESCE(MAX(orden), -1) + 1 FROM ProyectoAcceso WHERE proyecto_id = @proyecto_id AND fecha_eliminacion IS NULL;";
            maxOrdenCmd.Parameters.AddWithValue("@proyecto_id", proyectoId);
            int newOrden = Convert.ToInt32(maxOrdenCmd.ExecuteScalar());

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ProyectoAcceso (proyecto_id, orden, acceso_full_path, acceso_nombre, acceso_tipo)
                VALUES (@proyecto_id, @orden, @acceso_full_path, @acceso_nombre, @acceso_tipo);
                SELECT last_insert_rowid();";
            command.Parameters.AddWithValue("@proyecto_id", proyectoId);
            command.Parameters.AddWithValue("@orden", newOrden);
            command.Parameters.AddWithValue("@acceso_full_path", accesoFullPath);
            command.Parameters.AddWithValue("@acceso_nombre", accesoNombre);
            command.Parameters.AddWithValue("@acceso_tipo", accesoTipo);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public void UpdateProyectoAccesoOrden(List<(int Id, int NewOrden)> ordenUpdates)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var (Id, NewOrden) in ordenUpdates)
                {
                    var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = "UPDATE ProyectoAcceso SET orden = @orden WHERE id = @id AND fecha_eliminacion IS NULL;";
                    command.Parameters.AddWithValue("@id", Id);
                    command.Parameters.AddWithValue("@orden", NewOrden);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public void DeleteProyectoAcceso(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE ProyectoAcceso SET fecha_eliminacion = datetime('now', 'localtime') WHERE id = @id AND fecha_eliminacion IS NULL;";
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        public void RenameProyectoAcceso(int id, string newNombre)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE ProyectoAcceso SET acceso_nombre = @new_nombre WHERE id = @id AND fecha_eliminacion IS NULL;";
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@new_nombre", newNombre);
            command.ExecuteNonQuery();
        }

        public void UpdateProyectoLastAccess(int proyectoId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Proyecto SET fecha_ultimo_acceso = datetime('now', 'localtime') WHERE id = @id AND fecha_eliminacion IS NULL;";
            command.Parameters.AddWithValue("@id", proyectoId);
            command.ExecuteNonQuery();
        }
    }

    public class LoggedAppItem
    {
        public string Name { get; set; } = string.Empty;
        public DateTime LastAccessTime { get; set; }
        public int AccessCount { get; set; }
    }
}
