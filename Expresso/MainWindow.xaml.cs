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
        private static readonly Dictionary<string, Func<string, string, string>> ReaderDataServiceProviders = new Dictionary<string, Func<string, string, string>>()
        {
            { "Folder Filepaths", ExecuteFolderFilepathsQuery },
            { "ODBC", ExecuteODBCQuery },
            { "Microsoft Analysis Service", ExecuteAnalysisServiceQuery },
            { "CSV", ExecuteCSVQuery },
            { "Excel", ExecuteExcelQuery },
            { "SQLite", ExecuteSQLiteQuery },
            { "Expresso", ExecuteReaderQuery }
        };
        private static readonly Dictionary<string, Func<string, string, string>> WriterDataServiceProviders = new Dictionary<string, Func<string, string, string>>()
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
        private int _CurrentReaderQueryItemIndex = 0;
        public int CurrentReaderQueryItemIndex { get => _CurrentReaderQueryItemIndex; set => SetField(ref _CurrentReaderQueryItemIndex, value); }
        private int _CurrentReaderTabIndex = 0;
        public int CurrentReaderTabIndex
        {
            get => _CurrentReaderTabIndex;
            set => SetField(ref _CurrentReaderTabIndex, value);
        }
        private int _CurrentWriterTabIndex= 0;
        public int CurrentWriterTabIndex
        {
            get => _CurrentWriterTabIndex;
            set => SetField(ref _CurrentWriterTabIndex, value);
        }

        private string _CurrentFilePath;
        public string CurrentFilePath { get => _CurrentFilePath; set => SetField(ref _CurrentFilePath, value); }
        private string _BackgroundText = "Open or Create A File to Get Started.";
        public string BackgroundText { get => _BackgroundText; set => SetField(ref _BackgroundText, value); }
        private string _WindowTitle = "Expressor (Idle)";
        public string WindowTitle { get => _WindowTitle; set => SetField(ref _WindowTitle, value); }
        private string _ResultPreview;
        public string ResultPreview { get => _ResultPreview; set => SetField(ref _ResultPreview, value); }
        public static string[] ReaderDataServiceProviderNames => ReaderDataServiceProviders.Keys.ToArray();
        public static string[] WriterDataServiceProviderNames => WriterDataServiceProviders.Keys.ToArray();

        private ApplicationData _ApplicationData;
        public ApplicationData ApplicationData { get => _ApplicationData; set => SetField(ref _ApplicationData, value); }
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
        private void AddDataQueryButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationData.DataReaders[CurrentReaderTabIndex].DataQueries.Add(new ApplicationDataQuery());
            ApplicationData.DataReaders[CurrentReaderTabIndex].NotifyPropertyChanged(nameof(ApplicationDataReader.DataQueries));
        }
        private void ReaderQuerySubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var query = ApplicationData.DataReaders[CurrentReaderTabIndex].DataQueries[CurrentReaderQueryItemIndex];
            ResultPreview = ExecuteQuery(query.ServiceProvider, query.DataSourceString, query.Query);
        }
        private void ReaderTransformSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!WriterDataServiceProviders.ContainsKey(ApplicationData.OutputWriters[CurrentReaderTabIndex].ServiceProvider))
                throw new ArgumentException("Invalid service provider");
            else
                WriterDataServiceProviders[ApplicationData.OutputWriters[CurrentReaderTabIndex].ServiceProvider](ApplicationData.OutputWriters[CurrentReaderTabIndex].DataSourceString, ApplicationData.OutputWriters[CurrentReaderTabIndex].Command);
        }
        private void WriterExecuteButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void ReaderAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            (sender as TextEditor).Text = ApplicationData.DataReaders[CurrentReaderTabIndex].DataQueries[CurrentReaderQueryItemIndex].Query;
        }
        private void ReaderAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            ApplicationData.DataReaders[CurrentReaderTabIndex].DataQueries[CurrentReaderQueryItemIndex].Query = (sender as TextEditor).Text;
        }
        private void ReaderTransformAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            ApplicationData.DataReaders[CurrentReaderTabIndex].Transform = (sender as TextEditor).Text;
        }
        private void WriterAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            ApplicationData.OutputWriters[CurrentReaderTabIndex].Command = (sender as TextEditor).Text;
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
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.DataReaders));
        }
        private void MenuItemCreateWriter_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Writer;
            ApplicationData.OutputWriters.Add(new ApplicationOutputWriter());
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.OutputWriters));
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
        public static string ExecuteFolderFilepathsQuery(string connection, string query)
        {
            return "File Paths\n" + string.Join('\n', Directory.EnumerateFileSystemEntries(connection).ToArray());
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
