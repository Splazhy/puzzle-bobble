using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.IO;

namespace PuzzleBobble;

public class SaveData
{
    public static string SaveFolderPath =
        Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Splazhy",
            "PuzzleBobble"
        );

    public readonly long SaveId;

    private readonly SqliteConnection db;

    public SaveData(long saveId)
    {
        Directory.CreateDirectory(SaveFolderPath);

        var connectionString = new SqliteConnectionStringBuilder()
        {
            DataSource = Path.Join(SaveFolderPath, "savedata.db"),
            ForeignKeys = true,
        }.ToString();

        db = new SqliteConnection(connectionString);

        db.Open();

        var stmt = new SqliteCommand(
            """
            PRAGMA foreign_keys = OFF
            """,
            db
        );
        stmt.ExecuteNonQuery();

        stmt = new SqliteCommand(
            """
            PRAGMA journal_mode = WAL
            """,
            db
        );
        stmt.ExecuteNonQuery();

        Migrate();

        SaveId = saveId;
        stmt = new SqliteCommand(
            """
            INSERT INTO "SaveData"
            VALUES (@saveId)
            ON CONFLICT DO NOTHING
            """,
            db
        );
        stmt.Parameters.AddWithValue("@saveId", SaveId);
        stmt.ExecuteNonQuery();

        // db.Close();
    }

    private void Migrate()
    {
        var command = db.CreateCommand();

        command.CommandText =
            """
            PRAGMA user_version
            """;

        using var versionReader = command.ExecuteReader();
        if (!versionReader.Read())
        {
            throw new Exception("Failed to read user_version");
        }
        var version = versionReader.GetInt32(0);

        if (version == 1) return;

        command.CommandText =
            """
            CREATE TABLE "SaveData" (
                "saveId" INTEGER PRIMARY KEY
            )
            """;
        command.ExecuteNonQuery();

        command.CommandText =
            """
            CREATE TABLE "Inventory" (
                "saveId" INTEGER,
                "itemId" TEXT,
                "count" INTEGER NOT NULL,
                PRIMARY KEY ("saveId", "itemId"),
                FOREIGN KEY ("saveId") REFERENCES "SaveData" ("saveId")
            )
            """;
        command.ExecuteNonQuery();

        command.CommandText =
            """
            CREATE TABLE "PlayHistory" (
                "playHistoryId" INTEGER PRIMARY KEY,
                "saveId" INTEGER NOT NULL,
                "startTime" TEXT NOT NULL,
                "duration" REAL NOT NULL,
                "status" INTEGER NOT NULL,
                "accountedFor" BOOLEAN NOT NULL,
                FOREIGN KEY ("saveId") REFERENCES "SaveData" ("saveId")
            )
            """;
        command.ExecuteNonQuery();

        command.CommandText =
            """
            CREATE TABLE "PlayHistoryDetail" (
                "playHistoryId" INTEGER,
                "stat" TEXT,
                "value" INTEGER NOT NULL,
                PRIMARY KEY ("playHistoryId", "stat"),
                FOREIGN KEY ("playHistoryId") REFERENCES "PlayHistory" ("playHistoryId")
            )
            """;
        command.ExecuteNonQuery();

        command.CommandText =
            """
            PRAGMA user_version = 1
            """;
        command.ExecuteNonQuery();

        // db.LastInsertRowId;
    }

    public void Close()
    {
        db.Close();
    }

    public SqliteTransaction BeginTransaction()
    {
        CleanupCachedStmts();
        return db.BeginTransaction();
    }

    public void CleanupCachedStmts()
    {
        _updatePlayHistoryEntryStmt?.Dispose();
        _upsertPlayHistoryDetailStmt?.Dispose(); ;
        _getPlayHistoryStmt?.Dispose();
        _addToInventoryStmt?.Dispose();
        _updatePlayHistoryEntryStmt = null;
        _upsertPlayHistoryDetailStmt = null;
        _getPlayHistoryStmt = null;
        _addToInventoryStmt = null;
    }



    public long CreateNewPlayHistoryEntry(DateTime startTime)
    {
        using var stmt = new SqliteCommand(
            """
            INSERT INTO "PlayHistory"
            (saveId, startTime, duration, status, accountedFor)
            VALUES (@saveId, @startTime, @duration, @status, @accountedFor)
            RETURNING playHistoryId
            """,
            db
        );
        stmt.Parameters.AddWithValue("@saveId", SaveId);
        stmt.Parameters.AddWithValue("@startTime", startTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        stmt.Parameters.AddWithValue("@duration", 0);
        stmt.Parameters.AddWithValue("@status", (int)GameState.Playing);
        stmt.Parameters.AddWithValue("@accountedFor", false);
        using var reader = stmt.ExecuteReader();
        reader.Read();
        return reader.GetInt64(0);
    }

    private SqliteCommand? _updatePlayHistoryEntryStmt;

    public void UpdatePlayHistoryEntry(long playHistoryId, double duration, GameState status)
    {
        _updatePlayHistoryEntryStmt ??= new SqliteCommand(
            """
            UPDATE "PlayHistory"
            SET duration = @duration, status = @status
            WHERE playHistoryId = @playHistoryId
            """,
            db
        );
        _updatePlayHistoryEntryStmt.Parameters.Clear();
        _updatePlayHistoryEntryStmt.Parameters.AddWithValue("@duration", duration);
        _updatePlayHistoryEntryStmt.Parameters.AddWithValue("@status", (int)status);
        _updatePlayHistoryEntryStmt.Parameters.AddWithValue("@playHistoryId", playHistoryId);
        _updatePlayHistoryEntryStmt.ExecuteNonQuery();
    }

    private SqliteCommand? _upsertPlayHistoryDetailStmt;
    public void AddToPlayHistoryDetail(long playHistoryId, string stat, int value)
    {
        _upsertPlayHistoryDetailStmt ??= new SqliteCommand(
            """
            INSERT INTO "PlayHistoryDetail"
            (playHistoryId, stat, value)
            VALUES (@playHistoryId, @stat, @value)
            ON CONFLICT (playHistoryId, stat) DO UPDATE SET value = value + excluded.value
            """,
            db
        );
        _upsertPlayHistoryDetailStmt.Parameters.Clear();
        _upsertPlayHistoryDetailStmt.Parameters.AddWithValue("@playHistoryId", playHistoryId);
        _upsertPlayHistoryDetailStmt.Parameters.AddWithValue("@stat", stat);
        _upsertPlayHistoryDetailStmt.Parameters.AddWithValue("@value", value);
        _upsertPlayHistoryDetailStmt.ExecuteNonQuery();
    }

    public List<long> GetUnaccountedPlayHistoryEntries()
    {
        using var stmt = new SqliteCommand(
            """
            SELECT playHistoryId
            FROM "PlayHistory"
            WHERE saveId = @saveId AND accountedFor = 0
            """,
            db
        );
        stmt.Parameters.AddWithValue("@saveId", SaveId);
        using var reader = stmt.ExecuteReader();
        var result = new List<long>();
        while (reader.Read())
        {
            result.Add(reader.GetInt64(0));
        }
        return result;
    }

    public void SetPlayHistoryAccountedFor(long playHistoryId)
    {
        using var stmt = new SqliteCommand(
            """
            UPDATE "PlayHistory"
            SET accountedFor = 1
            WHERE playHistoryId = @playHistoryId
            """,
            db
        );
        stmt.Parameters.AddWithValue("@playHistoryId", playHistoryId);
        stmt.ExecuteNonQuery();
    }

    public class PlayHistory
    {
        public long PlayHistoryId;
        public DateTime StartTime;
        public double Duration;
        public GameState Status;
        public bool AccountedFor;
    }

    private SqliteCommand? _getPlayHistoryStmt;

    public PlayHistory GetPlayHistory(long playHistoryId)
    {
        _getPlayHistoryStmt ??= new SqliteCommand(
                    """
            SELECT startTime, duration, status, accountedFor
            FROM "PlayHistory"
            WHERE playHistoryId = @playHistoryId
            """,
                    db
                );
        _getPlayHistoryStmt.Parameters.AddWithValue("@playHistoryId", playHistoryId);
        using var reader = _getPlayHistoryStmt.ExecuteReader();
        if (!reader.Read())
        {
            throw new Exception("Play history not found");
        }
        return new PlayHistory
        {
            PlayHistoryId = playHistoryId,
            StartTime = DateTime.Parse(reader.GetString(0), System.Globalization.CultureInfo.InvariantCulture),
            Duration = reader.GetDouble(1),
            Status = (GameState)reader.GetInt32(2),
            AccountedFor = reader.GetBoolean(3),
        };
    }

    public List<KeyValuePair<string, int>> GetPlayHistoryDetails(long playHistoryId)
    {
        using var stmt = new SqliteCommand(
            """
            SELECT stat, value
            FROM "PlayHistoryDetail"
            WHERE playHistoryId = @playHistoryId
            """,
            db
        );
        stmt.Parameters.AddWithValue("@playHistoryId", playHistoryId);
        using var reader = stmt.ExecuteReader();
        var result = new List<KeyValuePair<string, int>>();
        while (reader.Read())
        {
            result.Add(new KeyValuePair<string, int>(reader.GetString(0), reader.GetInt32(1)));
        }
        return result;
    }

    private SqliteCommand? _addToInventoryStmt;

    public void AddToInventory(string itemId, int count)
    {
        _addToInventoryStmt ??= new SqliteCommand(
            """
            INSERT INTO "Inventory"
            (saveId, itemId, count)
            VALUES (@saveId, @itemId, @count)
            ON CONFLICT (saveId, itemId) DO UPDATE SET count = count + excluded.count
            """,
            db
        );
        _addToInventoryStmt.Parameters.Clear();
        _addToInventoryStmt.Parameters.AddWithValue("@saveId", SaveId);
        _addToInventoryStmt.Parameters.AddWithValue("@itemId", itemId);
        _addToInventoryStmt.Parameters.AddWithValue("@count", count);
        _addToInventoryStmt.ExecuteNonQuery();
    }

    public List<KeyValuePair<string, int>> GetInventory()
    {
        using var stmt = new SqliteCommand(
            """
            SELECT itemId, count
            FROM "Inventory"
            WHERE saveId = @saveId
            """,
            db
        );
        stmt.Parameters.AddWithValue("@saveId", SaveId);
        using var reader = stmt.ExecuteReader();
        var result = new List<KeyValuePair<string, int>>();
        while (reader.Read())
        {
            result.Add(new KeyValuePair<string, int>(reader.GetString(0), reader.GetInt32(1)));
        }
        return result;
    }

}