using Expresso.Components;
using Expresso.Core;
using Expresso.PopUps;
using ICSharpCode.AvalonEdit;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Expresso.DataTemplates
{
    partial class ReaderDataSourceQueryParameterResourceDictionary : ResourceDictionary
    {
        #region Construction
        public ReaderDataSourceQueryParameterResourceDictionary()
        {
            InitializeComponent();
        }
        #endregion

        #region Event Handlers
        private void ReaderDataSourceQueryAvalonTextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ReaderDataQueryParameterBase queryParameters = editor.DataContext as ReaderDataQueryParameterBase;
            if (queryParameters != null)
                editor.Text = queryParameters.Query;
        }
        private void ReaderDataSourceQueryAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ReaderDataQueryParameterBase queryParameters = editor.DataContext as ReaderDataQueryParameterBase;
            editor.Text = queryParameters.Query;
        }
        private void ReaderDataSourceQueryAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ReaderDataQueryParameterBase queryParameters = editor.DataContext as ReaderDataQueryParameterBase;
            queryParameters.Query = editor.Text;
        }
        private void ReaderDataQueryExistingReaderTypePickReaderButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ExpressorReaderDataQueryParameter parameter = button.DataContext as ExpressorReaderDataQueryParameter;

            var currentApplicationData = ExpressoApplicationContext.ApplicationData;
            ApplicationDataReader currentReader = currentApplicationData.FindReaderFromParameters(parameter);
            IEnumerable<ApplicationDataReader> directDependants = currentApplicationData.DataReaders.Where(r => r.DataQueries.Any(q => q.Parameters is ExpressorReaderDataQueryParameter exp && exp.ReaderName == currentReader.Name));
            string[] readerNames = currentApplicationData.DataReaders
                .Except(new[] { currentReader })
                .Except(directDependants)
                .Select(r => r.Name).ToArray();
            if (readerNames.Length != 0)
                parameter.ReaderName = ComboChoiceDialog.Prompt("Pick Reader", "Select reader to read data from:", readerNames.FirstOrDefault(), readerNames, "Notice you cannot pick readers that references the current reader.");
        }
        #endregion

        #region Event Handlers - SQLite
        private void ReaderDataQuerySQLiteShowAvailableTablesButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            SQLiteReaderDataQueryParameter parameter = button.DataContext as SQLiteReaderDataQueryParameter;

            if (System.IO.File.Exists(parameter.FilePath))
            {
                using SqliteConnection SqliteConnection = new SqliteConnection($"Data Source={parameter.FilePath}");
                SqliteConnection.Open();
                var dataTable = new DataTable();
                dataTable.Load(new SqliteCommand("SELECT name FROM sqlite_schema WHERE type='table' ORDER BY name", SqliteConnection).ExecuteReader());
                SqliteConnection.Close();
                string[] tableNames = dataTable.Rows.Select(r => r[0].ToString()).ToArray();

                string response = ListChoiceDialog.Prompt("Preview Tables", "Click \"Copy Name\" to copy selected table", tableNames.FirstOrDefault(), tableNames, null, "Copy Name");
                if (response != null)
                    Clipboard.SetText(response);
            }
        }
        private void ReaderDataQuerySQLiteTypeOpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            SQLiteReaderDataQueryParameter parameter = button.DataContext as SQLiteReaderDataQueryParameter;

            OpenFileDialog openFileDialog = new()
            {
                Filter = "All (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                parameter.FilePath = openFileDialog.FileName;
            }
        }
        #endregion

        #region Event Handlers - Microsoft Analysis Service
        private void ReaderAnalysisServiceTransformAvalonTextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            MicrosoftAnalysisServiceDataQueryParameter queryParameters = editor.DataContext as MicrosoftAnalysisServiceDataQueryParameter;
            if (queryParameters != null)
                editor.Text = queryParameters.Transform;
        }
        private void ReaderAnalysisServiceTransformAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            MicrosoftAnalysisServiceDataQueryParameter queryParameters = editor.DataContext as MicrosoftAnalysisServiceDataQueryParameter;
            editor.Text = queryParameters.Transform;
        }
        private void ReaderAnalysisServiceTransformAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            MicrosoftAnalysisServiceDataQueryParameter queryParameters = editor.DataContext as MicrosoftAnalysisServiceDataQueryParameter;
            queryParameters.Transform = editor.Text;
        }
        #endregion

        #region Event Handlers - CSV
        private void ReaderDataQueryCSVTypeOpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            CSVReaderDataQueryParameter parameter = button.DataContext as CSVReaderDataQueryParameter;

            OpenFileDialog openFileDialog = new()
            {
                Filter = "CSV Files (*.csv)|*.csv|All (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                parameter.FilePath = openFileDialog.FileName;
            }
        }
        #endregion

        #region Event Handlers - Excel
        private void ReaderDataQueryExcelTypeOpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ExcelReaderDataQueryParameter parameter = button.DataContext as ExcelReaderDataQueryParameter;

            OpenFileDialog openFileDialog = new()
            {
                Filter = "Excel XLS (*.xls)|*.xls|Excel XLSX (*.xlsx)|*.xlsx|Excel XLSB (*.xlsb)|*.xlsb|CSV (*.csv)|*.csv|All (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                parameter.FilePath = openFileDialog.FileName;
            }
        }
        #endregion

        #region Event Handlers - Program Output
        private void ReaderDataQueryProgramOutputOpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ProgramOutputReaderDataQueryParameter parameter = button.DataContext as ProgramOutputReaderDataQueryParameter;

            OpenFileDialog openFileDialog = new()
            {
                Filter = "Executable Files (*.exe)|*.exe|All (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                parameter.FilePathOrName = openFileDialog.FileName;
            }
        }
        private void ReaderProgramOutputEnvironmentVariableAvalonTextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ProgramOutputReaderDataQueryParameter queryParameters = editor.DataContext as ProgramOutputReaderDataQueryParameter;
            if (queryParameters != null)
                editor.Text = queryParameters.EnvironmentVariables;
        }
        private void ReaderProgramOutputEnvironmentVariableAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ProgramOutputReaderDataQueryParameter queryParameters = editor.DataContext as ProgramOutputReaderDataQueryParameter;
            editor.Text = queryParameters.EnvironmentVariables;
        }
        private void ReaderProgramOutputEnvironmentVariableAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ProgramOutputReaderDataQueryParameter queryParameters = editor.DataContext as ProgramOutputReaderDataQueryParameter;
            queryParameters.EnvironmentVariables = editor.Text;
        }
        #endregion
    }
}
