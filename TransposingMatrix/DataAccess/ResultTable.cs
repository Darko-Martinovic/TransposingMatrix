using System.Collections.Generic;

namespace TransposingMatrix
{
    /// <summary>
    /// A result as text: its column names and its rows, with null for NULL.
    /// </summary>
    public sealed class ResultTable
    {
        public ResultTable(IList<string> columns)
        {
            Columns = columns;
        }

        public IList<string> Columns { get; }

        public List<string?[]> Rows { get; } = new List<string?[]>();

        /// <summary>True when the query returned more rows than were read.</summary>
        public bool Truncated { get; set; }
    }
}
