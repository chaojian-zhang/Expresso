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
            OpenSettingsCommand = new DelegateCommand(() => OpenSettings(), () => true);

            // Initialize window
            InitializeComponent();
        }
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
        public ICommand OpenSettingsCommand { get; }
        #endregion

        #region Delegate Commands
        public void OpenSettings()
        {

        }
        #endregion

        #region Actions
        public string ExecuteQuery(string query)
        {
            try
            {
                var oracleConnection = new OdbcConnection($"DSN={ConfigurationHelper.GetConfiguration("ODBC")}");
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
        #endregion

        #region Routed UI Commands (Supports Shortcuts)
        private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

        }
        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {

        }
        private void OpenSettingsCommand_Click(object sender, ExecutedRoutedEventArgs e)
        {

        }
        private void OpenSettingsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

        }
        #endregion

        #region Events
        private void QueryExitBoxSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            ResultPreview = ExecuteQuery(AvalonTextEditor.Text);
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
    }
}
