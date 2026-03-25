using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace AccesosLauncher
{
    public class MigrationRunner
    {
        private readonly string _connectionString;
        private readonly string _dbPath;

        public MigrationRunner(string connectionString, string dbPath)
        {
            _connectionString = connectionString;
            _dbPath = dbPath;
        }

        public void RunMigrations()
        {
            CreateSchemaVersionTableIfNotExists();

            int currentVersion = GetCurrentVersion();
            var pendingMigrations = GetPendingMigrations(currentVersion);

            if (pendingMigrations.Count == 0)
                return;

            string backupPath = _dbPath + ".bak";

            try
            {
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                File.Copy(_dbPath, backupPath, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "No se pudo crear backup antes de migrar. Verifique permisos de escritura.", ex);
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var migration in pendingMigrations)
                {
                    using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = migration.Sql;
                    command.ExecuteNonQuery();

                    command.CommandText = @"
                        INSERT INTO SchemaVersion (version, descripcion, aplicada_en)
                        VALUES (@version, @descripcion, datetime('now', 'localtime'))";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@version", migration.Version);
                    command.Parameters.AddWithValue("@descripcion", migration.Descripcion);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                try
                {
                    if (File.Exists(backupPath))
                    {
                        if (File.Exists(_dbPath))
                            File.Delete(_dbPath);
                        File.Copy(backupPath, _dbPath, overwrite: true);
                    }
                }
                catch
                {
                    throw new InvalidOperationException(
                        $"Migration {pendingMigrations[0].Version} failed. Backup restored but error during restore: {ex.Message}", ex);
                }

                throw new InvalidOperationException(
                    $"Migration failed: {ex.Message}. La base ha sido restaurada desde el backup.", ex);
            }
        }

        public int GetCurrentVersion()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COALESCE(MAX(version), 0) FROM SchemaVersion;";
            var result = command.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        private void CreateSchemaVersionTableIfNotExists()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS SchemaVersion (
                    version INTEGER PRIMARY KEY,
                    descripcion TEXT NOT NULL,
                    aplicada_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                );";
            command.ExecuteNonQuery();
        }

        private List<Migration> GetPendingMigrations(int currentVersion)
        {
            var migrations = new List<Migration>
            {
                new Migration
                {
                    Version = 1,
                    Descripcion = "Create Proyecto and ProyectoAcceso tables",
                    Sql = @"
                        CREATE TABLE IF NOT EXISTS Proyecto (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            nombre TEXT NOT NULL UNIQUE,
                            descripcion_corta TEXT NOT NULL,
                            descripcion_larga TEXT,
                            activo TEXT NOT NULL DEFAULT 'S' CHECK(activo IN ('S', 'N')),
                            fecha_creacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            fecha_ultimo_acceso DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            fecha_eliminacion DATETIME
                        );

                        CREATE TABLE IF NOT EXISTS ProyectoAcceso (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            proyecto_id INTEGER NOT NULL,
                            orden INTEGER NOT NULL DEFAULT 0,
                            acceso_full_path TEXT NOT NULL,
                            acceso_nombre TEXT NOT NULL,
                            acceso_tipo TEXT NOT NULL CHECK(acceso_tipo IN ('File', 'Folder', 'Url', 'CarpetaDeTrabajo')),
                            fecha_eliminacion DATETIME,
                            FOREIGN KEY (proyecto_id) REFERENCES Proyecto(id),
                            UNIQUE(proyecto_id, acceso_full_path)
                        );

                        CREATE INDEX IF NOT EXISTS idx_proyecto_activo ON Proyecto(activo);
                        CREATE INDEX IF NOT EXISTS idx_proyecto_fecha_eliminacion ON Proyecto(fecha_eliminacion);
                        CREATE INDEX IF NOT EXISTS idx_proyecto_acceso_proyecto_id ON ProyectoAcceso(proyecto_id);
                        CREATE INDEX IF NOT EXISTS idx_proyecto_acceso_fecha_eliminacion ON ProyectoAcceso(fecha_eliminacion);"
                },
                new Migration
                {
                    Version = 2,
                    Descripcion = "Add CarpetaDeTrabajo to acceso_tipo CHECK constraint",
                    Sql = @"
                        -- Migration 2: Add 'CarpetaDeTrabajo' to acceso_tipo CHECK constraint
                        -- SQLite doesn't support ALTER TABLE for CHECK constraints, so we recreate the table
                        
                        -- Create temporary table with new constraint
                        CREATE TABLE IF NOT EXISTS ProyectoAcceso_new (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            proyecto_id INTEGER NOT NULL,
                            orden INTEGER NOT NULL DEFAULT 0,
                            acceso_full_path TEXT NOT NULL,
                            acceso_nombre TEXT NOT NULL,
                            acceso_tipo TEXT NOT NULL CHECK(acceso_tipo IN ('File', 'Folder', 'Url', 'CarpetaDeTrabajo')),
                            fecha_eliminacion DATETIME,
                            FOREIGN KEY (proyecto_id) REFERENCES Proyecto(id),
                            UNIQUE(proyecto_id, acceso_full_path)
                        );
                        
                        -- Copy data from old table to new table
                        INSERT INTO ProyectoAcceso_new (id, proyecto_id, orden, acceso_full_path, acceso_nombre, acceso_tipo, fecha_eliminacion)
                        SELECT id, proyecto_id, orden, acceso_full_path, acceso_nombre, acceso_tipo, fecha_eliminacion
                        FROM ProyectoAcceso;
                        
                        -- Drop old table
                        DROP TABLE ProyectoAcceso;
                        
                        -- Rename new table to original name
                        ALTER TABLE ProyectoAcceso_new RENAME TO ProyectoAcceso;
                        
                        -- Recreate indexes
                        CREATE INDEX IF NOT EXISTS idx_proyecto_acceso_proyecto_id ON ProyectoAcceso(proyecto_id);
                        CREATE INDEX IF NOT EXISTS idx_proyecto_acceso_fecha_eliminacion ON ProyectoAcceso(fecha_eliminacion);"
                },
                new Migration
                {
                    Version = 3,
                    Descripcion = "Update UNIQUE constraint to include acceso_tipo",
                    Sql = @"
                        -- Migration 3: Update UNIQUE constraint to include acceso_tipo
                        -- This allows same path with different types (e.g., Folder + CarpetaDeTrabajo)
                        -- SQLite doesn't support ALTER TABLE for constraints, so we recreate the table
                        
                        -- Create temporary table with new constraint (includes acceso_tipo)
                        CREATE TABLE IF NOT EXISTS ProyectoAcceso_new (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            proyecto_id INTEGER NOT NULL,
                            orden INTEGER NOT NULL DEFAULT 0,
                            acceso_full_path TEXT NOT NULL,
                            acceso_nombre TEXT NOT NULL,
                            acceso_tipo TEXT NOT NULL CHECK(acceso_tipo IN ('File', 'Folder', 'Url', 'CarpetaDeTrabajo')),
                            fecha_eliminacion DATETIME,
                            FOREIGN KEY (proyecto_id) REFERENCES Proyecto(id),
                            UNIQUE(proyecto_id, acceso_full_path, acceso_tipo)
                        );
                        
                        -- Copy data from old table to new table
                        INSERT INTO ProyectoAcceso_new (id, proyecto_id, orden, acceso_full_path, acceso_nombre, acceso_tipo, fecha_eliminacion)
                        SELECT id, proyecto_id, orden, acceso_full_path, acceso_nombre, acceso_tipo, fecha_eliminacion
                        FROM ProyectoAcceso;
                        
                        -- Drop old table
                        DROP TABLE ProyectoAcceso;
                        
                        -- Rename new table to original name
                        ALTER TABLE ProyectoAcceso_new RENAME TO ProyectoAcceso;
                        
                        -- Recreate indexes
                        CREATE INDEX IF NOT EXISTS idx_proyecto_acceso_proyecto_id ON ProyectoAcceso(proyecto_id);
                        CREATE INDEX IF NOT EXISTS idx_proyecto_acceso_fecha_eliminacion ON ProyectoAcceso(fecha_eliminacion);"
                }
            };

            var pending = new List<Migration>();
            foreach (var m in migrations)
            {
                if (m.Version > currentVersion)
                    pending.Add(m);
            }

            return pending;
        }

        private class Migration
        {
            public int Version { get; set; }
            public string Descripcion { get; set; } = string.Empty;
            public string Sql { get; set; } = string.Empty;
        }
    }
}
