using System.Data;
using Microsoft.Data.Sqlite;

namespace LeadPilot.Data;

/// <summary>Connection factory and small parameterised-SQL helpers. All values are bound as parameters.</summary>
public sealed class SqliteDb
{
    private readonly string _connectionString;

    /// <summary>Path of the database file.</summary>
    public string Path { get; }

    /// <summary>Creates a handle for the database file (created on first use).</summary>
    public SqliteDb(string path)
    {
        Path = path;
        var dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = true }.ToString();
    }

    /// <summary>Opens a connection with foreign keys enforced and a busy timeout.</summary>
    public SqliteConnection Open()
    {
        var c = new SqliteConnection(_connectionString);
        c.Open();
        Exec(c, "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;");
        return c;
    }

    /// <summary>Releases pooled file handles (needed before replacing the file on restore).</summary>
    public static void ReleaseFiles() => SqliteConnection.ClearAllPools();

    /// <summary>Runs a non-query command.</summary>
    public static int Exec(SqliteConnection c, string sql, params (string Name, object? Value)[] p) => Exec(c, null, sql, p);

    /// <summary>Runs a non-query command inside a transaction.</summary>
    public static int Exec(SqliteConnection c, SqliteTransaction? tx, string sql, params (string Name, object? Value)[] p)
    {
        using var cmd = Make(c, tx, sql, p);
        return cmd.ExecuteNonQuery();
    }

    /// <summary>Returns the first column of the first row, converted to <typeparamref name="T"/>.</summary>
    public static T? Scalar<T>(SqliteConnection c, SqliteTransaction? tx, string sql, params (string Name, object? Value)[] p)
    {
        using var cmd = Make(c, tx, sql, p);
        var o = cmd.ExecuteScalar();
        if (o == null || o is DBNull) return default;
        var t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(o, t, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Inserts and returns the new rowid.</summary>
    public static int Insert(SqliteConnection c, SqliteTransaction? tx, string sql, params (string Name, object? Value)[] p)
    {
        Exec(c, tx, sql, p);
        return (int)Scalar<long>(c, tx, "SELECT last_insert_rowid()");
    }

    /// <summary>Runs a query and maps each row.</summary>
    public static List<T> Query<T>(SqliteConnection c, string sql, Func<SqliteDataReader, T> map, params (string Name, object? Value)[] p) => QueryTx(c, null, sql, map, p);

    /// <summary>Runs a query inside a transaction and maps each row.</summary>
    public static List<T> QueryTx<T>(SqliteConnection c, SqliteTransaction? tx, string sql, Func<SqliteDataReader, T> map, params (string Name, object? Value)[] p)
    {
        using var cmd = Make(c, tx, sql, p);
        using var r = cmd.ExecuteReader();
        var list = new List<T>();
        while (r.Read()) list.Add(map(r));
        return list;
    }

    private static SqliteCommand Make(SqliteConnection c, SqliteTransaction? tx, string sql, (string Name, object? Value)[] p)
    {
        var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        foreach (var (name, value) in p) cmd.Parameters.AddWithValue(name, ToDb(value));
        return cmd;
    }

    /// <summary>Converts CLR values to SQLite-storable values (UTC ISO-8601 text for dates, ints for enums/bools).</summary>
    public static object ToDb(object? v) => v switch
    {
        null => DBNull.Value,
        DateTime d => d.ToUniversalTime().ToString("o", System.Globalization.CultureInfo.InvariantCulture),
        bool b => b ? 1 : 0,
        Enum e => Convert.ToInt32(e, System.Globalization.CultureInfo.InvariantCulture),
        _ => v
    };

    /// <summary>Applies pending migrations (tracked via PRAGMA user_version), each in its own transaction.</summary>
    public void Migrate()
    {
        using var c = Open();
        Exec(c, "PRAGMA journal_mode=WAL;");
        var current = Scalar<long>(c, null, "PRAGMA user_version");
        foreach (var (version, sql) in Migrations.All.Where(m => m.Version > current).OrderBy(m => m.Version))
        {
            using var tx = c.BeginTransaction();
            Exec(c, tx, sql);
            Exec(c, tx, $"PRAGMA user_version = {version}");
            tx.Commit();
        }
    }

    /// <summary>Current schema version.</summary>
    public int SchemaVersion() { using var c = Open(); return (int)Scalar<long>(c, null, "PRAGMA user_version"); }

    /// <summary>Writes a consistent copy of the database to <paramref name="destination"/> using SQLite's online backup API.</summary>
    public void BackupTo(string destination)
    {
        if (File.Exists(destination)) File.Delete(destination);
        using var src = Open();
        using var dst = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destination, Pooling = false }.ToString());
        dst.Open();
        src.BackupDatabase(dst);
    }
}

/// <summary>Null-safe typed column readers.</summary>
internal static class ReaderExt
{
    public static string S(this IDataRecord r, int i) => r.IsDBNull(i) ? "" : r.GetString(i);
    public static string? NS(this IDataRecord r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
    public static int I(this IDataRecord r, int i) => r.IsDBNull(i) ? 0 : (int)r.GetInt64(i);
    public static int? NI(this IDataRecord r, int i) => r.IsDBNull(i) ? null : (int)r.GetInt64(i);
    public static double D(this IDataRecord r, int i) => r.IsDBNull(i) ? 0 : r.GetDouble(i);
    public static double? ND(this IDataRecord r, int i) => r.IsDBNull(i) ? null : r.GetDouble(i);
    public static bool B(this IDataRecord r, int i) => !r.IsDBNull(i) && r.GetInt64(i) != 0;
    public static DateTime Dt(this IDataRecord r, int i) => r.IsDBNull(i) ? DateTime.MinValue : DateTime.Parse(r.GetString(i), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime();
    public static DateTime? NDt(this IDataRecord r, int i) => r.IsDBNull(i) ? null : r.Dt(i);
    public static T E<T>(this IDataRecord r, int i) where T : struct, Enum => (T)Enum.ToObject(typeof(T), r.IsDBNull(i) ? 0 : r.GetInt64(i));
}
