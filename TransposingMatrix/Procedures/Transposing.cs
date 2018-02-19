using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using Microsoft.SqlServer.Server;
using TransposingMatrix;

public partial class StoredProcedures
{

    public const int MAX_COLS = 1000;

    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void Transposing
        (
        SqlString Query, //query we passed
        [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString Params,
        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 Rco, //rotate column ordinal
        [SqlFacet(IsNullable = true, MaxSize = 4)]SqlInt16 KeyValueOption, //do we use key value option
        [SqlFacet(IsNullable = true, MaxSize = 4000)]SqlString ColumnMapping, //columns mappings
        [SqlFacet(IsNullable = true, MaxSize = 256)]SqlString TableName //temp table name

        )
    {
        DataSet ds = null;
        try
        {
            bool mySp = false;
            string queryValue = Query.Value.ToString();
            SqlParameter[] listOfParams = null;
            string[] splitQueries = queryValue.Split(';');

            string errorString = "";
            if (Params.IsNull == false && Params.Value.ToString().Equals(string.Empty) == false)
                listOfParams = DataAccess.MakeParams(Params.Value, ref errorString);


            if (errorString.Equals(string.Empty) == false)
            {
                SqlContext.Pipe.Send("There is an error when trying to determine parameters ! Error :" + errorString);
                return;
            }

            ds = DataAccess.GetDataSet(
                                queryValue, 
                                mySp, 
                                listOfParams,
                                ref errorString
                                );

            if (errorString.Equals(string.Empty) == false)
            {
                SqlContext.Pipe.Send("There is an error when trying to get the dataset ! Error :" + errorString);
                return;
            }

            foreach (DataTable t in ds.Tables)
            {
                DataTable tblCopy = t;
                if (t.Rows.Count > MAX_COLS)
                {
                    SqlContext.Pipe.Send("You are trying to generate table with more then : " + MAX_COLS.ToString() + " columns! I will display first " + MAX_COLS.ToString() + " columns !");
                    tblCopy = t.Clone();

                    // Use the ImportRow method to copy from original table to its clone.
                    for (int i = 0; i <= MAX_COLS; ++i)
                    {
                        tblCopy.ImportRow(t.Rows[i]);
                    }
                
                }
                DataTable tblRt = null;
                if (KeyValueOption.Value == 0)
                {
                     tblRt= TableManipulation.RotateTable(tblCopy);
                }
                else
                {
                    tblRt = TableManipulation.RotateTableWithKeyValue(tblCopy, ColumnMapping.IsNull ? null : ColumnMapping.Value);
                }
                if (TableName.IsNull == false)
                {
                    string cmdToExecute = TableManipulation.CreateTABLE(TableName.Value, tblRt);
                    cmdToExecute += ";\n" +  TableManipulation.CreateTYPE(TableName.Value, tblRt);
                    DataAccess.GetNonQuery(cmdToExecute);

                    DataAccess.SaveResult(TableName.Value, tblRt);

                    DataAccess.GetNonQuery("DROP TYPE MATRIX.TVP_" + TableName.Value);
                }
                PipeUtilities.PipeDataTable(tblRt);

            }

        }
        catch (Exception ex)
        {
           SqlContext.Pipe.Send("There is an error in transposing matrix : " + ex.Message + "\r\n" + ex.InnerException == null ? "" : ex.InnerException.Message);
    
        }
        finally
        {
            ds = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


    }


}
