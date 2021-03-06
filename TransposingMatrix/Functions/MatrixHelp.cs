using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Collections;
using System.Collections.Generic;

namespace TransposingMatrix
{

    public partial class UserDefinedFunctions
    {
        [Microsoft.SqlServer.Server.SqlFunction(
         Name = "Help",                                           // The function name
        FillRowMethodName = "ConfigTable_FillRow",                
        DataAccess = DataAccessKind.None,
        IsDeterministic = true,
                                                                  // The output table definition
        TableDefinition = @"parametarName nvarchar(100), ParametarDescription_______________________________________________________________________________________________________ nvarchar(max)")]
        public static IEnumerable MatrixHelp(
            SqlString procedureName  //
            )
        {
            var list = GetList(procedureName.Value);

            return list;

        }

        //
        // Insert row
        //
        private static void ConfigTable_FillRow(object obj, out SqlString parametarName, out SqlString parametarDescription)
        {
            parametarName = ((KeyValuePair<string, string>)(obj)).Key;
            parametarDescription = ((KeyValuePair<string, string>)(obj)).Value;
        }


        //
        // Let's describe our parameters
        //
        public static Dictionary<string, string> GetList(string procedureName)
        {
            var data = new Dictionary<string, string>();
            if (procedureName.ToLower().Equals("matrix.transposing"))
            {

                data.Add("@Query",
                    "Query or stored procedure, which result will be transposed. Calling a stored procedure always should begin with keyword EXEC.");

                data.Add("@Params",
                    "Query or stored procedure parameters.");

                data.Add("@Rco",
                    "Rotate column ordinal. Default is 0.");

                data.Add("@KeyValueOption",
                    "Do we use key-value option. Default is 0.");

                data.Add("@ColumnMapping",
                    "Columns mappings. Default is null.");

                data.Add("@TableName",
                    "If you like to save result into temporary or permanent table. For SQL Server 2008+");


            }
            else
            {
                data.Add("Error", $"The procedure or the function {procedureName}  does not exists!");
            }

            //
            // Do memory cleanup
            //
            for (var i = 0; i <= GC.MaxGeneration; i++)
            {
#if NET_4_5
            GC.Collect(i, GCCollectionMode.Forced,true,true);
#elif NET_4_0
            GC.Collect(i, GCCollectionMode.Forced,true);
#elif NET_3_5
            GC.Collect(i, GCCollectionMode.Forced);
#else
                GC.Collect(i, GCCollectionMode.Forced);
#endif
            }

            GC.WaitForPendingFinalizers();

            return data;
        }

    }
}
