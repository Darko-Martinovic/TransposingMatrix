using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace TransposingMatrix
{
    public static class PipeUtilities
    {
        // RAISERROR takes a message of up to 2047 characters
        private const int MaxMessageLength = 2047;

        /// <summary>
        /// Send the result back to the client, every column as nvarchar(max)
        /// </summary>
        public static void PipeTable(ResultTable table)
        {
            var md = new SqlMetaData[table.Columns.Count];
            for (var i = 0; i < md.Length; i++)
                md[i] = new SqlMetaData(table.Columns[i], SqlDbType.NVarChar, SqlMetaData.Max);

            var r = new SqlDataRecord(md);
            var p = SqlContext.Pipe;

            //---------------------------------------------------------------------------------------
            //  First is invoked "SendResultsStart".
            // "SendResultsStart" marks the beginning of a result set to be sent back to the client,
            //  and uses the r parameter to construct the metadata that describes the result set
            //---------------------------------------------------------------------------------------
            p.SendResultsStart(r);
            try
            {
                foreach (var row in table.Rows)
                {
                    for (var i = 0; i < row.Length; i++)
                    {
                        if (row[i] == null)
                            r.SetDBNull(i);
                        else
                            r.SetString(i, row[i]);
                    }

                    p.SendResultsRow(r);
                }
            }
            finally
            {
                //
                // "SendResultsEnd" marks the end of a result set and returns the SqlPipe instance to the initial state.
                //
                p.SendResultsEnd();
            }
        }

        /// <summary>
        /// A message, as PRINT sends it.
        /// </summary>
        public static void Print(string message) => SqlContext.Pipe.Send(message);

        /// <summary>
        /// Raises error 50000, where a message from Pipe.Send would only be printed: the client
        /// gets an error and @@ERROR is set, but T-SQL's TRY...CATCH doesn't see an error sent
        /// this way. RAISERROR, unlike THROW, lets the rest of the caller's batch run, as a failed
        /// statement does.
        /// </summary>
        public static void RaiseError(string message)
        {
            if (message.Length > MaxMessageLength)
                message = message.Substring(0, MaxMessageLength);

            using (var cnn = new SqlConnection("context connection=true"))
            using (var command = new SqlCommand("RAISERROR(N'%s', 16, 1, @message);", cnn))
            {
                command.Parameters.Add("@message", SqlDbType.NVarChar, MaxMessageLength).Value = message;
                cnn.Open();
                try
                {
                    SqlContext.Pipe.ExecuteAndSend(command);
                }
                catch (SqlException)
                {
                    // The error has reached the caller; letting it out of .NET would add error 6522 to it
                }
            }
        }
    }
}
