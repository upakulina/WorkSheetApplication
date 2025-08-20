using Npgsql;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkSheetApplication.Database
{

    public static class DatabaseConnection
    {
        private static string GetConnectionString()
        {
            return new NpgsqlConnectionStringBuilder
            {
                Host = "localhost",
                Port = 5432,
                Database = "System",
                Username = "postgres",
                Password = "123456",
                // Дополнительные параметры безопасности
                SslMode = SslMode.Prefer,
                TrustServerCertificate = true,
                // Таймауты
                Timeout = 30,
                CommandTimeout = 30
            }.ToString();
        }

        public static NpgsqlConnection GetConnection()
        {
            try
            {
                var connection = new NpgsqlConnection(GetConnectionString());
                return connection;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка создания подключения к базе данных: {ex.Message}");
            }
        }

        public static bool TestConnection()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    connection.Open();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
