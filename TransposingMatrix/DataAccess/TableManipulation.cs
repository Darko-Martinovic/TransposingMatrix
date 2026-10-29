using System;
using System.Collections.Generic;

namespace TransposingMatrix
{
    public static class TableManipulation
    {
        // SQL Server's column names (sysname) are at most 128 characters
        private const int MaxNameLength = 128;

        /// <summary>
        /// Transposing the result with custom or generic header
        /// </summary>
        /// <param name="oldTable">The result to transform</param>
        /// <param name="columnMapping">The custom header, comma separated</param>
        public static ResultTable RotateTableWithKeyValue(ResultTable oldTable, string? columnMapping)
        {
            var header = new HeaderNames();
            var size = oldTable.Rows.Count + 1;

            // The column mapping is not provided, so create the generic header: Key, Value, Value1...
            if (columnMapping == null)
            {
                header.Add("Key");
                for (var no = 0; no < oldTable.Rows.Count; no++)
                    header.Add(no == 0 ? "Value" : "Value" + no);
            }
            else
            {
                foreach (var name in columnMapping.Split(','))
                {
                    if (header.Count == size)
                        break;
                    header.Add(Unbracket(name.Trim()));
                }

                // if we do not pass "enough" names, fill the rest with generic names
                for (var no = header.Count; no < size; no++)
                    header.Add("Value" + no);
            }

            var newTable = new ResultTable(header.Names);
            for (var col = 0; col < oldTable.Columns.Count; col++)
                newTable.Rows.Add(Line(oldTable, col));
            return newTable;
        }

        /// <summary>
        /// The classical approach. The rotation is made based on "rco" value: the values of that
        /// column become the header.
        /// </summary>
        public static ResultTable RotateTable(ResultTable oldTable, int rco = 0)
        {
            if (rco < 0 || rco >= oldTable.Columns.Count)
                throw new TransposingException(
                    $"@Rco is {rco}, but the result's columns are numbered 0 to {oldTable.Columns.Count - 1}.");

            var header = new HeaderNames();
            header.Add(oldTable.Columns[rco]);
            foreach (var row in oldTable.Rows)
                header.Add(row[rco]);

            var newTable = new ResultTable(header.Names);
            for (var col = 0; col < oldTable.Columns.Count; col++)
                if (col != rco)
                    newTable.Rows.Add(Line(oldTable, col));
            return newTable;
        }

        /// <summary>
        /// A column of the old table as a row of the new one: its name, then its values.
        /// </summary>
        private static string?[] Line(ResultTable oldTable, int col)
        {
            var line = new string?[oldTable.Rows.Count + 1];
            line[0] = oldTable.Columns[col];
            for (var row = 0; row < oldTable.Rows.Count; row++)
                line[row + 1] = oldTable.Rows[row][col];
            return line;
        }

        private static string Unbracket(string name) =>
            name.Length > 2 && name.StartsWith("[", StringComparison.Ordinal) && name.EndsWith("]", StringComparison.Ordinal)
                ? name.Substring(1, name.Length - 2).Replace("]]", "]")
                : name;

        /// <summary>
        /// Column names a table accepts: an empty name becomes ColumnN, a repeated one gets the
        /// suffix _N, and a long one is cut to 128 characters.
        /// </summary>
        private sealed class HeaderNames
        {
            // Not a HashSet: SQL Server forbids System.Core's HashSet in a SAFE assembly
            private readonly Dictionary<string, bool> _used = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            public List<string> Names { get; } = new List<string>();

            public int Count => Names.Count;

            public void Add(string? name)
            {
                if (string.IsNullOrEmpty(name))
                    name = "Column" + (Names.Count + 1);
                name = Cut(name!, 0);

                // The suffix is the name's position among the values, as in version 1
                var unique = name;
                for (var no = Names.Count - 1; _used.ContainsKey(unique); no++)
                    unique = Cut(name, no.ToString().Length + 1) + "_" + no;
                _used.Add(unique, true);
                Names.Add(unique);
            }

            private static string Cut(string name, int room) =>
                name.Length > MaxNameLength - room ? name.Substring(0, MaxNameLength - room) : name;
        }
    }
}
