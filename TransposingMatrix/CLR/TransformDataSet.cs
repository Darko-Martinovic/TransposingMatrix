using System;
using System.Data;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace TransposingMatrix
{
    public static class DataSetUtilities
    {

        public static void PipeDataSet(DataSet ds)
        {
            if (ds == null)
            {
                throw new ArgumentException("PipeDataSet requires a non-null data set.");
            }
            else
            {
                foreach (DataTable dt in ds.Tables)
                {
                    PipeDataTable(dt);
                }
            }
        }


        public static void PipeDataTable(DataTable dt)
        {
            bool[] coerceToString;  
            SqlMetaData[] metaData = ExtractDataTableColumnMetaData(dt, out coerceToString);

            SqlDataRecord record = new SqlDataRecord(metaData);
            SqlPipe pipe = SqlContext.Pipe;
            pipe.SendResultsStart(record);
            try
            {
                foreach (DataRow row in dt.Rows)
                {
                    for (int index = 0; index < record.FieldCount; index++)
                    {
                        object value = row[index];
                        if (null != value && coerceToString[index])
                            value = value.ToString();
                        record.SetValue(index, value);
                    }

                    pipe.SendResultsRow(record);
                }
            }
            finally
            {
                pipe.SendResultsEnd();
            }
        }

        private static SqlMetaData[] ExtractDataTableColumnMetaData(DataTable dt, out bool[] coerceToString)
        {
            SqlMetaData[] metaDataResult = new SqlMetaData[dt.Columns.Count];
            coerceToString = new bool[dt.Columns.Count];
            for (int index = 0; index < dt.Columns.Count; index++)
            {
                DataColumn column = dt.Columns[index];
                metaDataResult[index] = SqlMetaDataFromColumn(column, out coerceToString[index]);
            }

            return metaDataResult;
        }

        private static Exception InvalidDataTypeCode(TypeCode code)
        {
            return new ArgumentException("Invalid type: " + code);
        }

        private static Exception UnknownDataType(Type clrType)
        {
            return new ArgumentException("Unknown type: " + clrType);
        }

        private static SqlMetaData SqlMetaDataFromColumn(DataColumn column, out bool coerceToString)
        {
            coerceToString = false;
            SqlMetaData smd = null;
            Type clrType = column.DataType;
            string name = column.ColumnName;
            switch (Type.GetTypeCode(clrType))
            {
                case TypeCode.Boolean: smd = new SqlMetaData(name, SqlDbType.Bit); break;
                case TypeCode.Byte: smd = new SqlMetaData(name, SqlDbType.TinyInt); break;
                case TypeCode.Char: smd = new SqlMetaData(name, SqlDbType.NVarChar, 1); break;
                case TypeCode.DateTime: smd = new SqlMetaData(name, SqlDbType.DateTime); break;
                case TypeCode.DBNull: throw InvalidDataTypeCode(TypeCode.DBNull);
                case TypeCode.Decimal: smd = new SqlMetaData(name, SqlDbType.Decimal, 18, 0); break;
                case TypeCode.Double: smd = new SqlMetaData(name, SqlDbType.Float); break;
                case TypeCode.Empty: throw InvalidDataTypeCode(TypeCode.Empty);
                case TypeCode.Int16: smd = new SqlMetaData(name, SqlDbType.SmallInt); break;
                case TypeCode.Int32: smd = new SqlMetaData(name, SqlDbType.Int); break;
                case TypeCode.Int64: smd = new SqlMetaData(name, SqlDbType.BigInt); break;
                case TypeCode.SByte: throw InvalidDataTypeCode(TypeCode.SByte);
                case TypeCode.Single: smd = new SqlMetaData(name, SqlDbType.Real); break;
                case TypeCode.String:
                    smd = new SqlMetaData(name, SqlDbType.NVarChar, column.MaxLength);
                    break;
                case TypeCode.UInt16: throw InvalidDataTypeCode(TypeCode.UInt16);
                case TypeCode.UInt32: throw InvalidDataTypeCode(TypeCode.UInt32);
                case TypeCode.UInt64: throw InvalidDataTypeCode(TypeCode.UInt64);
                case TypeCode.Object:
                    smd = SqlMetaDataFromObjectColumn(name, column, clrType);
                    if (smd == null)
                    {
                        // Unknown type, try to treat it as string;
                        smd = new SqlMetaData(name, SqlDbType.NVarChar, column.MaxLength);
                        coerceToString = true;
                    }
                    break;

                default: throw UnknownDataType(clrType);
            }

            return smd;
        }

        private static SqlMetaData SqlMetaDataFromObjectColumn(string name, DataColumn column, Type clrType)
        {
            SqlMetaData smd = null;
            if (clrType == typeof(System.Byte[]) || clrType == typeof(SqlBinary) || clrType == typeof(SqlBytes) ||
        clrType == typeof(System.Char[]) || clrType == typeof(SqlString) || clrType == typeof(SqlChars))
                smd = new SqlMetaData(name, SqlDbType.VarBinary, column.MaxLength);
            else if (clrType == typeof(System.Guid))
                smd = new SqlMetaData(name, SqlDbType.UniqueIdentifier);
            else if (clrType == typeof(System.Object))
                smd = new SqlMetaData(name, SqlDbType.Variant);
            else if (clrType == typeof(SqlBoolean))
                smd = new SqlMetaData(name, SqlDbType.Bit);
            else if (clrType == typeof(SqlByte))
                smd = new SqlMetaData(name, SqlDbType.TinyInt);
            else if (clrType == typeof(SqlDateTime))
                smd = new SqlMetaData(name, SqlDbType.DateTime);
            else if (clrType == typeof(SqlDouble))
                smd = new SqlMetaData(name, SqlDbType.Float);
            else if (clrType == typeof(SqlGuid))
                smd = new SqlMetaData(name, SqlDbType.UniqueIdentifier);
            else if (clrType == typeof(SqlInt16))
                smd = new SqlMetaData(name, SqlDbType.SmallInt);
            else if (clrType == typeof(SqlInt32))
                smd = new SqlMetaData(name, SqlDbType.Int);
            else if (clrType == typeof(SqlInt64))
                smd = new SqlMetaData(name, SqlDbType.BigInt);
            else if (clrType == typeof(SqlMoney))
                smd = new SqlMetaData(name, SqlDbType.Money);
            else if (clrType == typeof(SqlDecimal))
                smd = new SqlMetaData(name, SqlDbType.Decimal, SqlDecimal.MaxPrecision, 0);
            else if (clrType == typeof(SqlSingle))
                smd = new SqlMetaData(name, SqlDbType.Real);
            else if (clrType == typeof(SqlXml))
                smd = new SqlMetaData(name, SqlDbType.Xml);
            else
                smd = null;

            return smd;
        }



    }
}