using Expresso.Components;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Dynamic;
using System.Linq;

namespace Expresso.Core
{
    public static class SQLite
    {
        #region Rudimentary Interface
        public static ParcelDataGrid ProcessDataGrids(this ParcelDataGrid[] inputTables, string command)
        {
            if (inputTables.Select(t => t.TableName).Distinct().Count() != inputTables.Length)
                throw new ArgumentException("Missing Data Table names.");

            using SQLiteConnection connection = new SQLiteConnection("Data Source=:memory:");
            connection.Open();

            foreach (ParcelDataGrid table in inputTables)
                connection.PopulateTable(table);

            // Execute
            string formattedText = command.TrimEnd(';') + ';';
            for (int i = 0; i < inputTables.Length; i++)
                formattedText = formattedText.Replace($"@Table{i + 1}", $"'{inputTables[i].TableName}'"); // Table names can't use parameters, so do it manually
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(formattedText, connection);
            DataSet result = new();
            adapter.Fill(result);

            connection.Close();
            return new ParcelDataGrid(result);
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
