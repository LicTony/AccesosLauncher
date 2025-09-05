
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace AccesosLauncher
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

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
    }

    public class LoggedAppItem
    {
        public string Name { get; set; }
        public DateTime LastAccessTime { get; set; }
        public int AccessCount { get; set; }
    }
}
