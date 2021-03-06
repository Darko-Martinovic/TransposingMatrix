using System;
using System.Data;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace TransposingMatrix
{
    public static class PipeUtilities
    {


        /// <summary>
        /// Send the result back to the client
        /// </summary>
        /// <param name="dt"></param>
        public static void PipeDataTable(DataTable dt)
        {

            var md = ExtractDataTableColumnMetaData(dt);
            var r = new SqlDataRecord(md);
            var p = SqlContext.Pipe;

            //---------------------------------------------------------------------------------------
            //  First is invoked ‘SendResultsStart.’ 
            // ‘SendResultStart’ marks the beginning of a result set to be sent back to the client, 
            //  and uses the r parameter to construct the metadata that describes the result set
            //---------------------------------------------------------------------------------------
            p?.SendResultsStart(r);
            try
            {
                foreach (DataRow row in dt.Rows)
                {
                    for (var i = 0; i < r.FieldCount; i++)
                    {
                        var v = row[i];
                        v = DBNull.Value != v ? (object) v.ToString() : DBNull.Value;
                        r.SetValue(i, v);
                    }

                    p?.SendResultsRow(r);
                }
            }
            finally
            {
                //
                // 'SendResultEnd’ marks the end of a result set and returns the SqlPipe instance to the initial state.
                //
                p?.SendResultsEnd();
            }
        }


        /// <summary>
        /// Find out metadata ( nvarchar )
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private static SqlMetaData[] ExtractDataTableColumnMetaData(DataTable dt)
        {
            var md = new SqlMetaData[dt.Columns.Count];
            for (var index = 0; index < dt.Columns.Count; index++)
            {
                var column = dt.Columns[index];
                md[index] = SqlMetaDataFromColumn(column);
            }

            return md;
        }

        private static SqlMetaData SqlMetaDataFromColumn(DataColumn column)
        {
            SqlMetaData smd = null;
            var clrType = column.DataType;
            var name = column.ColumnName;
            switch (Type.GetTypeCode(clrType))
            {
                case TypeCode.String:
                    smd = new SqlMetaData(name, SqlDbType.NVarChar, column.MaxLength);
                    break;
                case TypeCode.Boolean: smd = new SqlMetaData(name, SqlDbType.Bit); break;
                case TypeCode.Byte: smd = new SqlMetaData(name, SqlDbType.TinyInt); break;
                case TypeCode.Char: smd = new SqlMetaData(name, SqlDbType.NVarChar, 1); break;
                case TypeCode.DateTime: smd = new SqlMetaData(name, SqlDbType.DateTime); break;
                case TypeCode.Decimal: smd = new SqlMetaData(name, SqlDbType.Decimal, 18, 0); break;
                case TypeCode.Double: smd = new SqlMetaData(name, SqlDbType.Float); break;
                case TypeCode.Int16: smd = new SqlMetaData(name, SqlDbType.SmallInt); break;
                case TypeCode.Int32: smd = new SqlMetaData(name, SqlDbType.Int); break;
                case TypeCode.Int64: smd = new SqlMetaData(name, SqlDbType.BigInt); break;
                case TypeCode.Single: smd = new SqlMetaData(name, SqlDbType.Real); break;
                default: throw UnknownDataType(clrType);
            }

            return smd;
        }

        private static Exception UnknownDataType(Type clrType)
        {
            return new ArgumentException("Unknown type: " + clrType);
        }




    }
}