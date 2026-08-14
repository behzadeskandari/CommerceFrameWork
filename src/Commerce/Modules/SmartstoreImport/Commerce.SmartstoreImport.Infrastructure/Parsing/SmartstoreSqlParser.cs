using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;
using Commerce.SmartstoreImport.Contracts;

namespace Commerce.SmartstoreImport.Infrastructure.Parsing;

public sealed partial class SmartstoreSqlParser : ISmartstoreSqlParser
{
    private static readonly Regex InsertHeaderRegex = InsertHeaderPattern();
    private static readonly Regex CreateTableRegex = CreateTablePattern();

    public Result<SmartstoreParsedDataSet> ParseFile(string sqlFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sqlFilePath))
        {
            return Result.Failure<SmartstoreParsedDataSet>(Error.Validation("smartstore.parse.path", "SQL file path is required."));
        }

        if (!File.Exists(sqlFilePath))
        {
            return Result.Failure<SmartstoreParsedDataSet>(
                Error.Validation("smartstore.parse.missing", $"SQL file not found: {sqlFilePath}"));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.GetFullPath(sqlFilePath);
        var fileBytes = File.ReadAllBytes(fullPath);
        var hash = Convert.ToHexString(SHA256.HashData(fileBytes)).ToLowerInvariant();
        var content = Encoding.UTF8.GetString(fileBytes);

        var warnings = new List<string>();
        var tableColumns = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var tableRows = new Dictionary<string, List<SmartstoreParsedRow>>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in CreateTableRegex.Matches(content))
        {
            var tableName = match.Groups["table"].Success
                ? NormalizeTableName(match.Groups["table"].Value)
                : match.Groups["table2"].Success
                    ? NormalizeTableName(match.Groups["table2"].Value)
                    : NormalizeTableName(match.Groups["table3"].Value);
            var body = match.Groups["body"].Value;
            var columns = ParseCreateTableColumns(body);
            if (columns.Count > 0)
            {
                tableColumns[tableName] = columns;
            }
        }

        var lineNumber = 0;
        foreach (var rawLine in content.Split('\n'))
        {
            cancellationToken.ThrowIfCancellationRequested();
            lineNumber++;

            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var headerMatch = InsertHeaderRegex.Match(line);
            if (!headerMatch.Success)
            {
                continue;
            }

            var tableName = ExtractInsertTableName(headerMatch);
            if (string.IsNullOrWhiteSpace(tableName))
            {
                warnings.Add($"Line {lineNumber}: Could not parse INSERT target table.");
                continue;
            }

            var columns = ParseColumnList(headerMatch.Groups["columns"].Value);
            if (columns.Count == 0)
            {
                warnings.Add($"Line {lineNumber}: INSERT into [{tableName}] has no columns.");
                continue;
            }

            if (!tableColumns.TryGetValue(tableName, out var schemaColumns))
            {
                tableColumns[tableName] = columns;
            }
            else if (!schemaColumns.SequenceEqual(columns, StringComparer.OrdinalIgnoreCase))
            {
                warnings.Add($"Line {lineNumber}: INSERT column list for [{tableName}] differs from CREATE TABLE schema.");
            }

            if (!tableRows.TryGetValue(tableName, out var rows))
            {
                rows = [];
                tableRows[tableName] = rows;
            }

            var valuesSection = line[headerMatch.Length..].TrimStart();
            if (!valuesSection.StartsWith("VALUES", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add($"Line {lineNumber}: INSERT into [{tableName}] missing VALUES clause.");
                continue;
            }

            valuesSection = valuesSection["VALUES".Length..].TrimStart();
            foreach (var tuple in SplitValueTuples(valuesSection))
            {
                var values = ParseValueList(tuple);
                if (values.Count != columns.Count)
                {
                    warnings.Add(
                        $"Line {lineNumber}: INSERT into [{tableName}] column/value count mismatch ({columns.Count} vs {values.Count}).");
                    continue;
                }

                var rowValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < columns.Count; i++)
                {
                    rowValues[columns[i]] = values[i];
                }

                rows.Add(new SmartstoreParsedRow(lineNumber, rowValues));
            }
        }

        var tables = new Dictionary<string, SmartstoreParsedTable>(StringComparer.OrdinalIgnoreCase);
        foreach (var (tableName, columns) in tableColumns)
        {
            tableRows.TryGetValue(tableName, out var rows);
            tables[tableName] = new SmartstoreParsedTable(
                tableName,
                columns,
                rows ?? []);
        }

        foreach (var (tableName, rows) in tableRows)
        {
            if (tables.ContainsKey(tableName))
            {
                continue;
            }

            var columns = rows.FirstOrDefault()?.Values.Keys.ToList() ?? [];
            tables[tableName] = new SmartstoreParsedTable(tableName, columns, rows);
        }

        return Result.Success(new SmartstoreParsedDataSet(fullPath, hash, tables, warnings));
    }

    private static List<string> ParseCreateTableColumns(string body)
    {
        var columns = new List<string>();
        foreach (var rawLine in body.Split('\n'))
        {
            var line = rawLine.Trim().TrimEnd(',');
            if (line.Length == 0 ||
                line.StartsWith("CONSTRAINT", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("PRIMARY KEY", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith(")", StringComparison.Ordinal))
            {
                continue;
            }

            var match = Regex.Match(line, @"^\[(?<col>[^\]]+)\]|^(?<col2>[A-Za-z_][A-Za-z0-9_]*)");
            if (match.Success)
            {
                var name = match.Groups["col"].Success ? match.Groups["col"].Value : match.Groups["col2"].Value;
                columns.Add(name);
            }
        }

        return columns;
    }

    private static List<string> ParseColumnList(string columnSection)
    {
        var columns = new List<string>();
        foreach (var token in SplitCommaSeparated(columnSection))
        {
            var trimmed = token.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                columns.Add(trimmed[1..^1]);
            }
            else if (trimmed.Length > 0)
            {
                columns.Add(trimmed);
            }
        }

        return columns;
    }

    private static IEnumerable<string> SplitValueTuples(string valuesSection)
    {
        var tuples = new List<string>();
        var depth = 0;
        var start = -1;

        for (var i = 0; i < valuesSection.Length; i++)
        {
            var ch = valuesSection[i];
            if (ch == '(')
            {
                if (depth == 0)
                {
                    start = i + 1;
                }

                depth++;
            }
            else if (ch == ')')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    tuples.Add(valuesSection[start..i]);
                    start = -1;
                }
            }
        }

        return tuples;
    }

    private static List<object?> ParseValueList(string tupleBody)
    {
        var values = new List<object?>();
        foreach (var token in SplitCommaSeparated(tupleBody))
        {
            values.Add(ParseScalar(token.Trim()));
        }

        return values;
    }

    private static object? ParseScalar(string token)
    {
        if (token.Equals("NULL", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (token.StartsWith('N') && token.Length >= 2 && token[1] == '\'')
        {
            return Unquote(token[1..]);
        }

        if (token.StartsWith('\''))
        {
            return Unquote(token);
        }

        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return token;
        }

        if (bool.TryParse(token, out var boolValue))
        {
            return boolValue;
        }

        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return intValue;
        }

        if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            return longValue;
        }

        if (decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return decimalValue;
        }

        if (DateTime.TryParse(token, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateValue))
        {
            return dateValue;
        }

        if (Guid.TryParse(token.Trim('\''), out var guidValue))
        {
            return guidValue;
        }

        return token;
    }

    private static string Unquote(string token)
    {
        var inner = token.Trim('\'');
        return inner.Replace("''", "'");
    }

    private static IEnumerable<string> SplitCommaSeparated(string input)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var inString = false;

        for (var i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            if (ch == '\'')
            {
                inString = !inString;
                current.Append(ch);
                continue;
            }

            if (ch == ',' && !inString)
            {
                parts.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }

        return parts;
    }

    internal static string NormalizeTableName(string rawName)
    {
        var name = rawName.Trim().Trim('[', ']');
        var lastDot = name.LastIndexOf('.');
        return lastDot >= 0 ? name[(lastDot + 1)..] : name;
    }

    [GeneratedRegex(
        @"INSERT\s+INTO\s+(?:\[dbo\]\.\[(?<table>[^\]]+)\]|\[(?<table2>[^\]]+)\]|(?<table3>[A-Za-z_][A-Za-z0-9_]*))\s*\((?<columns>[^\)]+)\)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex InsertHeaderPattern();

    private static string? ExtractInsertTableName(Match match)
    {
        if (match.Groups["table"].Success)
        {
            return NormalizeTableName(match.Groups["table"].Value);
        }

        if (match.Groups["table2"].Success)
        {
            return NormalizeTableName(match.Groups["table2"].Value);
        }

        if (match.Groups["table3"].Success)
        {
            return NormalizeTableName(match.Groups["table3"].Value);
        }

        return null;
    }

    [GeneratedRegex(
        @"CREATE\s+TABLE\s+(?:\[dbo\]\.\[(?<table>[^\]]+)\]|\[(?<table2>[^\]]+)\]|(?<table3>[A-Za-z_][A-Za-z0-9_]*))\s*\((?<body>.*?)\)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex CreateTablePattern();
}
