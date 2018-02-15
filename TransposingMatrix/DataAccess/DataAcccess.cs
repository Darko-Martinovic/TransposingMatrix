using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TransposingMatrix
{
    class DataAccess
    {
        public readonly List<object[]> data = new List<object[]>();
        public int rowCount = 0;
        public int fieldCount = 0;
        public List<object[]> GetData(string Query, bool isSp, SqlParameter[] listOfParams, ref string html)
        {
            try
            {
                using (SqlConnection cnn = new SqlConnection("context connection=true"))
                {
                    using (SqlCommand command = new SqlCommand(Query, cnn))
                    {
                        command.Parameters.Clear();
                        cnn.Open();
                        if (isSp)
                            command.CommandType = CommandType.StoredProcedure;
                        if (listOfParams != null)
                        {
                            foreach (SqlParameter p in listOfParams)
                                command.Parameters.Add(p);
                        }
                        SqlDataReader dr = command.ExecuteReader();

                        fieldCount = dr.FieldCount;
                        object[] o = new object[fieldCount];
                        for (int i = 0; i < fieldCount; i++)
                            o[i] = dr.GetName(i);

                        data.Add(o);
                        rowCount++;

                        while (dr.Read())
                        {
                            object[] o1 = new object[fieldCount];
                            dr.GetValues(o1);
                            data.Add(o1);
                            rowCount++;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                html += ex.Message;
            }
            return data;
        }



        public static DataSet GetDataSet(string Query, bool isSp, SqlParameter[] listOfParams, ref string errorText)
        {
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection cnn = new SqlConnection("context connection=true"))
                {
                    using (SqlCommand command = new SqlCommand(Query, cnn))
                    {
                        cnn.Open();
                        if (isSp)
                            command.CommandType = CommandType.StoredProcedure;
                        if (listOfParams != null)
                        {
                            foreach (SqlParameter p in listOfParams)
                            {
                                command.Parameters.Add(p);
                            }
                        }
                        using (SqlDataAdapter sqlAdp = new SqlDataAdapter())
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


        public static string GetResult(string Query)
        {
            string ds = null;
            try
            {
                using (SqlConnection cnn = new SqlConnection("context connection=true"))
                {
                    using (SqlCommand command = new SqlCommand(Query, cnn))
                    {
                        cnn.Open();
                        ds = command.ExecuteScalar().ToString();
                        cnn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ds = ex.Message;
            }
            return ds;
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

        public static int DeterminSize(string size, ref byte scale)
        {
            int retValue = 0;
            string[] splitter = size.Split(',');
            if (splitter.Length >= 1)
            {
                int outValue = 0;
                bool refer = Int32.TryParse(splitter[0], out outValue);
                if (refer)
                    retValue = outValue;
            }
            if (splitter.Length == 2)
            {
                byte ref1 = 0;
                bool res = Byte.TryParse(splitter[1], out ref1);
                if (res)
                    scale = ref1;
            }
            return retValue;
        }

        public static SqlParameter[] MakeParams(string value, ref string html)
        {
            SqlParameter[] sp = null;
            try
            {
                string[] splitter = Regex.Replace(value, "\r\n", "", RegexOptions.IgnoreCase).Split(',');
                Dictionary<int, string> pureString = new Dictionary<int, string>();
                for (int i = 0; i < splitter.Length; i++)
                {
                    if (splitter[i].Contains("@") == false && i > 0)
                    {
                        pureString[i - 1] += "," + splitter[i];
                        continue;
                    }
                    pureString.Add(i, splitter[i].Trim());
                }

                int counter = 0;
                foreach (string s in pureString.Values)
                {
                    SqlParameter s1 = new SqlParameter();
                    s1.ParameterName = s.Trim().Substring(0, s.Trim().IndexOf(' '));
                    string[] valueSpliiter = s.Split('=');
                    if (valueSpliiter.Length > 1)
                    {
                        string tester = valueSpliiter[0].Substring(valueSpliiter[0].IndexOf(' '), valueSpliiter[0].Length - valueSpliiter[0].IndexOf(' '));
                        s1.SqlDbType = DetermineSqlDbType(tester, ref html);
                        if (tester.Contains("("))
                        {
                            string pomValue = Regex.Replace(tester, " ", "", RegexOptions.IgnoreCase);
                            string size = pomValue.Substring(pomValue.IndexOf("(") + 1, pomValue.IndexOf(")") - pomValue.IndexOf("(") - 1);
                            byte scale = 0;
                            int sizeTester = DeterminSize(size, ref scale);
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
                html += ex.Message;
            }
            return sp;

        }



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
                bool result;
                string valueString = Regex.Replace(valueSplitter, "'", "", RegexOptions.IgnoreCase).Trim();
                bool succ = Boolean.TryParse(valueString, out result);
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

        // Method to Execute Query

    }
}
