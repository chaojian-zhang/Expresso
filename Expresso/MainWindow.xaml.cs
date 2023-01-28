using Expresso.Components;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Odbc;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Expresso.Services;
using Microsoft.AnalysisServices.AdomdClient;
using Microsoft.Win32;
using Expresso.Core;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections;

namespace Expresso
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Constructor
        public MainWindow()
        {
            // Support SQL syntax highlight
            using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Expresso.Resources.sql.xshd.xml"))
            {
                using System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(stream);
                SQLSyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader,
                    HighlightingManager.Instance);
            }

            // Initialize window
            InitializeComponent();
        }
        #endregion

        #region Handlers
        private enum MainTabControlTabIndexMapping
        {
            Welcome = 0,
            Condition = 1,
            Reader = 2,
            Writer = 3,
            Variable = 4,
            Workflow = 5
        };
        private readonly Dictionary<string, Func<string, string, string>> ReaderDataServiceProviders = new Dictionary<string, Func<string, string, string>>()
        {
            { "ODBC", ExecuteODBCQuery },
            { "Microsoft Analysis Service", ExecuteAnalysisServiceQuery },
            { "CSV", ExecuteCSVQuery },
            { "Excel", ExecuteExcelQuery },
            { "SQLite", ExecuteSQLiteQuery },
            { "Expresso", ExecuteReaderQuery }
        };
        private readonly Dictionary<string, Func<string, string, string>> WriterDataServiceProviders = new Dictionary<string, Func<string, string, string>>()
        {
            { "Execute ODBC Command", ExecuteODBCNonQuery },
            { "Execute SQLite Command", ExecuteSQLiteNonQuery },
            { "Output Arbitrary Text", WriteArbitraryText },
            { "Write Reader to ODBC", WriteReaderToODBC },
            { "Write Reader to CSV", WriteReaderToCSV },
            { "Write Reader to SQLite", WriteReaderToSQLite },
        };
        #endregion

        #region Data Binding Properties

        private int _MainTabControlTabIndex = 0;
        public int MainTabControlTabIndex { get => _MainTabControlTabIndex; set => SetField(ref _MainTabControlTabIndex, value); }
        private int _CurrentEditItemIndex = 0;
        public int CurrentEditItemIndex { get => _CurrentEditItemIndex; 
            set
            {
                SetField(ref _CurrentEditItemIndex, value);
                ReaderTabControlEditItemChangedEvent(value);
            }
        }

        private string _CurrentFilePath;
        public string CurrentFilePath { get => _CurrentFilePath; set => SetField(ref _CurrentFilePath, value); }
        private string _BackgroundText = "Open or Create A File to Get Started.";
        public string BackgroundText { get => _BackgroundText; set => SetField(ref _BackgroundText, value); }
        private string _WindowTitle = "Expressor (Idle)";
        public string WindowTitle { get => _WindowTitle; set => SetField(ref _WindowTitle, value); }
        private string _ResultPreview;
        public string ResultPreview { get => _ResultPreview; set => SetField(ref _ResultPreview, value); }
        public string[] ReaderDataServiceProviderNames => ReaderDataServiceProviders.Keys.ToArray();
        public string[] WriterDataServiceProviderNames => WriterDataServiceProviders.Keys.ToArray();

        private ApplicationData _ApplicationData;
        public ApplicationData ApplicationData { get => _ApplicationData; set => SetField(ref _ApplicationData, value); }
        private ApplicationDataReader _CurrentEditingDataReader;
        public ApplicationDataReader CurrentEditingDataReader { get => _CurrentEditingDataReader; set => SetField(ref _CurrentEditingDataReader, value); }
        private ApplicationExecutionConditional _CurrentEditingConditional;
        public ApplicationExecutionConditional CurrentEditingConditional { get => _CurrentEditingConditional; set => SetField(ref _CurrentEditingConditional, value); }
        private ApplicationOutputWriter _CurrentEditingWriter;
        public ApplicationOutputWriter CurrentEditingOutputWriter { get => _CurrentEditingWriter; set => SetField(ref _CurrentEditingWriter, value); }
        private ApplicationWorkflow _CurrentEditingWorkflow;
        public ApplicationWorkflow CurrentEditingWorkflow { get => _CurrentEditingWorkflow; set => SetField(ref _CurrentEditingWorkflow, value); }
        #endregion

        #region Syntax Highlighter

        private IHighlightingDefinition _SQLSyntaxHighlighting;
        public IHighlightingDefinition SQLSyntaxHighlighting
        {
            get => _SQLSyntaxHighlighting;
            set => SetField(ref _SQLSyntaxHighlighting, value);
        }
        #endregion

        #region Actions
        public string ExecuteQuery(string dataSource, string connectionString, string query)
        {
            if (!ReaderDataServiceProviders.ContainsKey(dataSource))
                return "Invalid service provider";
            else
                return ReaderDataServiceProviders[dataSource](connectionString, query);
        }
        #endregion

        #region Events
        private void AddExpressionButton_Click(object sender, RoutedEventArgs e)
        {
        }
        private void AddQueryButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentEditingDataReader.DataQueries.Add(new ApplicationDataQuery());
            CurrentEditingDataReader.NotifyPropertyChanged(nameof(CurrentEditingDataReader.DataQueries));
        }
        private void ReaderQuerySubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var query = CurrentEditingDataReader.DataQueries[CurrentEditItemIndex];
            ResultPreview = ExecuteQuery(query.ServiceProvider, query.DataSourceString, DataReaderAvalonTextEditor.Text);
        }
        private void ReaderTransformSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!WriterDataServiceProviders.ContainsKey(CurrentEditingOutputWriter.ServiceProvider))
                throw new ArgumentException("Invalid service provider");
            else
                WriterDataServiceProviders[CurrentEditingOutputWriter.ServiceProvider](CurrentEditingOutputWriter.DataSourceString, CurrentEditingOutputWriter.Command);
        }
        private void WriterExecuteButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ReaderTabControlEditItemChangedEvent(int value)
        {
            DataReaderAvalonTextEditor.Text = CurrentEditingDataReader.DataQueries[value].Query;
        }
        private void ReaderAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            CurrentEditingDataReader.DataQueries[CurrentEditItemIndex].Query = DataReaderAvalonTextEditor.Text;
        }
        private void ReaderTransformAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            CurrentEditingDataReader.Transform = ReanderTransformAvalonTextEditor.Text;
        }
        private void WriterAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            CurrentEditingOutputWriter.Command = WriterAvalonTextEditor.Text;
        }
        #endregion

        #region Menu Items
        private void MenuItemFileNew_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "Expresso (*.eso)|*.eso|All (*.*)|*.*",
                AddExtension = true,
            };
            if(saveFileDialog.ShowDialog() == true)
            {
                CurrentFilePath = saveFileDialog.FileName;
                ApplicationData = new ApplicationData();
                ApplicationDataSerializer.Save(CurrentFilePath, ApplicationData);
                WindowTitle = $"Expresso - {CurrentFilePath}";
            }
        }
        private void MenuItemFileOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Expresso (*.eso)|*.eso|All (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                CurrentFilePath = openFileDialog.FileName;
                ApplicationData = ApplicationDataSerializer.Load(CurrentFilePath);
                WindowTitle = $"Expresso - {CurrentFilePath}";
            }
        }
        private void MenuItemFileSave_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentFilePath != null)
                ApplicationDataSerializer.Save(CurrentFilePath, ApplicationData);
        }
        private void MenuItemCreateWorkflow_Click(object sender, RoutedEventArgs e)
        {

        }
        private void MenuItemCreateVariable_Click(object sender, RoutedEventArgs e)
        {

        }
        private void MenuItemCreateCondition_Click(object sender, RoutedEventArgs e)
        {

        }
        private void MenuItemCreateReader_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Reader;

            ApplicationData.DataReaders.Add(new ApplicationDataReader());
            CurrentEditingDataReader = ApplicationData.DataReaders.Last();
        }
        private void MenuItemCreateWriter_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Writer;
            ApplicationData.OutputWriters.Add(new ApplicationOutputWriter());
            CurrentEditingOutputWriter = ApplicationData.OutputWriters.Last();
        }
        #endregion

        #region UI Commands
        private void FileNewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) 
            => e.CanExecute = true;

        private void FileNewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
            => MenuItemFileNew_Click(null, null);
        #endregion

        #region Data Binding
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetField<TType>(ref TType field, TType value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<TType>.Default.Equals(field, value)) return false;
            field = value;
            NotifyPropertyChanged(propertyName);
            return true;
        }
        #endregion

        #region Routines
        public static string ExecuteAnalysisServiceQuery(string connection, string query)
        {
            try
            {
                using AdomdConnection conn = new AdomdConnection(connection);
                conn.Open();
                using AdomdCommand cmd = new AdomdCommand(query.TrimEnd(';'), conn);
                CellSet result = cmd.ExecuteCellSet();
                return result.CellSetToTable().ToCSV();
            }
            catch (Exception e)
            {
                return $"Result,Message\nError,\"{e.Message}\"";
            }
        }
        public static string ExecuteODBCQuery(string connection, string query)
        {
            try
            {
                var oracleConnection = new OdbcConnection($"DSN={connection}");
                oracleConnection.Open();
                var dt = new DataTable();
                dt.Load(new OdbcCommand(query, oracleConnection).ExecuteReader());
                return dt.ToConsoleTable();
            }
            catch (Exception e)
            {
                return $"Result,Message\nError,\"{e.Message}\"";
            }
        }
        private static string ExecuteReaderQuery(string connection, string query)
        {
            throw new NotImplementedException();
        }
        private static string ExecuteSQLiteQuery(string connection, string query)
        {
            throw new NotImplementedException();
        }
        private static string ExecuteExcelQuery(string connection, string query)
        {
            throw new NotImplementedException();
        }
        private static string ExecuteCSVQuery(string connection, string query)
        {
            throw new NotImplementedException();
        }

        private static string WriteArbitraryText(string connection, string query)
        {
            throw new NotImplementedException();
        }
        private static string ExecuteODBCNonQuery(string connection, string query)
        {
            throw new NotImplementedException();
        }
        private static string ExecuteSQLiteNonQuery(string connection, string query)
        {
            throw new NotImplementedException();
        }
        private static string WriteReaderToODBC(string connection, string query)
        {
            throw new NotImplementedException();
        }
        private static string WriteReaderToSQLite(string connection, string query)
        {
            throw new NotImplementedException();
        }
        private static string WriteReaderToCSV(string connection, string query)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
