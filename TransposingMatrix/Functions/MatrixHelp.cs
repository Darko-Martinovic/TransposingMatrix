using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Collections;
using System.Collections.Generic;

namespace TransposingMatrix
{

    public partial class UserDefinedFunctions
    {
        [Microsoft.SqlServer.Server.SqlFunction(Name = "Help",
        FillRowMethodName = "ConfigTable_FillRow",
        DataAccess = DataAccessKind.None,
        IsDeterministic = true,
        TableDefinition = @"ParametarName nvarchar(100), ParametarDescription_______________________________________________________________________________________________________ nvarchar(max)")]
        public static IEnumerable MatrixHelp(
            SqlString ProcedureName  //
            )
        {
            Dictionary<string, string> list = GetList(ProcedureName.Value);

            return list;

        }
        private static void ConfigTable_FillRow(object obj, out SqlString ParametarName, out SqlString ParametarDescription)
        {
            ParametarName = ((System.Collections.Generic.KeyValuePair<string, string>)(obj)).Key;
            ParametarDescription = ((System.Collections.Generic.KeyValuePair<string, string>)(obj)).Value;
        }

        public static Dictionary<string, string> GetList(string ProcedureName)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            if (ProcedureName.ToLower().Equals("matrix.transposing"))
            {

                data.Add("@Query", "Query or stored procedure, which result will be transposed. Calling a stored procedure always should begin with keyword EXEC.");

                data.Add("@Params", "Query or stored procedure parameters.");

                data.Add("@Rco", "Rotate column ordinal. Default is 1.");

                data.Add("@KeyValueOption", "Do we use key-value option. Default is 0.");

                data.Add("@ColumnMapping", "Columns mappings.Default is null.");


            }
            else
            {
                data.Add("Error", "Procedure or function " + ProcedureName + " does not exists!");
            }

            for (int i = 0; i <= GC.MaxGeneration; i++)
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
