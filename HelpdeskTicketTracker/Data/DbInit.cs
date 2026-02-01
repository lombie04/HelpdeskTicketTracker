using Microsoft.Data.Sqlite;
using System;
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

            using (var cmd = con.CreateCommand())
            {
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

            // Seed demo data only when there are no tickets (safe for local + useful for Render)
            using (var countCmd = con.CreateCommand())
            {
                countCmd.CommandText = "SELECT COUNT(*) FROM Tickets;";
                var ticketCount = Convert.ToInt32(countCmd.ExecuteScalar() ?? 0);

                if (ticketCount == 0)
                {
                    // Categories (INSERT OR IGNORE prevents duplicates)
                    using (var seedCategories = con.CreateCommand())
                    {
                        seedCategories.CommandText = @"
INSERT OR IGNORE INTO Categories (Name) VALUES ('General');
INSERT OR IGNORE INTO Categories (Name) VALUES ('Billing');
INSERT OR IGNORE INTO Categories (Name) VALUES ('Technical');
INSERT OR IGNORE INTO Categories (Name) VALUES ('Account');
";
                        seedCategories.ExecuteNonQuery();
                    }

                    // Get CategoryIds
                    int GetCategoryId(string name)
                    {
                        using var getCmd = con.CreateCommand();
                        getCmd.CommandText = "SELECT CategoryId FROM Categories WHERE Name = $name LIMIT 1;";
                        getCmd.Parameters.AddWithValue("$name", name);
                        return Convert.ToInt32(getCmd.ExecuteScalar());
                    }

                    var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                    var generalId = GetCategoryId("General");
                    var billingId = GetCategoryId("Billing");
                    var techId = GetCategoryId("Technical");

                    using (var seedTickets = con.CreateCommand())
                    {
                        seedTickets.CommandText = @"
INSERT INTO Tickets (Title, Description, Status, CreatedAt, CategoryId)
VALUES ($t1, $d1, 'Open', $now, $c1);

INSERT INTO Tickets (Title, Description, Status, CreatedAt, CategoryId)
VALUES ($t2, $d2, 'In Progress', $now, $c2);

INSERT INTO Tickets (Title, Description, Status, CreatedAt, CategoryId)
VALUES ($t3, $d3, 'Resolved', $now, $c3);
";
                        seedTickets.Parameters.AddWithValue("$now", now);

                        seedTickets.Parameters.AddWithValue("$t1", "Unable to log in");
                        seedTickets.Parameters.AddWithValue("$d1", "User reports login fails after password reset. Please verify account status and reset flow.");
                        seedTickets.Parameters.AddWithValue("$c1", generalId);

                        seedTickets.Parameters.AddWithValue("$t2", "Invoice mismatch for January");
                        seedTickets.Parameters.AddWithValue("$d2", "Customer claims invoice amount does not match the agreed plan. Review billing details and provide clarification.");
                        seedTickets.Parameters.AddWithValue("$c2", billingId);

                        seedTickets.Parameters.AddWithValue("$t3", "App crashes on ticket submit");
                        seedTickets.Parameters.AddWithValue("$d3", "Reported crash when submitting a ticket with long description. Reproduce and fix validation/handling.");
                        seedTickets.Parameters.AddWithValue("$c3", techId);

                        seedTickets.ExecuteNonQuery();
                    }

                    using (var seedComments = con.CreateCommand())
                    {
                        seedComments.CommandText = @"
INSERT INTO Comments (TicketId, Text, CreatedAt)
VALUES (1, 'Auto-seeded demo comment: investigate logs and confirm steps to reproduce.', $now);
";
                        seedComments.Parameters.AddWithValue("$now", now);
                        seedComments.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
