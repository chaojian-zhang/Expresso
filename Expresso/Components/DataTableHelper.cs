/* Author: Charles Zhang 📧Email: charles_zhang@totalimagine.com
 * Version: v0.1.1
 * ALL MODIFICATIONS MUST INCREMENT VERSION NUMBER.
 * 
 * # Version Changes
 * 
 * - Pre v0.1.0: Draft definition
 * - v0.1.1 (2023-02-24): Merge changes from Expresso project.
 */

using ConsoleTables;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace DataHelpers
{
    public static class DataTableHelper
    {
        #region Conversion
        public static string ToCSV(this DataTable dt)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dt.Columns
                .Cast<DataColumn>()
                .Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field =>
                {
                    string value = field.ToString();
                    string escapeQuotes = value.Contains('"')
                        ? string.Concat("\"", value.Replace("\"", "\"\""), "\"")
                        : value;
                    string addQuotes = escapeQuotes.Contains(',') ? $"\"{escapeQuotes}\"" : escapeQuotes;
                    return addQuotes;
                });
                sb.AppendLine(string.Join(",", fields));
            }

            return sb.ToString();
        }
        #endregion

        #region Display
        public static string ToCSV2(this DataTable dataTable)
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(string.Join(",", Enumerable.Range(0, dataTable.Columns.Count).Select(i => dataTable.Columns[i].ColumnName.Replace(" ", string.Empty))));
            foreach (DataRow row in dataTable.Rows)
            {
                IEnumerable<string> items = row.ItemArray.Select(item => $"\"{item.ToString().Replace("\"", "\"\"")}\"");
                string csvLine = string.Join(",", items);
                output.AppendLine(csvLine);
            }
            return output.ToString();
        }
        public static string ToCSVFull(this DataTable dataTable)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dataTable.Columns
                .Cast<System.Data.DataColumn>()
                .Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dataTable.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field =>
                {
                    string value = field.ToString();
                    string escapeQuotes = value.Contains('"')
                        ? string.Concat("\"", value.Replace("\"", "\"\""), "\"")
                        : value;
                    string addQuotes = escapeQuotes.Contains(',') ? $"\"{escapeQuotes}\"" : escapeQuotes;
                    return addQuotes;
                });
                sb.AppendLine(string.Join(",", fields));
            }

            return sb.ToString();
        }
        public static string Print(this DataTable dataTable)
        {
            var consoleTable = new ConsoleTable();
            var columns = Enumerable.Range(0, dataTable.Columns.Count).Select(i => dataTable.Columns[i].ColumnName).ToArray();
            consoleTable.AddColumn(columns);

            foreach (DataRow dr in dataTable.Rows)
            {
                var items = dr.ItemArray.Select(i => i.ToString()).ToArray();
                consoleTable.AddRow(items);
            }
            consoleTable.Write();
            return consoleTable.ToString();
        }
        #endregion
    }
}
