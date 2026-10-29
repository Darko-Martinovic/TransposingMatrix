using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace TransposingMatrix
{
    public partial class StoredProcedures
    {
        /// <summary>
        /// The most rows a result may have. Each row becomes a column, and SQL Server tables hold
        /// at most 1024 columns.
        /// </summary>
        public const int MaxRows = 1000;

        [SqlProcedure]
        public static void Transposing
            (
            SqlString query,                                                      // the query, or EXEC and a stored procedure with its arguments
            [SqlFacet(IsNullable = true, MaxSize = 4000)] SqlString parameters,   // the query's parameters, declared as in DECLARE: @id int = 1, @name nvarchar(50) = N'x'
            SqlInt16 rco,                                                         // rotate column ordinal: the column whose values become the header
            SqlInt16 keyValueOption,                                              // 1: a generic Key, Value, Value1... header instead
            [SqlFacet(IsNullable = true, MaxSize = 4000)] SqlString columnMapping,// the header's names with keyValueOption 1, comma separated
            [SqlFacet(IsNullable = true, MaxSize = 256)] SqlString tableName      // a global temporary (##) or permanent table to save the result in
            )
        {
            try
            {
                if (query.IsNull || query.Value.Trim().Length == 0)
                    throw new TransposingException("@Query is required.");

                //
                // The parameters are a DECLARE list, so SQL Server itself parses their types and values
                //
                var batch = parameters.IsNull || parameters.Value.Trim().Length == 0
                    ? query.Value
                    : $"DECLARE {parameters.Value};\n{query.Value}";

                var results = DataAccess.GetResults(batch, MaxRows);
                var saveTo = tableName.IsNull || tableName.Value.Trim().Length == 0 ? null : tableName.Value.Trim();

                for (var i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    if (result.Truncated)
                        PipeUtilities.Print($"The result has more than {MaxRows} rows: only the first {MaxRows} are transposed.");

                    var rotated = !keyValueOption.IsNull && keyValueOption.Value != 0
                        ? TableManipulation.RotateTableWithKeyValue(result, columnMapping.IsNull ? null : columnMapping.Value)
                        : TableManipulation.RotateTable(result, rco.IsNull ? 0 : rco.Value);

                    if (saveTo != null && i == 0)
                        DataAccess.SaveResult(saveTo, rotated);
                    else if (saveTo != null)
                        PipeUtilities.Print($"Only the first result is saved in {saveTo}; result {i + 1} is not.");

                    //
                    // Send the result to the client
                    //
                    PipeUtilities.PipeTable(rotated);
                }
            }
            catch (Exception ex)
            {
                PipeUtilities.RaiseError(ErrorMessage(ex));
            }
        }

        private static string ErrorMessage(Exception ex)
        {
            if (ex is TransposingException)
                return ex.Message;

            var message = $"MATRIX.Transposing failed: {ex.Message}";
            //
            // The in-process data access of .NET Framework predates the json and vector types of
            // SQL Server 2025, and fails on them with this message
            //
            if (ex.Message.Contains("Inproc data access"))
                message += " If the query returns json or vector columns, CAST them to nvarchar(max).";
            return message;
        }
    }

    /// <summary>A mistake in the procedure's arguments, reported as it is.</summary>
    public sealed class TransposingException : Exception
    {
        public TransposingException(string message) : base(message)
        {
        }
    }
}
