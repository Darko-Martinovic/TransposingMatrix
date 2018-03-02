using System.Data;

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





    }
}
