using Microsoft.Data.Sqlite;
using System.Data;

namespace HelpdeskTicketTracker.Data
{
    public class DataAccessLayer
    {
        private readonly string _dbPath;

        public DataAccessLayer(string dbPath)
        {
            _dbPath = dbPath;
        }

        private SqliteConnection CreateConnection()
        {
            var cs = new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();
            return new SqliteConnection(cs);
        }

        public int ExecuteNonQuery(string sql, params (string Name, object? Value)[] parameters)
        {
            using var con = CreateConnection();
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;

            foreach (var p in parameters)
                cmd.Parameters.AddWithValue(p.Name, p.Value ?? DBNull.Value);

            return cmd.ExecuteNonQuery();
        }

        public DataTable SelectData(string sql, params (string Name, object? Value)[] parameters)
        {
            using var con = CreateConnection();
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;

            foreach (var p in parameters)
                cmd.Parameters.AddWithValue(p.Name, p.Value ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();

            var dt = new DataTable();
            dt.Load(reader);
            return dt;
        }
    }
}

