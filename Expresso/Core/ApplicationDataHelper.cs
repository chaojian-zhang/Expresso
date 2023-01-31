using Expresso.Components;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
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
        public static ApplicationDataReader FindReaderFromParameters(this ApplicationData data, ReaderDataQueryParameterBase parameter)
        {
            foreach (ApplicationDataReader reader in data.DataReaders)
                if (reader.DataQueries.Any(q => q.Parameters == parameter))
                    return reader;
            return null;
        }
        public static ApplicationDataQuery FindReaderDataQueryFromParameters(this ApplicationData data, ReaderDataQueryParameterBase parameter)
        {
            foreach (ApplicationDataReader reader in data.DataReaders)
                foreach (var query in reader.DataQueries)
                    if (query.Parameters == parameter)
                        return query;
            return null;
        }
        #endregion

        #region Evaluators
        public static string InterpolateVariables(this string templateString)
        {
            var applicationData = GetCurrentApplicationData();
            var variables = applicationData.Variables;

            return Regex.Replace(templateString, "${(.+?)}", match =>
            {
                string variableName = match.Groups[1].Value;
                ApplicationVariable variable = variables.FirstOrDefault(v => v.Name == variableName);
                if (variable != null)
                    return variable.EvaluateVariable().First();
                else return string.Empty;
            });
        }
        public static string[] EvaluateVariable(this ApplicationVariable variable)
        {
            switch (variable.ValueType)
            {
                case VariableValueType.SingleValue:
                    return new string[] { variable.DefaultValue };
                case VariableValueType.MultiValueArray:
                case VariableValueType.Iterator:
                    switch (variable.SourceType)
                    {
                        case VariableSourceType.Fixed:
                        case VariableSourceType.CustomList:
                            return variable.Source.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        case VariableSourceType.Reader:
                            var reader = GetCurrentApplicationData().FindReaderWithName(variable.Source);
                            if (reader != null)
                            {
                                reader.EvaluateTransform(out ParcelDataGrid data, out _);
                                if (data != null)
                                    return data.Columns[0].GetDataAs<string>().ToArray();
                                else return new string[] { };
                            }
                            else throw new ApplicationException($"Cannot find reader: {variable.Source}");
                        default:
                            throw new ArgumentException($"Invalid source type: {variable.SourceType}.");
                    }
                default:
                    throw new ArgumentException($"Invalid variable type: {variable.ValueType}.");
            }
        }
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
                var transformQuery = reader.Transform;
                if (string.IsNullOrEmpty(transformQuery))
                    transformQuery = $"select * from {reader.DataQueries.Last().Name}";

                dataGrid = intermediateData.ProcessDataGrids(transformQuery, out table);
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
