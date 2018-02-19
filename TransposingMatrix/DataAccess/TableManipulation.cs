using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace TransposingMatrix
{
    public static class TableManipulation
    {
        public static DataTable RotateTableWithKeyValue(DataTable oldTable,string columnMapping)
        {
            DataTable newTable = new DataTable();
            DataRow dr = default(DataRow);


            if ( columnMapping == null )
            {
                newTable.Columns.Add("Key");
                int no = 0;
                foreach (DataRow row in oldTable.Rows)
                {
                    newTable.Columns.Add("Value" + (no == 0 ? "" : no.ToString().Trim()));
                    no++;
                }
            }
            else
            {
                string[] names = columnMapping.Split(',');
                foreach (string s in names)
                {
                    newTable.Columns.Add(s);
                }

                if ( names.Length < oldTable.Rows.Count+1)
                {
                    int no = names.Length;
                    while (no < oldTable.Rows.Count+1)
                    {
                        newTable.Columns.Add("Value" + no.ToString().Trim());
                        no++;
                    }
                }


            }
            for (int col = 0; col <= oldTable.Columns.Count - 1; col++)
            {

                dr = newTable.NewRow();


                dr[0] = oldTable.Columns[col].ColumnName;


                for (int row = 0; row <= oldTable.Rows.Count - 1; row++)
                {
                    dr[row + 1] = oldTable.Rows[row][col];
                }

                newTable.Rows.Add(dr);
            }

            return newTable;
        }

        public static DataTable RotateTable(DataTable oldTable, int pco = 0)
        {
            DataTable newTable = new DataTable();
            DataRow dr = default(DataRow);

            if (pco > oldTable.Columns.Count)
            {
                pco = 0;
            }
            newTable.Columns.Add(oldTable.Columns[pco].ColumnName);
            int counter = 0;
            foreach (DataRow row in oldTable.Rows)
            {
                string newName = row[pco].ToString();
                if (newTable.Columns.Contains(newName))
                {
                    newName += "_" + counter.ToString().Trim();
                }
                newTable.Columns.Add(newName);
                counter += 1;
            }

            for (int col = 0; col <= oldTable.Columns.Count - 1; col++)
            {

                if (col == pco)
                    continue;

                dr = newTable.NewRow();


                dr[0] = oldTable.Columns[col].ColumnName;


                for (int row = 0; row <= oldTable.Rows.Count - 1; row++)
                {
                    dr[row + 1] = oldTable.Rows[row][col];
                }


                newTable.Rows.Add(dr);
            }

            return newTable;
        }

        public static string CreateTABLE(string tableName, DataTable table)
        {
            string sqlsc;
            sqlsc = "CREATE TABLE " + tableName + "(";
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
                        sqlsc += string.Format(" nvarchar({0}) ", table.Columns[i].MaxLength == -1 ? "max" : table.Columns[i].MaxLength.ToString());
                        break;
                }
                if (table.Columns[i].AutoIncrement)
                    sqlsc += " IDENTITY(" + table.Columns[i].AutoIncrementSeed.ToString() + "," + table.Columns[i].AutoIncrementStep.ToString() + ") ";
                if (!table.Columns[i].AllowDBNull)
                    sqlsc += " NOT NULL ";
                sqlsc += ",";
            }
            return sqlsc.Substring(0, sqlsc.Length - 1) + "\n)";
        }

    }
}
