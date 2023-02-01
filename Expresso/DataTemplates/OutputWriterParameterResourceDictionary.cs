using Expresso.Core;
using Expresso.PopUps;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Expresso.DataTemplates
{
    partial class OutputWriterParameterResourceDictionary : ResourceDictionary
    {
        #region Construction
        public OutputWriterParameterResourceDictionary()
        {
            InitializeComponent();
        }
        #endregion

        #region Event Handlers - ODBCCommandParameter
        private void ODBCCommandAvalonTextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterODBCCommandParameter parameters = editor.DataContext as OutputWriterODBCCommandParameter;
            if (parameters != null)
                editor.Text = parameters.Query;
        }
        private void ODBCCommandAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterODBCCommandParameter parameters = editor.DataContext as OutputWriterODBCCommandParameter;
            editor.Text = parameters.Query;
        }
        private void ODBCCommandAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterODBCCommandParameter parameters = editor.DataContext as OutputWriterODBCCommandParameter;
            parameters.Query = editor.Text;
        }
        #endregion

        #region Event Handlers - SQLiteCommandParameter
        private void OutputWriterSQLiteCommandAvalonTextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterSQLiteCommandParameter parameters = editor.DataContext as OutputWriterSQLiteCommandParameter;
            if (parameters != null)
                editor.Text = parameters.Query;
        }
        private void OutputWriterSQLiteCommandAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterSQLiteCommandParameter parameters = editor.DataContext as OutputWriterSQLiteCommandParameter;
            editor.Text = parameters.Query;
        }
        private void OutputWriterSQLiteCommandAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterSQLiteCommandParameter parameters = editor.DataContext as OutputWriterSQLiteCommandParameter;
            parameters.Query = editor.Text;
        }
        #endregion

        #region Event Handlers - ODBCWriterParameter
        private void ODBCWriterAddInputButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OutputWriterODBCWriterParameter parameter = button.DataContext as OutputWriterODBCWriterParameter;

            parameter.InputTableNames.Add($"Table{parameter.InputTableNames.Count + 1}");
        }
        private void ODBCWriterRemoveInputButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OutputWriterODBCWriterParameter parameter = button.DataContext as OutputWriterODBCWriterParameter;

            if (parameter.InputTableNames.Count != 0)
                parameter.InputTableNames.RemoveAt(parameter.InputTableNames.Count - 1);
        }
        private void ODBCWriterAvalonTextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterODBCWriterParameter parameters = editor.DataContext as OutputWriterODBCWriterParameter;
            if (parameters != null)
                editor.Text = parameters.Transform;
        }
        private void ODBCWriterAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterODBCWriterParameter parameters = editor.DataContext as OutputWriterODBCWriterParameter;
            editor.Text = parameters.Transform;
        }
        private void ODBCWriterAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterODBCWriterParameter parameters = editor.DataContext as OutputWriterODBCWriterParameter;
            parameters.Transform = editor.Text;
        }
        private void ODBCWriterReaderNamePickButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OutputWriterODBCWriterParameter parameter = button.DataContext as OutputWriterODBCWriterParameter;

            var currentApplicationData = ExpressoApplicationContext.ApplicationData;
            string[] readerNames = currentApplicationData.DataReaders
                .Select(r => r.Name).ToArray();
            if (readerNames.Length != 0)
            {
                var pick = ComboChoiceDialog.Prompt("Pick Reader", "Select reader to use as input:", readerNames.FirstOrDefault(), readerNames, "Writers can optionally take inputs from row processors in Workflow page.");
                if (pick != null)
                    parameter.InputTableNames.Add(pick);
            }
        }
        #endregion

        #region Event Handlers - CSVWriterParameter
        private void CSVWriterChooseSaveLocationButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OutputWriterCSVWriterParameter parameter = button.DataContext as OutputWriterCSVWriterParameter;

            SaveFileDialog saveFileDialog = new()
            {
                Filter = "CSV (*.csv)|*.csv|All (*.*)|*.*",
                AddExtension = true,
                Title = "Choose location to save file"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                parameter.FilePath = saveFileDialog.FileName;
            }
        }
        private void CSVWriterAddInputButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OutputWriterCSVWriterParameter parameter = button.DataContext as OutputWriterCSVWriterParameter;

            parameter.InputTableNames.Add($"Table{parameter.InputTableNames.Count + 1}");
        }
        private void CSVWriterRemoveInputButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OutputWriterCSVWriterParameter parameter = button.DataContext as OutputWriterCSVWriterParameter;

            if (parameter.InputTableNames.Count != 0)
                parameter.InputTableNames.RemoveAt(parameter.InputTableNames.Count - 1);
        }
        private void CSVWriterAvalonTextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterCSVWriterParameter parameters = editor.DataContext as OutputWriterCSVWriterParameter;
            if (parameters != null)
                editor.Text = parameters.Transform;
        }
        private void CSVWriterAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterCSVWriterParameter parameters = editor.DataContext as OutputWriterCSVWriterParameter;
            editor.Text = parameters.Transform;
        }
        private void CSVWriterAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterCSVWriterParameter parameters = editor.DataContext as OutputWriterCSVWriterParameter;
            parameters.Transform = editor.Text;
        }
        private void CSVWriterReaderNamePickButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OutputWriterCSVWriterParameter parameter = button.DataContext as OutputWriterCSVWriterParameter;

            var currentApplicationData = ExpressoApplicationContext.ApplicationData;
            string[] readerNames = currentApplicationData.DataReaders
                .Select(r => r.Name).ToArray();
            if (readerNames.Length != 0)
            {
                var pick = ComboChoiceDialog.Prompt("Pick Reader", "Select reader to use as input:", readerNames.FirstOrDefault(), readerNames, "Writers can optionally take inputs from row processors in Workflow page.");
                if (pick != null)
                    parameter.InputTableNames.Add(pick);
            }

        }
        #endregion

        #region Event Handlers - ExcelWriterParameter
        private void ExcelWriterChooseSaveLocationButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OutputWriterExcelWriterParameter parameter = button.DataContext as OutputWriterExcelWriterParameter;

            SaveFileDialog saveFileDialog = new()
            {
                Filter = "Excel XLS (*.xls)|*.xls|All (*.*)|*.*",
                AddExtension = true,
                Title = "Choose location to save file"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                parameter.FilePath = saveFileDialog.FileName;
            }
        }
        private void ExcelWriterAddInputButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OutputWriterExcelWriterParameter parameter = button.DataContext as OutputWriterExcelWriterParameter;

            parameter.InputTableNames.Add($"Table{parameter.InputTableNames.Count + 1}");
        }
        private void ExcelWriterRemoveInputButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OutputWriterExcelWriterParameter parameter = button.DataContext as OutputWriterExcelWriterParameter;

            if (parameter.InputTableNames.Count != 0)
                parameter.InputTableNames.RemoveAt(parameter.InputTableNames.Count - 1);
        }
        private void ExcelWriterAvalonTextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterExcelWriterParameter parameters = editor.DataContext as OutputWriterExcelWriterParameter;
            if (parameters != null)
                editor.Text = parameters.Transform;
        }
        private void ExcelWriterAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterExcelWriterParameter parameters = editor.DataContext as OutputWriterExcelWriterParameter;
            editor.Text = parameters.Transform;
        }
        private void ExcelWriterAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterExcelWriterParameter parameters = editor.DataContext as OutputWriterExcelWriterParameter;
            parameters.Transform = editor.Text;
        }
        private void ExcelWriterReaderNamePickButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            OutputWriterExcelWriterParameter parameter = button.DataContext as OutputWriterExcelWriterParameter;

            var currentApplicationData = ExpressoApplicationContext.ApplicationData;
            string[] readerNames = currentApplicationData.DataReaders
                .Select(r => r.Name).ToArray();
            if (readerNames.Length != 0)
            {
                var pick = ComboChoiceDialog.Prompt("Pick Reader", "Select reader to use as input:", readerNames.FirstOrDefault(), readerNames, "Writers can optionally take inputs from row processors in Workflow page.");
                if (pick != null)
                    parameter.InputTableNames.Add(pick);
            }

        }
        #endregion
    }
}
