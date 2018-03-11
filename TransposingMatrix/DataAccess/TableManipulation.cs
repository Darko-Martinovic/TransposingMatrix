using System;
using System.Data;
using System.Text;

namespace TransposingMatrix
{
    public static class TableManipulation
    {

        /// <summary>
        /// Transposing the result with custom or generic header
        /// </summary>
        /// <param name="oldTable">The datatable object to transform</param>
        /// <param name="columnMapping">The custom header</param>
        /// <returns></returns>
        public static DataTable RotateTableWithKeyValue(DataTable oldTable,string columnMapping)
        {
            DataTable newTable = new DataTable();
            DataRow dr = default(DataRow);

            // The column mapping is not provided,so create the generic header
            if ( columnMapping == null )
            {
                //the first column name is key
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
                // if we do not pass "enough" names, fill the rest with generic names
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
                    if (StringExtensions.IsNullOrWhiteSpace(oldTable.Rows[row][col].ToString()))
                    {
                        dr[row + 1] = DBNull.Value;
                        continue;
                    }
                    if (oldTable.Rows[row][col].ToString().Equals("System.Byte[]"))
                        dr[row + 1] = ByteArrayToString((byte[])oldTable.Rows[row][col]);
                    else if (oldTable.Columns[col].DataType.ToString().Equals("System.DateTime"))
                        dr[row + 1] = Convert.ToDateTime(oldTable.Rows[row][col]).ToString("yyyy-MM-dd HH:mm:ss.fff");
                    else
                        dr[row + 1] = oldTable.Rows[row][col];
                }

                newTable.Rows.Add(dr);
            }

            return newTable;
        }

       
        /// <summary>
        /// The classical approach. The rotation is made based on "pco" value
        /// </summary>
        /// <param name="oldTable"></param>
        /// <param name="pco"></param>
        /// <returns></returns>
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

                    if ( StringExtensions.IsNullOrWhiteSpace(oldTable.Rows[row][col].ToString()))
                    {
                        dr[row + 1] =DBNull.Value;
                        continue;
                    }
                    if (oldTable.Rows[row][col].ToString().Equals("System.Byte[]"))
                        dr[row + 1] = ByteArrayToString((byte[])oldTable.Rows[row][col]);
                    else if (oldTable.Columns[col].DataType.ToString().Equals("System.DateTime"))
                        dr[row + 1] = Convert.ToDateTime(oldTable.Rows[row][col]).ToString("yyyy-MM-dd HH:mm:ss.fff");
                    else
                        dr[row + 1] = oldTable.Rows[row][col];
                }


                newTable.Rows.Add(dr);
            }

            return newTable;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:X2}", b);
            return "0x" + hex.ToString();
        }



    }
}
