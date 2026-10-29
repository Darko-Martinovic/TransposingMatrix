using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace TransposingMatrix
{
    public partial class UserDefinedFunctions
    {
        [SqlFunction(
            Name = "Help",                                                  // The function name
            FillRowMethodName = nameof(ConfigTable_FillRow),
            DataAccess = DataAccessKind.None,
            IsDeterministic = true,
            TableDefinition = "ParameterName nvarchar(100), ParameterDescription nvarchar(max)")] // The output table definition
        public static IEnumerable MatrixHelp(SqlString procedureName)
        {
            return GetList(procedureName.IsNull ? "" : procedureName.Value);
        }

        //
        // Insert row
        //
        private static void ConfigTable_FillRow(object obj, out SqlString parameterName, out SqlString parameterDescription)
        {
            var row = (KeyValuePair<string, string>)obj;
            parameterName = row.Key;
            parameterDescription = row.Value;
        }

        //
        // Let's describe our parameters
        //
        public static List<KeyValuePair<string, string>> GetList(string procedureName)
        {
            var data = new List<KeyValuePair<string, string>>();
            var name = procedureName.Replace("[", "").Replace("]", "").Trim().ToLowerInvariant();
            if (name == "matrix.transposing" || name == "transposing")
            {
                Add(data, "@Query",
                    "The query or stored procedure whose result is transposed. Calling a stored procedure always begins with the keyword EXEC.");
                Add(data, "@Params",
                    "The query's parameters, declared as in DECLARE, for example: @id int = 1, @name nvarchar(50) = N'Bikes'. Default is null.");
                Add(data, "@Rco",
                    "Rotate column ordinal: the column, counted from 0, whose values become the header. Default is 0.");
                Add(data, "@KeyValueOption",
                    "1 for a generic Key, Value, Value1... header, or the names in @ColumnMapping. Default is 0.");
                Add(data, "@ColumnMapping",
                    "The header's names with @KeyValueOption = 1, comma separated; missing names are filled with ValueN. Default is null.");
                Add(data, "@TableName",
                    "A global temporary table (##name) or a permanent table to save the result in; a table of that name is replaced. Default is null.");
            }
            else
            {
                Add(data, "Error", $"The procedure or the function {procedureName} does not exist!");
            }

            return data;
        }

        private static void Add(List<KeyValuePair<string, string>> data, string key, string value) =>
            data.Add(new KeyValuePair<string, string>(key, value));
    }
}
