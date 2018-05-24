using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using TransposingMatrix;

public partial class StoredProcedures
{

    public const int MaxCols = 1000;

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void Transposing
        (
        SqlString query,                                                      // the query or the stored procedure name. Calling a stored procedure always should begin with keyword EXEC.
        [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString Params,        // the query or the stored procedure parameters 
        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 rco,               // rotate column ordinal
        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 keyValueOption,    // do we use key value option
        [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString columnMapping, // columns mappings
        [SqlFacet(IsNullable = true, MaxSize = 256)]SqlString tableName       // temp(permanent) table name

        )
    {
        DataSet ds;
        var errorString = "";
        try
        {
            var queryValue = query.Value;
            SqlParameter[] listOfParams = null;


            //
            // Make parameters
            //
            if (Params.IsNull == false && Params.Value.Equals(string.Empty) == false)
                listOfParams = DataAccess.MakeParams(Params.Value, ref errorString);

            var pipe = SqlContext.Pipe;
            //
            // Quit if any errors
            //
            if (errorString.Equals(string.Empty) == false)
            {
                pipe?.Send($"There is an error when trying to determine parameters ! Error :{errorString}");
                return;
            }


            //
            // Determine the dataset object
            //
            ds = DataAccess.GetDataSet(
                                         queryValue, 
                                         listOfParams,
                                         ref errorString
                                      );

            //
            // Quit if any errors
            //
            if (errorString.Equals(string.Empty) == false)
            {
                pipe?.Send($"There is an error when trying to determine the dataset ! Error :{errorString}");
                return;
            }


            //
            // Let's iterate through the table collection
            //
            foreach (DataTable t in ds.Tables)
            {
                var tblCopy = t;

                //
                // Temporary limitation to MaxCols
                //
                if (t.Rows.Count > MaxCols)
                {
                    pipe?.Send(
                        $"You are trying to generate table with more then : {MaxCols} columns! I will display first {MaxCols} columns !");
                    tblCopy = t.Clone();

                    //
                    // Use the ImportRow method to copy from original table to its clone
                    //
                    for (var i = 0; i <= MaxCols; ++i)
                        tblCopy.ImportRow(t.Rows[i]);
                }

                var tblRt = keyValueOption.Value == 0
                    ? TableManipulation.RotateTable(tblCopy, rco.Value)
                    : TableManipulation.RotateTableWithKeyValue(tblCopy,
                        columnMapping.IsNull ? null : columnMapping.Value);

                //
                // If we want to save our results
                //
                if (tableName.IsNull == false)
                {
                    var fullName = tableName.Value.Split('.');
                    var partialTableName = fullName[fullName.Length - 1];

                    //
                    // First we build T-SQL to create the table 
                    //
                    string cmdToExecute = DataAccess.CreateTable(tableName.Value, tblRt);
                    // 
                    // Then build T-SQL to create the table value type ( SQL 2008+ )
                    //
                    cmdToExecute += ";\n" +  DataAccess.CreateType(partialTableName, tblRt);

                    DataAccess.ExecuteNonQuery(cmdToExecute);

                    DataAccess.SaveResult(tableName.Value, tblRt);
                    //
                    // Drop the type
                    //
                    DataAccess.ExecuteNonQuery("DROP TYPE MATRIX.TVP_" + partialTableName);
                }
                //
                // Send the result to the client
                //
                PipeUtilities.PipeDataTable(tblRt);

            }

        }
        catch (Exception ex)
        {
            errorString = ex.Message;

            if (ex.InnerException != null)
                errorString += $"\r\n{ex.InnerException.Message}";

            SqlContext.Pipe.Send($"There is an error in the stored procedure : {errorString}");
            

        }
        finally
        {
            ds = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


    }


}
