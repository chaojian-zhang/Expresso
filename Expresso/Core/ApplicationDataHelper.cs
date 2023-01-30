using Expresso.Components;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Expresso.Core
{
    internal static class ApplicationDataHelper
    {
        #region Getters
        public static ApplicationData GetCurrentApplicationData()
        {
            MainWindow window = Application.Current.MainWindow as MainWindow;
            return window.ApplicationData;
        }
        #endregion

        #region Look-ups
        public static ApplicationDataReader FindReaderWithName(this ApplicationData data, string name)
        {
            foreach (ApplicationDataReader reader in data.DataReaders)
                if (reader.Name == name)
                    return reader;
            return null;
        }
        public static ApplicationDataReader FindReaderFromParameters(this ApplicationData data, ExpressorReaderDataQueryParameter parameter)
        {
            foreach (ApplicationDataReader reader in data.DataReaders)
                if (reader.DataQueries.Any(q => q.Parameters == parameter))
                    return reader;
            return null;
        }
        #endregion

        #region Evaluators
        public static string EvaluateTransform(this ApplicationDataReader reader, out ParcelDataGrid dataGrid, out DataTable table)
        {
            List<ParcelDataGrid> intermediateData = new();
            foreach (var query in reader.DataQueries)
                intermediateData.Add(new ParcelDataGrid(query.Name, query.Parameters.MakeQuery(), true));

            if (reader.DataQueries.Count == 0 )
            {
                dataGrid = null;
                table = null;
                return $"Result,Message\nError,Reader has no data queries.";
            }

            string resultCSV = null;
            try
            {
                var query = reader.Transform;
                if (string.IsNullOrEmpty(query))
                    query = $"select * from {reader.DataQueries.Last().Name}";

                dataGrid = intermediateData.ProcessDataGrids(query, out table);
                dataGrid.TableName = table.TableName = reader.Name;
                resultCSV = dataGrid.ToCSV();
            }
            catch (Exception err)
            {
                dataGrid = null;
                table = null;
                resultCSV = $"Result,Message\nError,\"{err.Message.Replace(Environment.NewLine, " ")}\"";
            }

            return resultCSV;
        }
        #endregion
    }
}
