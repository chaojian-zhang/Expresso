using Expresso.Components;
using Microsoft.Data.Sqlite;
using Python.Runtime;
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
    internal static class SQLiteHelper
    {
        #region One-Shot Use
        public static string TransformCSV(string tableName, string csv, string transformQuery, out ParcelDataGrid dataGrid, out DataTable table)
        {
            ParcelDataGrid tableGrid = new(tableName, csv, true);
            try
            {
                dataGrid = new ParcelDataGrid[] { tableGrid }.ProcessDataGrids(transformQuery, out table);
                dataGrid.TableName = table.TableName = tableName;
                return dataGrid.ToCSV();
            }
            catch (Exception err)
            {
                dataGrid = null;
                table = null;
                return $"Result,Message\nError,\"{err.Message.Replace(Environment.NewLine, " ")}\"";
            }
        }
        #endregion

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
            cmd.CommandText = $"CREATE TABLE '{table.TableName}'({string.Join(',', table.Columns.Select(c => $"\"{c.Header}\""))})";
            cmd.ExecuteNonQuery();

            InMemorySQLIte.InsertDbData(connection, table.TableName, table);
        }
        #endregion
    }

    internal class ExpressoApplicationContext
    {
        #region Singleton
        private ExpressoApplicationContext()
        {
        }
        private static ExpressoApplicationContext _Singleton;
        public static ExpressoApplicationContext Context
        {
            get
            {
                if (_Singleton != null) 
                    return _Singleton;
                else throw new InvalidOperationException("Application context not initialized. Make sure initialize context once and only once.");
            }
        }
        public static ExpressoApplicationContext Initialize()
        {
            _Singleton = new ExpressoApplicationContext();
            return _Singleton;
        }
        #endregion

        #region Runtime Data
        private ApplicationData _ApplicationData;
        public static ApplicationData ApplicationData => Context._ApplicationData;
        public void SetCurrentApplicationData(ApplicationData value)
        {
            _ApplicationData = value;
        }
        #endregion
    }

    internal static class ApplicationDataHelper
    {
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
        public static void ExecuteWorkflow(this ApplicationWorkflow workflow, Action<string> statusReportCallback)
        {
            // Remark-cz: Below is but a functional implementation; Aparently much more optimization can be done.

            // Gather permutations of variables
            List<(string Name, string Value)[]> variablePermutations = GatherVariablePermutations();
            if (variablePermutations.Count != 0 && statusReportCallback != null)
            {
                statusReportCallback("Variable Permutations: ");
                statusReportCallback(string.Join(", ", variablePermutations.First().Select(p => p.Name)));
                foreach (var permutation in variablePermutations)
                    statusReportCallback(string.Join(", ", permutation.Select(p => p.Value)));
            }

            // Test: Execute Python
            if (workflow.StartingSteps.Count != 0 && workflow.StartingSteps.First().ActionType == WorkflowActionType.Programmer)
            {
                Runtime.PythonDLL = "python39.dll";
                PythonEngine.Initialize();
                using (Py.GIL())
                {
                    using PyModule scope = Py.CreateScope();
                    scope.Set("input", "Charles");

                    string code = """
                        output = f"Hello: {input}"
                        """;
                    scope.Exec(code);
                    string output = scope.Get<string>("output");
                    MessageBox.Show(output, "Output");
                }
                PythonEngine.Shutdown();
            }
        }
        public static List<(string Name, string Value)[]> GatherVariablePermutations()
        {
            if (ExpressoApplicationContext.ApplicationData.Variables.Count == 0)
                return new List<(string Name, string Value)[]>();

            List<(string Name, string[] Values)> variablePotentialValues = ExpressoApplicationContext.ApplicationData.Variables
                .Select(v => (v.Name, Values: v.EvaluateVariable()))
                .Where(v => v.Values.Length != 0)
                .OrderBy(v => v.Name)
                .ToList();

            List<(string Name, string Value)[]> variablePermutations = new();
            foreach (string value in variablePotentialValues.First().Values)
                IterateNextLevel(new List<string>() { value }, variablePotentialValues, variablePermutations);
            return variablePermutations;

            static void IterateNextLevel(List<string> currentPermutation, List<(string Name, string[] Values)> variablePotentialValues, List<(string Name, string Value)[]> variablePermutations)
            {
                if (currentPermutation.Count == variablePotentialValues.Count)
                    variablePermutations.Add(currentPermutation.Zip(variablePotentialValues, (v, p) => (p.Name, v)).ToArray());
                else
                {
                    foreach (string item in variablePotentialValues[currentPermutation.Count].Values)
                    {
                        var variant = currentPermutation.ToList(); /*Make a copy*/
                        variant.Add(item);
                        IterateNextLevel(variant, variablePotentialValues, variablePermutations);
                    }
                }
            }
        }
        public static string InterpolateVariables(this string templateString)
        {
            var applicationData = ExpressoApplicationContext.ApplicationData;
            var variables = applicationData.Variables;

            return Regex.Replace(templateString, @"\${(.+?)}", match =>
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
            string[] values = EvaluateVariableValues(variable);
            switch (variable.ValueType)
            {
                case VariableValueType.SingleValue:
                    return values;
                case VariableValueType.MultiValueArray:
                    return new string[] { string.Join(variable.ArrayJoinSeparator, values) };
                case VariableValueType.Iterator:
                    return values;
                default:
                    throw new ArgumentException($"Invalid variable type: {variable.ValueType}");
            }

            string[] EvaluateVariableValues(ApplicationVariable variable)
            {
                switch (variable.ValueType)
                {
                    case VariableValueType.SingleValue:
                        switch (variable.SourceType)
                        {
                            case VariableSourceType.Fixed:
                            case VariableSourceType.CustomList:
                                return new string[] { variable.DefaultValue };
                            case VariableSourceType.Reader:
                                var reader = ExpressoApplicationContext.ApplicationData.FindReaderWithName(variable.Source);
                                if (reader != null)
                                {
                                    reader.EvaluateTransform(out ParcelDataGrid data, out _);
                                    if (data != null)
                                        return data.Columns[0].GetDataAs<object>().Select(v => v.ToString()).Take(1).ToArray();
                                    else return new string[] { };
                                }
                                else throw new ApplicationException($"Cannot find reader: {variable.Source}");
                            default:
                                throw new ArgumentException($"Invalid source type: {variable.SourceType}.");
                        }
                    case VariableValueType.MultiValueArray:
                    case VariableValueType.Iterator:
                        switch (variable.SourceType)
                        {
                            case VariableSourceType.Fixed:
                            case VariableSourceType.CustomList:
                                return variable.Source.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            case VariableSourceType.Reader:
                                var reader = ExpressoApplicationContext.ApplicationData.FindReaderWithName(variable.Source);
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
