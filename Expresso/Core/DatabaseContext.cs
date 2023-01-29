using Expresso.Components;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expresso.Core
{
    internal class DatabaseContext
    {
        #region Properties

        #endregion

        #region Methods

        #endregion
    }

    internal static class SQLiteHelper
    {
        #region Rudimentary Interface
        public static ParcelDataGrid ProcessDataGrids(this IList<ParcelDataGrid> inputTables, string command, out DataTable dataTable)
        {
            if (inputTables.Select(t => t.TableName).Distinct().Count() != inputTables.Count)
                throw new ArgumentException("Missing Data Table names.");

            using SQLiteConnection connection = new SQLiteConnection("Data Source=:memory:");
            connection.Open();

            foreach (ParcelDataGrid table in inputTables)
                connection.PopulateTable(table);

            // Execute
            string formattedText = command.TrimEnd(';') + ';';
            for (int i = 0; i < inputTables.Count; i++)
                formattedText = formattedText.Replace($"@Table{i + 1}", $"'{inputTables[i].TableName}'"); // Table names can't use parameters, so do it manually

            dataTable = new DataTable();
            dataTable.Load(new SQLiteCommand(command, connection).ExecuteReader());

            connection.Close();
            return new ParcelDataGrid(dataTable);
        }
        #endregion

        #region Helpers
        public static void PopulateTable(this SQLiteConnection connection, ParcelDataGrid table)
        {
            SQLiteCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"CREATE TABLE '{table.TableName}'({string.Join(',', table.Columns.Select(c => $"'{c.Header}'"))})";
            cmd.ExecuteNonQuery();

            // Remark: The API is as shitty as it can get
            using SQLiteTransaction transaction = connection.BeginTransaction();

            string sql = $"select * from '{table.TableName}' limit 1";
            using SQLiteDataAdapter adapter = new SQLiteDataAdapter(sql, connection);
            adapter.InsertCommand = new SQLiteCommandBuilder(adapter).GetInsertCommand();

            DataSet dataSet = new DataSet();
            adapter.FillSchema(dataSet, SchemaType.Source, table.TableName);
            adapter.Fill(dataSet, table.TableName);     // Load exiting table data (will be empty) 

            // Insert data
            DataTable dataTable = dataSet.Tables[table.TableName];
            foreach (ExpandoObject row in table.Rows)
            {
                DataRow dataTableRow = dataTable.NewRow();
                foreach (KeyValuePair<string, dynamic> pair in (IDictionary<string, dynamic>)row)
                    dataTableRow[pair.Key] = pair.Value;
                dataTable.Rows.Add(dataTableRow);
            }
            int result = adapter.Update(dataTable);

            transaction.Commit();
            dataSet.AcceptChanges();

            // Release resources 
            adapter.Dispose();
            dataSet.Clear();
        }
        #endregion
    }
}
