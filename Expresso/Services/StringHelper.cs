using ConsoleTables;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Expresso.Services
{
    public static class StringHelper
    {
        public static string ToConsoleTable(this DataTable dt)
        {
            string[] columnNames = dt.Columns
                .Cast<DataColumn>()
                .Select(column => column.ColumnName)
                .ToArray();
            var consoleTable = new ConsoleTable(columnNames);

            foreach (DataRow row in dt.Rows)
            {
                string[] fields = row.ItemArray.Select(field =>
                {
                    string value = field.ToString();
                    string escapeQuotes = value.Contains('"')
                        ? string.Concat("\"", value.Replace("\"", "\"\""), "\"")
                        : value;
                    string addQuotes = escapeQuotes.Contains(',') ? $"\"{escapeQuotes}\"" : escapeQuotes;
                    return addQuotes;
                }).ToArray();

                consoleTable.AddRow(fields);
            }
            return consoleTable.ToMinimalString();
        }
        public static string CSVToConsoleTable(this string csv)
        {
            try
            {
                var reader = Csv.CsvReader.ReadFromText(csv, new Csv.CsvOptions()
                {
                    HeaderMode = Csv.HeaderMode.HeaderPresent,
                    AllowNewLineInEnclosedFieldValues = true,
                }).ToArray();

                string[] columnNames = reader.First().Headers;
                var consoleTable = new ConsoleTable(columnNames);

                foreach (var row in reader)
                    consoleTable.AddRow(row.Values);
                return consoleTable.ToMinimalString();
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
        public static DataView CSVToDataTable(this string csv)
        {
            var reader = Csv.CsvReader.ReadFromText(csv, new Csv.CsvOptions()
            {
                HeaderMode = Csv.HeaderMode.HeaderPresent,
                AllowNewLineInEnclosedFieldValues = true
            }).ToArray();

            var headers = reader.Length > 0 
                ? reader.First().Headers
                : csv.Trim().Split(',');
            DataTable dataTable = new DataTable();
            foreach (string header in headers)
                dataTable.Columns.Add(new DataColumn(EscapeDataGridViewInvalidCharacters(header), typeof(string)));

            foreach (Csv.ICsvLine row in reader)
            {
                DataRow dataRow = dataTable.NewRow();
                dataRow.ItemArray = row.Values;
                dataTable.Rows.Add(dataRow);
            }

            return new DataView(dataTable);

            static string EscapeDataGridViewInvalidCharacters(string header)
            {
                var invalidChar = "()[]!@#$%^&*-_=\\/,. ".ToArray();
                if (invalidChar.Any(c => header.Contains(c)))
                    return $"\"{header}\"";
                else return header;
            }
        }
    }
}
