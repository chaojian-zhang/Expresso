using Expresso.Components;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using Microsoft.Data.Sqlite;

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

            using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            foreach (ParcelDataGrid table in inputTables)
                connection.PopulateTable(table);

            // Execute
            string formattedText = command.TrimEnd(';') + ';';
            for (int i = 0; i < inputTables.Count; i++)
                formattedText = formattedText.Replace($"@Table{i + 1}", $"'{inputTables[i].TableName}'"); // Table names can't use parameters, so do it manually

            dataTable = new DataTable();
            dataTable.Load(new SqliteCommand(command, connection).ExecuteReader());

            connection.Close();
            return new ParcelDataGrid(dataTable);
        }
        #endregion

        #region Helpers
        public static void PopulateTable(this SqliteConnection connection, ParcelDataGrid table)
        {
            SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"CREATE TABLE '{table.TableName}'({string.Join(',', table.Columns.Select(c => $"'{c.Header}'"))})";
            cmd.ExecuteNonQuery();

            InMemorySQLIte.InsertDbData(connection, table.TableName, table);
        }
        #endregion
    }
}
