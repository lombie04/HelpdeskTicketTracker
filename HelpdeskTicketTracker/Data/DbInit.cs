using Microsoft.Data.Sqlite;
using System.IO;

namespace HelpdeskTicketTracker.Data
{
    public static class DbInit
    {
        public static void EnsureCreated(string dbPath)
        {
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var cs = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath
            }.ToString();

            using var con = new SqliteConnection(cs);
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Categories (
    CategoryId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name       TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Tickets (
    TicketId    INTEGER PRIMARY KEY AUTOINCREMENT,
    Title       TEXT NOT NULL,
    Description TEXT NOT NULL,
    Status      TEXT NOT NULL DEFAULT 'Open',
    CreatedAt   TEXT NOT NULL,
    CategoryId  INTEGER NOT NULL,
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
);

CREATE TABLE IF NOT EXISTS Comments (
    CommentId  INTEGER PRIMARY KEY AUTOINCREMENT,
    TicketId   INTEGER NOT NULL,
    Text       TEXT NOT NULL,
    CreatedAt  TEXT NOT NULL,
    FOREIGN KEY (TicketId) REFERENCES Tickets(TicketId)
);
";
            cmd.ExecuteNonQuery();
        }
    }
}
