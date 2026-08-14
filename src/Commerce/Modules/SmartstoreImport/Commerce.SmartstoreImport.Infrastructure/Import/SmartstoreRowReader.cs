using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Commerce.SmartstoreImport.Contracts;

namespace Commerce.SmartstoreImport.Infrastructure.Import;

internal static class SmartstoreRowReader
{
    public static bool TryGetInt(SmartstoreParsedRow row, string column, out int value)
    {
        value = default;
        if (!row.Values.TryGetValue(column, out var raw) || raw is null)
        {
            return false;
        }

        return raw switch
        {
            int i => Assign(i, out value),
            long l => Assign((int)l, out value),
            decimal d => Assign((int)d, out value),
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) =>
                Assign(parsed, out value),
            _ => false
        };
    }

    public static int GetInt(SmartstoreParsedRow row, string column, int fallback = 0) =>
        TryGetInt(row, column, out var value) ? value : fallback;

    public static bool TryGetDecimal(SmartstoreParsedRow row, string column, out decimal value)
    {
        value = default;
        if (!row.Values.TryGetValue(column, out var raw) || raw is null)
        {
            return false;
        }

        return raw switch
        {
            decimal d => Assign(d, out value),
            int i => Assign(i, out value),
            long l => Assign(l, out value),
            double dbl => Assign((decimal)dbl, out value),
            string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) =>
                Assign(parsed, out value),
            _ => false
        };
    }

    public static decimal GetDecimal(SmartstoreParsedRow row, string column, decimal fallback = 0m) =>
        TryGetDecimal(row, column, out var value) ? value : fallback;

    public static bool TryGetBool(SmartstoreParsedRow row, string column, out bool value)
    {
        value = default;
        if (!row.Values.TryGetValue(column, out var raw) || raw is null)
        {
            return false;
        }

        return raw switch
        {
            bool b => Assign(b, out value),
            int i => Assign(i != 0, out value),
            string s when bool.TryParse(s, out var parsed) => Assign(parsed, out value),
            string s when int.TryParse(s, out var intParsed) => Assign(intParsed != 0, out value),
            _ => false
        };
    }

    public static bool GetBool(SmartstoreParsedRow row, string column, bool fallback = false) =>
        TryGetBool(row, column, out var value) ? value : fallback;

    public static string? GetString(SmartstoreParsedRow row, string column)
    {
        if (!row.Values.TryGetValue(column, out var raw) || raw is null)
        {
            return null;
        }

        return raw switch
        {
            string s => s,
            Guid g => g.ToString(),
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            _ => Convert.ToString(raw, CultureInfo.InvariantCulture)
        };
    }

    public static DateTime GetDateTimeUtc(SmartstoreParsedRow row, string column, DateTime? fallback = null)
    {
        if (!row.Values.TryGetValue(column, out var raw) || raw is null)
        {
            return fallback ?? DateTime.UtcNow;
        }

        return raw switch
        {
            DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) =>
                DateTime.SpecifyKind(parsed, DateTimeKind.Utc),
            _ => fallback ?? DateTime.UtcNow
        };
    }

    public static Guid GetGuid(SmartstoreParsedRow row, string column, Guid? fallback = null)
    {
        if (!row.Values.TryGetValue(column, out var raw) || raw is null)
        {
            return fallback ?? Guid.NewGuid();
        }

        return raw switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => fallback ?? Guid.NewGuid()
        };
    }

    public static string ToSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "item";
        }

        var normalized = value.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", string.Empty);
        normalized = Regex.Replace(normalized, @"\s+", "-");
        normalized = Regex.Replace(normalized, @"-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "item" : normalized;
    }

    public static string ToSystemName(string? value, int maxLength = 100)
    {
        var slug = ToSlug(value);
        return slug.Length <= maxLength ? slug : slug[..maxLength];
    }

    private static bool Assign<T>(T source, out T target)
    {
        target = source;
        return true;
    }
}

internal static class SmartstoreDataSetExtensions
{
    public static SmartstoreParsedTable? GetTable(this SmartstoreParsedDataSet dataSet, string tableName)
    {
        return dataSet.Tables.TryGetValue(tableName, out var table) ? table : null;
    }

    public static bool HasTable(this SmartstoreParsedDataSet dataSet, string tableName) =>
        dataSet.Tables.ContainsKey(tableName);

    public static bool HasAnyTable(this SmartstoreParsedDataSet dataSet, params string[] tableNames) =>
        tableNames.Any(dataSet.HasTable);
}
