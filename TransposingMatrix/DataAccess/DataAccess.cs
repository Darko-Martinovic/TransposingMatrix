using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace TransposingMatrix
{
    internal static class DataAccess
    {
        // We must use "context connection=true": the caller's connection, permissions and transaction
        private const string ContextConnection = "context connection=true";

        // "Cannot create a row of size %d which is greater than the allowable maximum row size of %d."
        private const int RowTooLarge = 511;

        /// <summary>
        /// Runs the batch and reads every result it returns as text, up to maxRows rows each.
        /// </summary>
        public static List<ResultTable> GetResults(string batch, int maxRows)
        {
            var results = new List<ResultTable>();
            using (var cnn = new SqlConnection(ContextConnection))
            using (var command = new SqlCommand(batch, cnn))
            {
                cnn.Open();
                using (var reader = command.ExecuteReader())
                {
                    do
                    {
                        //
                        // Statements without a result, such as SET or INSERT, have no columns
                        //
                        if (reader.FieldCount == 0)
                            continue;

                        var columns = new string[reader.FieldCount];
                        for (var i = 0; i < columns.Length; i++)
                            columns[i] = reader.GetName(i);

                        var formatter = new ValueFormatter(reader);
                        var table = new ResultTable(columns);
                        while (reader.Read())
                        {
                            if (table.Rows.Count == maxRows)
                            {
                                table.Truncated = true;
                                break;
                            }

                            var row = new string?[columns.Length];
                            for (var i = 0; i < row.Length; i++)
                                row[i] = formatter.Format(reader, i);
                            table.Rows.Add(row);
                        }

                        results.Add(table);
                    } while (reader.NextResult());
                }
            }

            return results;
        }

        /// <summary>
        /// Saves the transposed result in a new table, replacing a table of that name. A local
        /// temporary table (#name) can't be used: it would be dropped when the procedure ends.
        /// </summary>
        public static void SaveResult(string tableName, ResultTable table)
        {
            if (tableName.StartsWith("#", StringComparison.Ordinal) && !tableName.StartsWith("##", StringComparison.Ordinal))
                throw new TransposingException(
                    $"{tableName} is a local temporary table, which SQL Server drops when MATRIX.Transposing ends. Use a global temporary table (#{tableName}) or a permanent table.");

            using (var cnn = new SqlConnection(ContextConnection))
            {
                cnn.Open();
                var lengths = ColumnLengths(table);
                Execute(cnn, CreateTable(tableName, table, lengths), new SqlParameter("@objectName", ObjectName(tableName)));

                try
                {
                    InsertRows(cnn, tableName, table, lengths);
                }
                catch (SqlException ex) when (ex.Number == RowTooLarge)
                {
                    Execute(cnn, $"DROP TABLE {tableName};");
                    throw new TransposingException(
                        $"The transposed result is too wide to save in a table: {ex.Message} Transpose fewer rows, or save fewer of them in each table.");
                }
            }
        }

        /// <summary>
        /// The length of each column: its longest value, or max beyond 4000 characters. Tables of
        /// many nvarchar(max) columns can't hold a row, since each needs a pointer of its own.
        /// </summary>
        private static int[] ColumnLengths(ResultTable table)
        {
            var lengths = new int[table.Columns.Count];
            for (var i = 0; i < lengths.Length; i++)
                lengths[i] = 1;

            foreach (var row in table.Rows)
                for (var i = 0; i < lengths.Length; i++)
                    if (row[i] != null && row[i]!.Length > lengths[i])
                        lengths[i] = row[i]!.Length;

            for (var i = 0; i < lengths.Length; i++)
                if (lengths[i] > 4000)
                    lengths[i] = -1;
            return lengths;
        }

        /// <summary>
        /// The name OBJECT_ID needs: temporary tables are in tempdb.
        /// </summary>
        private static string ObjectName(string tableName) =>
            tableName.StartsWith("#", StringComparison.Ordinal) ? "tempdb.." + tableName : tableName;

        /// <summary>
        /// That's the way how to build DDL statement to create table
        /// </summary>
        private static string CreateTable(string tableName, ResultTable table, int[] lengths)
        {
            var sql = new StringBuilder();
            sql.Append("IF OBJECT_ID(@objectName, N'U') IS NOT NULL\n");
            sql.Append($"    DROP TABLE {tableName};\n");
            sql.Append($"CREATE TABLE {tableName} (");
            for (var i = 0; i < table.Columns.Count; i++)
            {
                sql.Append(i == 0 ? "\n" : ",\n");
                sql.Append($"    {QuoteName(table.Columns[i])} nvarchar({(lengths[i] == -1 ? "max" : lengths[i].ToString())}) NULL");
            }

            return sql.Append("\n);").ToString();
        }

        /// <summary>
        /// One parameterised INSERT per row: a row has at most 1001 values, within the 2100
        /// parameters a command may have.
        /// </summary>
        private static void InsertRows(SqlConnection cnn, string tableName, ResultTable table, int[] lengths)
        {
            var sql = new StringBuilder($"INSERT INTO {tableName} VALUES (");
            for (var i = 0; i < table.Columns.Count; i++)
                sql.Append(i == 0 ? "@p0" : $", @p{i}");
            sql.Append(");");

            using (var command = new SqlCommand(sql.ToString(), cnn))
            {
                for (var i = 0; i < table.Columns.Count; i++)
                    command.Parameters.Add($"@p{i}", SqlDbType.NVarChar, lengths[i]);

                foreach (var row in table.Rows)
                {
                    for (var i = 0; i < row.Length; i++)
                        command.Parameters[i].Value = (object?)row[i] ?? DBNull.Value;
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void Execute(SqlConnection cnn, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, cnn))
            {
                command.Parameters.AddRange(parameters);
                command.ExecuteNonQuery();
            }
        }

        private static string QuoteName(string name) => "[" + name.Replace("]", "]]") + "]";
    }
}
