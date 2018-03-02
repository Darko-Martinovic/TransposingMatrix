using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using TransposingMatrix;

public partial class StoredProcedures
{

    public const int MAX_COLS = 1000;

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void Transposing
        (
        SqlString Query,                                                      // the query or the stored procedure name. If we pass the stored procedure
        [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString Params,        // the query or the stored procedure parameters 
        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 Rco,               // rotate column ordinal
        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 KeyValueOption,    // do we use key value option
        [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString ColumnMapping, // columns mappings
        [SqlFacet(IsNullable = true, MaxSize = 256)]SqlString TableName       // temp(permanent) table name

        )
    {
        DataSet ds = null;
        string errorString = "";
        try
        {
            bool mySp = false;
            string queryValue = Query.Value.ToString();
            SqlParameter[] listOfParams = null;
            string[] splitQueries = queryValue.Split(';');

            //
            // Make parameters
            //
            if (Params.IsNull == false && Params.Value.ToString().Equals(string.Empty) == false)
                listOfParams = DataAccess.MakeParams(Params.Value, ref errorString);

            //
            // Quit if any errors
            //
            if (errorString.Equals(string.Empty) == false)
            {
                SqlContext.Pipe.Send("There is an error when trying to determine parameters ! Error :" + errorString);
                return;
            }


            //
            // Determine the dataset object
            //
            ds = DataAccess.GetDataSet(
                                         queryValue, 
                                         mySp, 
                                         listOfParams,
                                         ref errorString
                                      );

            //
            // Quit if any errors
            //
            if (errorString.Equals(string.Empty) == false)
            {
                SqlContext.Pipe.Send("There is an error when trying to determine the dataset ! Error :" + errorString);
                return;
            }


            //
            // Let's iterate through the table collection
            //
            foreach (DataTable t in ds.Tables)
            {
                DataTable tblCopy = t;

                //
                // Temporary limitation to MAX_COLS
                //
                if (t.Rows.Count > MAX_COLS)
                {
                    SqlContext.Pipe.Send("You are trying to generate table with more then : " + MAX_COLS.ToString() + 
                                         " columns! I will display first " + MAX_COLS.ToString() + " columns !");
                    tblCopy = t.Clone();

                    //
                    // Use the ImportRow method to copy from original table to its clone
                    //
                    for (int i = 0; i <= MAX_COLS; ++i)
                    {
                        tblCopy.ImportRow(t.Rows[i]);
                    }
                
                }
                DataTable tblRt = null;
                if (KeyValueOption.Value == 0)
                {
                     tblRt= TableManipulation.RotateTable(tblCopy,Rco.Value);
                }
                else
                {
                    tblRt = TableManipulation.RotateTableWithKeyValue(tblCopy, ColumnMapping.IsNull ? null : ColumnMapping.Value);
                }

                //
                // If we want to save our results
                //
                if (TableName.IsNull == false)
                {
                    string[] fullName = TableName.Value.Split('.');
                    string partialTableName = fullName[fullName.Length - 1];

                    //
                    // First we build T-SQL to create the table 
                    //
                    string cmdToExecute = DataAccess.CreateTABLE(TableName.Value, tblRt);
                    // 
                    // Then build T-SQL to create the table value type ( SQL 2008+ )
                    //
                    cmdToExecute += ";\n" +  DataAccess.CreateTYPE(partialTableName, tblRt);

                    DataAccess.ExecuteNonQuery(cmdToExecute);

                    DataAccess.SaveResult(TableName.Value, tblRt);
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
                errorString += "\r\n" + ex.InnerException.Message;

            SqlContext.Pipe.Send("There is an error in the stored procedure : " + errorString);
            

        }
        finally
        {
            ds = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


    }


}
