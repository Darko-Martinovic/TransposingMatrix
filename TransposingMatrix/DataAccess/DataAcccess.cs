using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TransposingMatrix
{
    class DataAccess
    {

        /// <summary>
        /// The general routine to get the dataset object
        /// </summary>
        /// <param name="query">T-SQL query or the stored procedure name</param>
        /// <param name="listOfParams">The query or the stored procedure parameters</param>
        /// <param name="errorText">If an error occurred, we captured the message</param>
        /// <returns></returns>
        public static DataSet GetDataSet(string query,  SqlParameter[] listOfParams, ref string errorText)
        {
            var ds = new DataSet();
            try
            {
                //
                // we must use "context connection=true" keyword
                //
                using (var cnn = new SqlConnection("context connection=true"))
                {
                    using (var command = new SqlCommand(query, cnn))
                    {
                        cnn.Open();
                        //if (isSp)
                        //    command.CommandType = CommandType.StoredProcedure;
                        if (listOfParams != null)
                        {
                            foreach (var p in listOfParams)
                            {
                                command.Parameters.Add(p);
                            }
                        }
                        using (var sqlAdp = new SqlDataAdapter())
                        {
                            sqlAdp.SelectCommand = command;
                            sqlAdp.Fill(ds);
                        }
                        cnn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                errorText += ex.Message;
            }
            return ds;
        }


        /// <summary>
        /// A wrapper around ExecuteNonQuery. It is used to create a permanent or a temporary table.
        /// </summary>
        /// <param name="commandText">DDL command</param>
        /// <returns></returns>
        public static bool ExecuteNonQuery(string commandText)
        {
            var retValue = false;
            try
            {

                //
                // we must use "context connection=true" keyword
                //
                using (var cnn = new SqlConnection("context connection=true"))
                {
                    using (var command = new SqlCommand(commandText, cnn))
                    {
                        cnn.Open();
                        command.ExecuteNonQuery();
                        retValue = true;
                        cnn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return retValue;
        }


        /// <summary>
        /// It is used to save the result of transposing to the temporary or the permanent table.
        /// I used mapping of the datatable object to SqlDbType.Structed parameter. 
        /// The structed parameter = SQL Server table value type
        /// </summary>
        /// <param name="tblName">The temporary table name</param>
        /// <param name="dt">The datatable object</param>
        /// <returns></returns>
        public static bool SaveResult(string tblName, DataTable dt)
        {
            bool retValue = true;
            try
            {

                string[] fullName = tblName.Split('.');
                string partialTableName = fullName[fullName.Length - 1];

                using (SqlConnection cnn = new SqlConnection("context connection=true"))
                {
                    string colNames = GetTableColumns(dt);
                    string cmdText = "INSERT INTO " + tblName + " (" + colNames + ")" + "\n";
                    cmdText += " SELECT " + colNames + " FROM @tvpTable ";

                    using (SqlCommand command = new SqlCommand(cmdText, cnn))
                    {
                        cnn.Open();
                        command.Parameters.AddWithValue("@tvpTable", dt);
                        command.Parameters[command.Parameters.Count - 1].TypeName = "MATRIX.TVP_" + partialTableName;
                        command.Parameters[command.Parameters.Count - 1].SqlDbType = SqlDbType.Structured;
                        command.ExecuteNonQuery();
                        cnn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                retValue = false;
                throw new Exception(ex.Message);
            }
            return retValue;
        }



        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/ms131092.aspx
        /// </summary>
        /// <param name="input"></param>
        /// <param name="html"></param>
        /// <returns></returns>
        public static SqlDbType DetermineSqlDbType(string input, ref string html)
        {
            SqlDbType retValue = SqlDbType.NVarChar;
            try
            {

                if (Regex.IsMatch(input.ToLower(), "nvarchar"))  //test it
                    retValue = SqlDbType.NVarChar;
                else if (Regex.IsMatch(input.ToLower(), "int"))  //test it 
                    retValue = SqlDbType.Int;
                else if (Regex.IsMatch(input.ToLower(), "char"))
                    retValue = SqlDbType.Char;
                else if (Regex.IsMatch(input.ToLower(), "decimal")) //test it 
                    retValue = SqlDbType.Decimal;
                else if (Regex.IsMatch(input.ToLower(), "datetime")) // test it
                    retValue = SqlDbType.DateTime;
                else if (Regex.IsMatch(input.ToLower(), "date"))
                    retValue = SqlDbType.Date;
                else if (Regex.IsMatch(input.ToLower(), "bit")) //test it
                    retValue = SqlDbType.Bit;
                else if (Regex.IsMatch(input.ToLower(), "bigint")) // test it
                    retValue = SqlDbType.BigInt;
                else if (Regex.IsMatch(input.ToLower(), "binary"))
                    retValue = SqlDbType.Binary;
                else if (Regex.IsMatch(input.ToLower(), "datetime2"))
                    retValue = SqlDbType.DateTime2;
                else if (Regex.IsMatch(input.ToLower(), "datetimeoffset"))
                    retValue = SqlDbType.DateTimeOffset;
                else if (Regex.IsMatch(input.ToLower(), "float")) // test it
                    retValue = SqlDbType.Float;
                else if (Regex.IsMatch(input.ToLower(), "image"))
                    retValue = SqlDbType.Image;
                else if (Regex.IsMatch(input.ToLower(), "money"))
                    retValue = SqlDbType.Money;
                else if (Regex.IsMatch(input.ToLower(), "nchar")) //test it
                    retValue = SqlDbType.NChar;
                else if (Regex.IsMatch(input.ToLower(), "ntext"))
                    retValue = SqlDbType.NText;
                else if (Regex.IsMatch(input.ToLower(), "real"))
                    retValue = SqlDbType.Real;
                else if (Regex.IsMatch(input.ToLower(), "smalldatetime"))
                    retValue = SqlDbType.SmallDateTime;
                else if (Regex.IsMatch(input.ToLower(), "smallint")) //test it
                    retValue = SqlDbType.SmallInt;
                else if (Regex.IsMatch(input.ToLower(), "smallmoney"))
                    retValue = SqlDbType.SmallMoney;
                else if (Regex.IsMatch(input.ToLower(), "structed"))
                    retValue = SqlDbType.Structured;
                else if (Regex.IsMatch(input.ToLower(), "text"))
                    retValue = SqlDbType.Text;
                else if (Regex.IsMatch(input.ToLower(), "time"))
                    retValue = SqlDbType.Time;
                else if (Regex.IsMatch(input.ToLower(), "timestamp"))
                    retValue = SqlDbType.Timestamp;
                else if (Regex.IsMatch(input.ToLower(), "tinyint"))
                    retValue = SqlDbType.TinyInt;
                else if (Regex.IsMatch(input.ToLower(), "uniqueidentifier")) // test it
                    retValue = SqlDbType.UniqueIdentifier;
                else if (Regex.IsMatch(input.ToLower(), "varbinary"))
                    retValue = SqlDbType.VarBinary;
                else if (Regex.IsMatch(input.ToLower(), "varchar"))
                    retValue = SqlDbType.VarChar;
                else if (Regex.IsMatch(input.ToLower(), "variant"))
                    retValue = SqlDbType.Variant;
                else if (Regex.IsMatch(input.ToLower(), "xml"))
                    retValue = SqlDbType.Xml;



            }
            catch (Exception ex)
            {
                html += ex.Message;
            }

            return retValue;
        }


        /// <summary>
        /// How I determine the parameter size
        /// </summary>
        /// <param name="size"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static int DeterminSize(string size, ref byte scale)
        {
            var retValue = 0;
            var splitter = size.Split(',');
            if (splitter.Length >= 1)
            {
                var refer = int.TryParse(splitter[0], out var outValue);
                if (refer)
                    retValue = outValue;
            }
            if (splitter.Length == 2)
            {
                var res = byte.TryParse(splitter[1], out var ref1);
                if (res)
                    scale = ref1;
            }
            return retValue;
        }


        /// <summary>
        /// That the way how to make parameters collection
        /// </summary>
        /// <param name="value"></param>
        /// <param name="errorText"></param>
        /// <returns></returns>
        public static SqlParameter[] MakeParams(string value, ref string errorText)
        {
            SqlParameter[] sp = null;
            try
            {
                var splitter = Regex.Replace(value, "\r\n", "", RegexOptions.IgnoreCase).Split(',');
                var pureString = new Dictionary<int, string>();
                for (var i = 0; i < splitter.Length; i++)
                {
                    if (splitter[i].Contains("@") == false && i > 0)
                    {
                        pureString[i - 1] += "," + splitter[i];
                        continue;
                    }
                    pureString.Add(i, splitter[i].Trim());
                }

                var counter = 0;
                foreach (var s in pureString.Values)
                {
                    var s1 = new SqlParameter
                    {
                        ParameterName = s.Trim().Substring(0, s.Trim().IndexOf(' '))
                    };
                    string[] valueSpliiter = s.Split('=');
                    if (valueSpliiter.Length > 1)
                    {
                        var tester = valueSpliiter[0].Substring(valueSpliiter[0].IndexOf(' '),
                            valueSpliiter[0].Length - valueSpliiter[0].IndexOf(' '));
                        s1.SqlDbType = DetermineSqlDbType(tester, ref errorText);
                        if (tester.Contains("("))
                        {
                            var pomValue = Regex.Replace(tester, " ", "", RegexOptions.IgnoreCase);
                            var pos = pomValue.IndexOf("(", StringComparison.Ordinal);
                            var size = pomValue.Substring( pos + 1, pomValue.IndexOf(")", StringComparison.Ordinal) - pos - 1);
                            byte scale = 0;
                            var sizeTester = DeterminSize(size, ref scale);
                            if (sizeTester != 0)
                                s1.Size = sizeTester;
                            if (scale != 0)
                                s1.Scale = scale;

                        }
                        DetermineValue(valueSpliiter[1], ref s1);

                    }
                    if (sp == null)
                        sp = new SqlParameter[pureString.Values.Count];
                    sp[counter] = s1;
                    counter++;



                }

            }
            catch (Exception ex)
            {
                sp = null;
                errorText += ex.Message;
            }
            return sp;

        }


        /// <summary>
        /// That's the way how to determine parameter value
        /// </summary>
        /// <param name="valueSplitter"></param>
        /// <param name="s1"></param>
        private static void DetermineValue(string valueSplitter, ref SqlParameter s1)
        {
            if (s1.SqlDbType == SqlDbType.Int)
            {
                Int32 j;
                bool succ = Int32.TryParse(valueSplitter, out j);
                if (succ)
                    s1.Value = j;
            }
            else if (s1.SqlDbType == SqlDbType.SmallInt)
            {
                Int16 j;
                bool succ = Int16.TryParse(valueSplitter, out j);
                if (succ)
                    s1.Value = j;
            }
            else if (s1.SqlDbType == SqlDbType.BigInt)
            {
                Int64 j;
                bool succ = Int64.TryParse(valueSplitter, out j);
                if (succ)
                    s1.Value = j;
            }
            else if (s1.SqlDbType == SqlDbType.Real)
            {
                Single j;
                bool succ = Single.TryParse(valueSplitter, out j);
                if (succ)
                    s1.Value = j;
            }
            else if (s1.SqlDbType == SqlDbType.Decimal)
            {
                decimal j;
                bool succ = Decimal.TryParse(valueSplitter.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator), out j);
                if (succ)
                    s1.Value = j;
            }
            else if (s1.SqlDbType == SqlDbType.Float)
            {
                double j;
                bool succ = Double.TryParse(valueSplitter.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator), out j);
                if (succ)
                    s1.Value = j;
            }

            else if (s1.SqlDbType == SqlDbType.Date)
            {
                string valueString = Regex.Replace(valueSplitter, "'", "", RegexOptions.IgnoreCase).Trim();
                s1.Value = DateTime.ParseExact(valueString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            else if (s1.SqlDbType == SqlDbType.DateTime || s1.SqlDbType == SqlDbType.SmallDateTime)
            {
                string valueString = Regex.Replace(valueSplitter, "'", "", RegexOptions.IgnoreCase).Trim();
                s1.Value = DateTime.ParseExact(valueString, "yyyy-MM-dd hh:mm:ss.fff", CultureInfo.InvariantCulture);
            }
            else if (s1.SqlDbType == SqlDbType.UniqueIdentifier)
            {
                string valueString = Regex.Replace(valueSplitter, "'", "", RegexOptions.IgnoreCase).Trim();
                s1.Value = new Guid(valueString);
            }
            else if (s1.SqlDbType == SqlDbType.Bit) // Bit to boolean
            {
                string valueString = Regex.Replace(valueSplitter, "'", "", RegexOptions.IgnoreCase).Trim();
                var succ = bool.TryParse(valueString, out var result);
                if (succ)
                    s1.Value = result;

            }

            else
            {
                string valueString = valueSplitter.Trim();
                if (valueString.StartsWith("'"))
                    valueString = valueString.Substring(1);

                if (valueString.EndsWith("'"))
                    valueString = valueString.Substring(0, valueString.Length - 1);
                s1.Value = valueString;


            }

        }

        /// <summary>
        /// That's the way how to build list of the table columns
        /// </summary>
        /// <param name="table">The datatable object</param>
        /// <returns></returns>
        public static string GetTableColumns(DataTable table)
        {
            string sqlsc = null;
            for (int i = 0; i < table.Columns.Count; i++)
            {
                sqlsc += "\n [" + table.Columns[i].ColumnName + "],";
            }
            return sqlsc.Substring(0, sqlsc.Length - 1) + "\n";

        }

        /// <summary>
        /// That's the way how to build DDL statement to create table
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static string CreateTable(string tableName, DataTable table)
        {
            var sqlsc = tableName.StartsWith("#") == false
                ? "IF OBJECT_ID('{0}', 'U') IS NOT NULL" + "\n"
                : "IF OBJECT_ID('tempdb.dbo.{0}', 'U') IS NOT NULL" + "\n";

            sqlsc += "DROP TABLE {0};" + "\n";
            sqlsc = string.Format(sqlsc, tableName);
            sqlsc += "CREATE TABLE " + tableName + "(";
            for (int i = 0; i < table.Columns.Count; i++)
            {
                sqlsc += "\n [" + table.Columns[i].ColumnName + "] ";
                string columnType = table.Columns[i].DataType.ToString();
                switch (columnType)
                {
                    case "System.Int32":
                        sqlsc += " int ";
                        break;
                    case "System.Int64":
                        sqlsc += " bigint ";
                        break;
                    case "System.Int16":
                        sqlsc += " smallint";
                        break;
                    case "System.Byte":
                        sqlsc += " tinyint";
                        break;
                    case "System.Decimal":
                        sqlsc += " decimal ";
                        break;
                    case "System.DateTime":
                        sqlsc += " datetime ";
                        break;
                    case "System.String":
                    default:
                        sqlsc +=
                            $" nvarchar({(table.Columns[i].MaxLength == -1 ? "max" : table.Columns[i].MaxLength.ToString())}) ";
                        break;
                }
                if (table.Columns[i].AutoIncrement)
                    sqlsc += $" IDENTITY({table.Columns[i].AutoIncrementSeed},{table.Columns[i].AutoIncrementStep}) ";
                if (!table.Columns[i].AllowDBNull)
                    sqlsc += " NOT NULL ";
                sqlsc += ",";
            }
            return sqlsc.Substring(0, sqlsc.Length - 1) + "\n)";
        }


        /// <summary>
        /// That's the way how to build DDL statement to create table value type
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static string CreateType(string tableName, DataTable table)
        {
            var sqlsc = "IF EXISTS (SELECT * FROM sys.types st JOIN sys.schemas ss ON st.schema_id = ss.schema_id" + "\n";
            sqlsc += $"WHERE st.name = N\'TVP_{tableName}\' AND ss.name = N\'MATRIX\')\n";
            sqlsc += "DROP TYPE MATRIX.TVP_{0};" + "\n";

            sqlsc = string.Format(sqlsc, tableName);
            sqlsc += "CREATE TYPE MATRIX.TVP_" + tableName + " AS TABLE (";
            for (var i = 0; i < table.Columns.Count; i++)
            {
                sqlsc += "\n [" + table.Columns[i].ColumnName + "] ";
                string columnType = table.Columns[i].DataType.ToString();
                switch (columnType)
                {
                    case "System.Int32":
                        sqlsc += " int ";
                        break;
                    case "System.Int64":
                        sqlsc += " bigint ";
                        break;
                    case "System.Int16":
                        sqlsc += " smallint";
                        break;
                    case "System.Byte":
                        sqlsc += " tinyint";
                        break;
                    case "System.Decimal":
                        sqlsc += " decimal ";
                        break;
                    case "System.DateTime":
                        sqlsc += " datetime ";
                        break;
                    case "System.String":
                    default:
                        sqlsc +=
                            $" nvarchar({(table.Columns[i].MaxLength == -1 ? "max" : table.Columns[i].MaxLength.ToString())}) ";
                        break;
                }
                if (!table.Columns[i].AllowDBNull)
                    sqlsc += " NOT NULL ";
                sqlsc += ",";
            }
            return sqlsc.Substring(0, sqlsc.Length - 1) + "\n)";
        }



    }
}
