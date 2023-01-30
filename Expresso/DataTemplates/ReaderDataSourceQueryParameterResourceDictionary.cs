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

            var currentApplicationData = ApplicationDataHelper.GetCurrentApplicationData();
            ApplicationDataReader currentReader = currentApplicationData.FindReaderFromParameters(parameter);
            IEnumerable<ApplicationDataReader> directDependants = currentApplicationData.DataReaders.Where(r => r.DataQueries.Any(q => q.Parameters is ExpressorReaderDataQueryParameter exp && exp.ReaderName == currentReader.Name));
            string[] readerNames = currentApplicationData.DataReaders
                .Except(new[] { currentReader })
                .Except(directDependants)
                .Select(r => r.Name).ToArray();
            if (readerNames.Length != 0)
                parameter.ReaderName = ComboChoiceDialog.Prompt("Pick Reader", "Select reader to read data from:", readerNames.FirstOrDefault(), readerNames, "Notice you cannot pick readers that references the current reader.");
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
    }
}
