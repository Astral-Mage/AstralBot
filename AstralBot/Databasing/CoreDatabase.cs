using AstralBot.Bot;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace AstralBot.Databasing
{
    internal static class SqliteSchema
    {
        private const string DatabaseName = "AstralBot.db";
        private static SqliteConnection? conn;
        private static bool isInitialized = false;
        private static readonly object _lock = new();

        internal static void CreateTable<T>()
        {
            var sql = BuildCreateTableSql<T>();

            lock (_lock)
            {
                if (!isInitialized) Initialize();
                if (conn == null) throw new InvalidOperationException("DB not initialized.");

                conn.Open();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        internal static T? GetById<T>(object id) where T : new()
        {
            var type = typeof(T);
            var table = GetTableName<T>();

            var pkProp = GetMappedProperties(type)
                .FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null)
                ?? throw new InvalidOperationException($"{type.Name} has no PrimaryKey.");

            var pkCol = GetColumnName(pkProp);

            lock (_lock)
            {
                if (!isInitialized) Initialize();
                if (conn == null) throw new InvalidOperationException("SQLite connection was not initialized.");

                conn.Open();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"SELECT * FROM {Q(table)} WHERE {Q(pkCol)} = $id;";
                    cmd.Parameters.AddWithValue("$id", ToDbParam(id, pkProp.PropertyType));
                    using var reader = cmd.ExecuteReader();

                    if (reader.Read())
                        return MapRow<T>(reader);

                    return default;
                }
                finally
                {
                    conn.Close();
                }
            }
        }


        internal static List<T> GetAll<T>() where T : new()
        {
            var results = new List<T>();
            var table = GetTableName<T>();

            lock (_lock)
            {
                if (!isInitialized) Initialize();
                if (conn == null) throw new Exception();
                conn.Open();
                using var cmd = conn!.CreateCommand();
                cmd.CommandText = $"SELECT * FROM {Q(table)};";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    results.Add(MapRow<T>(reader));
                }
                conn.Close();
            }

            return results;
        }

        internal static void Insert<T>(T obj)
        {
            var type = typeof(T);
            var table = GetTableName<T>();
            var props = GetMappedProperties(type).ToList();

            var pkProp = props.FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null)
                ?? throw new InvalidOperationException($"{type.Name} has no PrimaryKey.");

            var pkAttr = pkProp.GetCustomAttribute<PrimaryKeyAttribute>()!;
            bool pkAuto = pkAttr.AutoIncrement;

            // Include everything except an autoincrement PK (SQLite must generate that)
            var insertProps = props.Where(p => !(pkAuto && p == pkProp)).ToList();

            var colNames = insertProps.Select(p => Q(GetColumnName(p))).ToList();
            var paramNames = insertProps.Select((p, i) => $"$p{i}").ToList();

            var sql = $"INSERT INTO {Q(table)} ({string.Join(", ", colNames)}) " +
                      $"VALUES ({string.Join(", ", paramNames)});";

            lock (_lock)
            {
                if (!isInitialized) Initialize();
                if (conn == null) throw new InvalidOperationException("SQLite connection was not initialized.");

                conn.Open();
                try
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = sql;

                        for (int i = 0; i < insertProps.Count; i++)
                        {
                            var prop = insertProps[i];
                            var value = prop.GetValue(obj);
                            cmd.Parameters.AddWithValue(paramNames[i], ToDbParam(value, prop.PropertyType));
                        }

                        cmd.ExecuteNonQuery();
                    }

                    // If PK is autoincrement, read it back and assign it to the object
                    if (pkAuto)
                    {
                        using var idCmd = conn.CreateCommand();
                        idCmd.CommandText = "SELECT last_insert_rowid();";
                        long newId = (long)idCmd.ExecuteScalar()!;

                        if (pkProp.PropertyType == typeof(int))
                            pkProp.SetValue(obj, checked((int)newId));
                        else if (pkProp.PropertyType == typeof(long))
                            pkProp.SetValue(obj, newId);
                        else
                            throw new InvalidOperationException(
                                $"[{type.Name}.{pkProp.Name}] AutoIncrement PK must be int or long.");
                    }
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        internal static int Update<T>(T obj)
        {
            var type = typeof(T);
            var table = GetTableName<T>();
            var props = GetMappedProperties(type).ToList();

            var pkProp = props.FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyAttribute>() != null)
                ?? throw new InvalidOperationException($"{type.Name} has no PrimaryKey.");

            var pkCol = GetColumnName(pkProp);
            var pkValue = pkProp.GetValue(obj) ?? throw new InvalidOperationException($"Primary key value is null: {type.Name}.{pkProp.Name}");

            // All non-PK columns get updated (including setting NULLs explicitly)
            var updateProps = props.Where(p => p != pkProp).ToList();

            // UPDATE "Table" SET "A"=$p0, "B"=$p1 WHERE "Pk"=$id;
            var setClauses = updateProps.Select((p, i) => $"{Q(GetColumnName(p))} = $p{i}").ToList();
            var sql = $"UPDATE {Q(table)} SET {string.Join(", ", setClauses)} WHERE {Q(pkCol)} = $id;";

            lock (_lock)
            {
                if (!isInitialized) Initialize();
                if (conn == null) throw new InvalidOperationException("SQLite connection was not initialized.");

                conn.Open();
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;

                    for (int i = 0; i < updateProps.Count; i++)
                    {
                        var prop = updateProps[i];
                        var value = prop.GetValue(obj);
                        cmd.Parameters.AddWithValue($"$p{i}", ToDbParam(value, prop.PropertyType));
                    }

                    cmd.Parameters.AddWithValue("$id", ToDbParam(pkValue, pkProp.PropertyType));

                    return cmd.ExecuteNonQuery();
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        // helpers

        private static object ToDbParam(object? value, Type propType)
        {
            if (value == null)
                return DBNull.Value;

            var t = Nullable.GetUnderlyingType(propType) ?? propType;

            if (IsJsonBackedType(t))
            {
                // Fast + compact; change options later if you want versioned JSON
                return System.Text.Json.JsonSerializer.Serialize(value);
            }

            // Existing behavior preserved
            if (t.IsEnum)
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);

            if (t == typeof(bool))
                return (bool)value ? 1 : 0;

            if (t == typeof(DateTime))
                return ((DateTime)value).ToString("O", CultureInfo.InvariantCulture);

            if (t == typeof(Guid))
                return value.ToString()!;

            return value;
        }

        static T MapRow<T>(SqliteDataReader reader) where T : new()
        {
            var obj = new T();
            var type = typeof(T);

            foreach (var prop in GetMappedProperties(type))
            {
                var colName = GetColumnName(prop);

                int ordinal;
                try
                {
                    ordinal = reader.GetOrdinal(colName);
                }
                catch (IndexOutOfRangeException)
                {
                    // Column not present (schema drift). Skip it.
                    continue;
                }

                if (reader.IsDBNull(ordinal))
                {
                    if (!prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) != null)
                        prop.SetValue(obj, null);

                    continue;
                }

                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                object value;

                if (IsJsonBackedType(targetType))
                {
                    var json = reader.GetString(ordinal);

                    // Empty string => treat as null (defensive)
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        value = null!;
                    }
                    else
                    {
                        value = System.Text.Json.JsonSerializer.Deserialize(json, targetType)
                            ?? throw new InvalidOperationException($"Failed to deserialize JSON for {type.Name}.{prop.Name}");
                    }
                }
                else if (targetType.IsEnum)
                {
                    long raw = reader.GetInt64(ordinal);
                    value = Enum.ToObject(targetType, raw);
                }
                else
                {
                    value = targetType switch
                    {
                        Type t when t == typeof(int) => reader.GetInt32(ordinal),
                        Type t when t == typeof(long) => reader.GetInt64(ordinal),
                        Type t when t == typeof(bool) => reader.GetInt32(ordinal) != 0,
                        Type t when t == typeof(float) => reader.GetFloat(ordinal),
                        Type t when t == typeof(double) => reader.GetDouble(ordinal),
                        Type t when t == typeof(decimal) => reader.GetDecimal(ordinal),
                        Type t when t == typeof(string) => reader.GetString(ordinal),
                        Type t when t == typeof(Guid) => Guid.Parse(reader.GetString(ordinal)),
                        Type t when t == typeof(DateTime)
                            => DateTime.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                        _ => throw new NotSupportedException($"Unsupported type: {targetType}")
                    };
                }

                prop.SetValue(obj, value);
            }

            return obj;
        }


        static string GetTableName<T>() => typeof(T).Name;

        static IEnumerable<PropertyInfo> GetMappedProperties(Type t)
        {
            return t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.CanWrite)
                    .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null);
        }

        static string GetColumnName(PropertyInfo p)
        {
            var name = p.GetCustomAttribute<ColumnAttribute>()?.Name;
            return string.IsNullOrWhiteSpace(name) ? p.Name : name;
        }

        private static void Initialize()
        {
            if (isInitialized)
                return;

            SQLitePCL.Batteries_V2.Init();
            string dbPath = Path.Combine(AppContext.BaseDirectory, "Data", DatabaseName);
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            conn = new SqliteConnection($"Data Source={dbPath}");
            isInitialized = true;
        }

        private static string BuildCreateTableSql<T>()
        {
            var type = typeof(T);
            var tableName = type.Name;

            var props = GetMappedProperties(type);

            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE IF NOT EXISTS {Q(tableName)} (");

            bool first = true;

            foreach (var prop in props)
            {
                var pk = prop.GetCustomAttribute<PrimaryKeyAttribute>();
                var col = prop.GetCustomAttribute<ColumnAttribute>();

                var colName = col?.Name ?? prop.Name;

                if (!first) sb.Append(", ");
                first = false;

                sb.Append(Q(colName)).Append(' ');

                if (pk != null)
                {
                    var pkType = MapPkType(prop.PropertyType, out bool isIntegerPk);
                    sb.Append(pkType).Append(" PRIMARY KEY");

                    if (pk.AutoIncrement)
                    {
                        if (!isIntegerPk)
                            throw new InvalidOperationException(
                                $"[{type.Name}.{prop.Name}] AutoIncrement requires int/long primary key in SQLite.");

                        sb.Append(" AUTOINCREMENT");
                    }

                    continue;
                }

                bool isJson = IsJsonBackedType(prop.PropertyType);
                sb.Append(isJson ? "TEXT" : MapType(prop.PropertyType));

                bool nullableDefault = IsNullableByCSharp(prop.PropertyType);
                bool nullable = col?.Nullable ?? nullableDefault;

                sb.Append(nullable ? " NULL" : " NOT NULL");

                if (col?.Unique == true)
                    sb.Append(" UNIQUE");

                if (col?.Default != null)
                {
                    sb.Append(" DEFAULT ").Append(ToSqlLiteral(col.Default));
                }
                else if (isJson && !nullable)
                {
                    // Optional but helpful: prevent NULL JSON columns if you mark them NOT NULL
                    // List/arrays => '[]', objects => '{}'
                    sb.Append(" DEFAULT ").Append(ToSqlLiteral(IsEnumerableType(prop.PropertyType) ? "[]" : "{}"));
                }
            }

            sb.Append(");");
            return sb.ToString();
        }

        private static bool IsJsonBackedType(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;

            // these are already handled by MapType
            if (t == typeof(string)) return false;
            if (IsSimpleSqlType(t)) return false;

            // Collections (List<>, arrays, etc.) -> JSON
            if (IsEnumerableType(t)) return true;

            // Any other non-simple class/struct -> JSON
            if (t.IsClass) return true;
            if (t.IsValueType && !t.IsPrimitive && !t.IsEnum) return true;

            return false;
        }

        private static bool IsEnumerableType(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            if (t == typeof(string)) return false;
            if (t == typeof(byte[])) return false;

            return typeof(System.Collections.IEnumerable).IsAssignableFrom(t);
        }

        // This should match your MapType coverage; keep it conservative.
        private static bool IsSimpleSqlType(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;

            return t.IsEnum
                || t == typeof(string)
                || t == typeof(bool)
                || t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)
                || t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort) || t == typeof(sbyte)
                || t == typeof(float) || t == typeof(double) || t == typeof(decimal)
                || t == typeof(DateTime) || t == typeof(DateTimeOffset)
                || t == typeof(Guid);
        }

        private static string MapType(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;

            if (t.IsEnum)
                return "INTEGER";

            if (t == typeof(int) || t == typeof(long) || t == typeof(bool))
                return "INTEGER";
            if (t == typeof(float) || t == typeof(double) || t == typeof(decimal))
                return "REAL";
            if (t == typeof(string) || t == typeof(DateTime) || t == typeof(Guid))
                return "TEXT";
            if (t == typeof(byte[]))
                return "BLOB";

            throw new NotSupportedException($"Unsupported type: {t.FullName}");
        }


        private static string MapPkType(Type t, out bool isIntegerPk)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;

            if (t == typeof(int) || t == typeof(long))
            {
                isIntegerPk = true;
                return "INTEGER";
            }

            // Allow TEXT primary keys (e.g., Guid/string), but no AUTOINCREMENT.
            if (t == typeof(string) || t == typeof(Guid))
            {
                isIntegerPk = false;
                return "TEXT";
            }

            throw new NotSupportedException($"Unsupported primary key type: {t.FullName}");
        }

        private static bool IsNullableByCSharp(Type t)
            => !t.IsValueType || Nullable.GetUnderlyingType(t) != null;

        private static string ToSqlLiteral(object value)
        {
            return value switch
            {
                null => "NULL",
                bool b => b ? "1" : "0",

                _ when value.GetType().IsEnum
                    => Convert.ToInt64(value, CultureInfo.InvariantCulture)
                              .ToString(CultureInfo.InvariantCulture),

                string s => $"'{s.Replace("'", "''")}'",
                Guid g => $"'{g}'",
                DateTime dt => $"'{dt.ToString("O", CultureInfo.InvariantCulture)}'",
                float f => f.ToString(CultureInfo.InvariantCulture),
                double d => d.ToString(CultureInfo.InvariantCulture),
                decimal m => m.ToString(CultureInfo.InvariantCulture),
                int i => i.ToString(CultureInfo.InvariantCulture),
                long l => l.ToString(CultureInfo.InvariantCulture),

                _ => throw new NotSupportedException(
                    $"Unsupported default literal: {value.GetType().FullName}")
            };
        }

        private static string Q(string ident) => "\"" + ident.Replace("\"", "\"\"") + "\"";
    }
}
