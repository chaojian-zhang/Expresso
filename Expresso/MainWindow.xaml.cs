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
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YamlDotNet.Core.Tokens;
using Expresso.Services;
using Microsoft.AnalysisServices.AdomdClient;

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

            // Initialize Commands
            CreateVariableCommand = new DelegateCommand(() => { }, () => true);
            EditVariablesCommand = new DelegateCommand(() => { }, () => true);

            // Initialize window
            InitializeComponent();
        }
        #endregion

        #region Handlers
        private Dictionary<string, Func<string, string, string>> DataServiceProviders = new Dictionary<string, Func<string, string, string>>()
        {
            { "ODBC", ExecuteODBCQuery },
            { "Microsoft Analysis Service", ExecuteAnalysisServiceQuery },
            { "CSV", ExecuteCSVQuery },
            { "Excel", ExecuteExcelQuery },
            { "SQLite", ExecuteSQLiteQuery },
            { "File/Workflow~~", ExecuteThisQuery }
        };
        #endregion

        #region Data Binding Properties
        private bool _IsFileOpen = false;
        public bool IsFileOpen { get => _IsFileOpen; set => SetField(ref _IsFileOpen, value); }

        private string _BackgroundText = "Open or Create A File to Get Started.";
        public string BackgroundText { get => _BackgroundText; set => SetField(ref _BackgroundText, value); }
        private string _WindowTitle = "Expressor (Idle)";
        public string WindowTitle { get => _WindowTitle; set => SetField(ref _WindowTitle, value); }
        private string _ResultPreview;
        public string ResultPreview { get => _ResultPreview; set => SetField(ref _ResultPreview, value); }
        private string _DataServiceProvider = "ODBC";
        public string DataServiceProvider { get => _DataServiceProvider; set => SetField(ref _DataServiceProvider, value); }
        private string _DataSourceString;
        public string DataSourceString { get => _DataSourceString; set => SetField(ref _DataSourceString, value); }
        public string[] DataServiceProviderNames => DataServiceProviders.Keys.ToArray();
        #endregion

        #region Syntax Highlighter

        private IHighlightingDefinition _SQLSyntaxHighlighting;
        public IHighlightingDefinition SQLSyntaxHighlighting
        {
            get => _SQLSyntaxHighlighting;
            set => SetField(ref _SQLSyntaxHighlighting, value);
        }
        #endregion

        #region Custom Commands
        public ICommand CreateVariableCommand { get; }
        public ICommand EditVariablesCommand { get; }
        #endregion

        #region Delegate Commands
        #endregion

        #region Actions
        public string ExecuteQuery(string dataSource, string connectionString, string query)
        {
            if (!DataServiceProviders.ContainsKey(dataSource))
                return "Invalid service provider";
            else
                return DataServiceProviders[dataSource](connectionString, query);
        }
        #endregion

        #region Routed UI Commands (Supports Shortcuts)
        private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

        }
        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }
        #endregion

        #region Events
        private void AddQueryButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void QueryExitBoxSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            ResultPreview = ExecuteQuery(DataServiceProvider, DataSourceString, AvalonTextEditor.Text);
        }
        private void AvalonEditor_OnInitialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
        }
        private void AvalonEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
        }
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
        private static string ExecuteThisQuery(string connection, string query)
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
        #endregion
    }
}
