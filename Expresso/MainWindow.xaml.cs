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
using System.Windows.Controls;
using System.Diagnostics;
using System.Text;
using Expresso.PopUps;

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
            Processor = 4,
            Variable = 5,
            Workflow = 6
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
        private bool _ShowResultAsDataGrid = false;
        public bool ShowResultAsDataGrid { get => _ShowResultAsDataGrid; set => SetField(ref _ShowResultAsDataGrid, value); }

        private int _MainTabControlTabIndex = 0;
        public int MainTabControlTabIndex { get => _MainTabControlTabIndex; set => SetField(ref _MainTabControlTabIndex, value); }
        private int _ProcessorStepTabItemIndex = 0;
        public int ProcessorStepTabItemIndex { get => _ProcessorStepTabItemIndex; set => SetField(ref _ProcessorStepTabItemIndex, value); }

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

        private ICollection _ReaderResultsView;
        public ICollection ReaderResultsView { get => _ReaderResultsView; set => SetField(ref _ReaderResultsView, value); }
        private ApplicationData _ApplicationData;
        public ApplicationData ApplicationData { get => _ApplicationData; set => SetField(ref _ApplicationData, value); }
        private ApplicationVariable _CurrentSelectedVariable;
        public ApplicationVariable CurrentSelectedVariable { get => _CurrentSelectedVariable; set => SetField(ref _CurrentSelectedVariable, value); }
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
        public static string ExecuteQuery(string dataSource, string connectionString, string query)
        {
            if (!ReaderDataServiceProviders.ContainsKey(dataSource))
                return "Invalid service provider";
            else
                return ReaderDataServiceProviders[dataSource](connectionString, query);
        }
        #endregion

        #region Events
        private void BackgroundLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            =>MenuItemFileOpen_Click(null, null);
        private void DeleteProcessorButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationProcessor processor = button.DataContext as ApplicationProcessor;

            if (ApplicationData.Processors.Remove(processor))
                ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Processors));

            if (ApplicationData.Processors.Count == 0)
                MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Welcome;
        }
        private void AddProcessorInputStepButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationProcessor processor = button.DataContext as ApplicationProcessor;

            ApplicationProcessorStep step = new ApplicationProcessorStep()
            {
                IsStartingStep = true,
                Name = "Root"
            };
            processor.StartingSteps.Add(step);
            processor.ListingOfAllSteps.Add(step);
            processor.NotifyPropertyChanged(nameof(processor.ListingOfAllSteps));
        }
        private void AddProcessorStepInputButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationProcessorStep step = button.DataContext as ApplicationProcessorStep;

            step.Inputs.Add(new ApplicationProcessorStep.ParameterMapping());
            step.NotifyPropertyChanged(nameof(ApplicationProcessorStep.Inputs));
        }

        private void AddProcessorStepOutputButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationProcessorStep step = button.DataContext as ApplicationProcessorStep;

            step.Outputs.Add(new ApplicationProcessorStep.ParameterMapping());
            step.NotifyPropertyChanged(nameof(ApplicationProcessorStep.Outputs));
        }
        private void AddProcessorStepSubstepButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationProcessor processor = button.Tag as ApplicationProcessor;
            ApplicationProcessorStep step = button.DataContext as ApplicationProcessorStep;

            ApplicationProcessorStep nextStep = new ApplicationProcessorStep()
            {
                Name = "New"
            };
            step.NextSteps.Add(nextStep);
            processor.ListingOfAllSteps.Add(nextStep);
            processor.NotifyPropertyChanged(nameof(processor.ListingOfAllSteps));
        }
        private void RemoveProcessorStepButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationProcessor processor = button.Tag as ApplicationProcessor;
            ApplicationProcessorStep step = button.DataContext as ApplicationProcessorStep;

            FindAndRemoveStep(processor.StartingSteps, step);
            RemoveAllStepsFromList(processor.ListingOfAllSteps, step);
            processor.NotifyPropertyChanged(nameof(processor.ListingOfAllSteps));

            void FindAndRemoveStep(ObservableCollection<ApplicationProcessorStep> stepsCollection, ApplicationProcessorStep stepToRemove)
            {
                if (!stepsCollection.Remove(stepToRemove))
                {
                    foreach (ApplicationProcessorStep childStep in stepsCollection)
                        FindAndRemoveStep(childStep.NextSteps, stepToRemove);
                }
            }
            void RemoveAllStepsFromList(ObservableCollection<ApplicationProcessorStep> allStepsList, ApplicationProcessorStep step)
            {
                allStepsList.Remove(step);
                foreach (var next in step.NextSteps)
                    RemoveAllStepsFromList(allStepsList, next);
            }
        }
        private void TestProcessorButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationProcessor processor = button.DataContext as ApplicationProcessor;

            Dictionary<string, string> inputs = new();
            foreach (ApplicationProcessorStep item in processor.StartingSteps)
            {
                foreach (ApplicationProcessorStep.ParameterMapping input in item.Inputs)
                {
                    string response = PromptDialog.Prompt($"Enter value for {input.FromName}", $"Specify Processor Step Inputs: {item.Name}");
                    if (response == null)
                        return;
                    inputs.Add(input.AsName, response);
                }
            }
            Evaluation.TestProcessor(processor.StartingSteps, inputs);
        }
        private void AddDataQueryButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationDataReader reader = button.DataContext as ApplicationDataReader;

            reader.DataQueries.Add(new ApplicationDataQuery());
            reader.NotifyPropertyChanged(nameof(ApplicationDataReader.DataQueries));
        }
        private void ReaderQuerySubmitButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationDataQuery query = button.DataContext as ApplicationDataQuery;

            string resultCSV = ExecuteQuery(query.ServiceProvider, query.DataSourceString, query.Query);
            ResultPreview = resultCSV.CSVToConsoleTable();
            ReaderResultsView = resultCSV.CSVToDataTable();
        }
        private void ReaderTransformSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            
        }
        private void ReaderDataQueryCSVTypeOpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationDataQuery query = button.DataContext as ApplicationDataQuery;

            OpenFileDialog openFileDialog = new()
            {
                Filter = "All (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                query.DataSourceString = openFileDialog.FileName;
            }
        }
        private void WriterExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationOutputWriter writer = button.DataContext as ApplicationOutputWriter;

            if (!WriterDataServiceProviders.ContainsKey(writer.ServiceProvider))
                throw new ArgumentException("Invalid service provider");
            else
                WriterDataServiceProviders[writer.ServiceProvider](writer.DataSourceString, writer.Command);
        }
        private void ReaderAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ApplicationDataQuery query = editor.DataContext as ApplicationDataQuery;
            editor.Text = query.Query;
        }
        private void ReaderAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ApplicationDataQuery query = editor.DataContext as ApplicationDataQuery;
            query.Query = editor.Text;
        }
        private void ReaderTransformAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ApplicationDataReader reader = editor.DataContext as ApplicationDataReader;
            reader.Transform = editor.Text;
        }
        private void VariablesAddVariableButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentSelectedVariable = new ApplicationVariable();
            ApplicationData.Variables.Add(CurrentSelectedVariable);
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Variables));
        }
        private void VariablesRemoveVariableButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationData.Variables.Remove(CurrentSelectedVariable);
            CurrentSelectedVariable = null;
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Variables));
        }
        private void WriterAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ApplicationOutputWriter writer = editor.DataContext as ApplicationOutputWriter;
            writer.Command = editor.Text;
        }
        private void ProcessorTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = sender as TreeView;
            ApplicationProcessor processor = treeView.DataContext as ApplicationProcessor;
            ApplicationProcessorStep step = e.NewValue as ApplicationProcessorStep;

            if (step != null) 
                ProcessorStepTabItemIndex = processor.ListingOfAllSteps.IndexOf(step);
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
                ApplicationData = OpenFile(CurrentFilePath);
                WindowTitle = $"Expresso - {CurrentFilePath}";
            }
        }
        private void MenuItemFileSave_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentFilePath != null)
                ApplicationDataSerializer.Save(CurrentFilePath, ApplicationData);
        }
        private void MenuItemCreateProcessor_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Processor;

            ApplicationData.Processors.Add(new ApplicationProcessor()
            {
                Name = "Processor"
            });
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Processors));
        }
        private void MenuItemCreateWorkflow_Click(object sender, RoutedEventArgs e)
        {

        }
        private void MenuItemCreateVariable_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Variable;

            CurrentSelectedVariable = new ApplicationVariable();
            ApplicationData.Variables.Add(CurrentSelectedVariable);
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Variables));
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
        private void FileOpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
            => MenuItemFileOpen_Click(null, null);
        private void FileOpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = true;
        private void FileSaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
            => MenuItemFileSave_Click(null, null);
        private void FileSaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = CurrentFilePath != null;
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
            StringBuilder csvBuilder = new StringBuilder("File Paths,File Names,Type/Extensions,Sizes");
            foreach (FileSystemInfo item in new DirectoryInfo(connection).EnumerateFileSystemInfos())
                csvBuilder.AppendLine($"{item.FullName},{item.Name},{(item is DirectoryInfo ? "Folder" : item.Extension)},{(item is FileInfo file ? file.Length : string.Empty)}");
            return csvBuilder.ToString();
        }
        public static string ExecuteODBCQuery(string connection, string query)
        {
            try
            {
                var oracleConnection = new OdbcConnection($"DSN={connection}");
                oracleConnection.Open();
                var dt = new DataTable();
                dt.Load(new OdbcCommand(query, oracleConnection).ExecuteReader());
                return dt.ToCSV();
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
            return File.ReadAllText(connection);
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
        private static ApplicationData OpenFile(string filePath)
        {
            // Open file
            var appData = ApplicationDataSerializer.Load(filePath);
            // Some additional GUI requred setups
            foreach (var processor in appData.Processors)
            {
                foreach (var startingSteps in processor.StartingSteps)
                    PopulateSteps(processor, startingSteps);
            }

            return appData;

            void PopulateSteps(ApplicationProcessor processor, ApplicationProcessorStep step)
            {
                processor.ListingOfAllSteps.Add(step);
                foreach (var nextStep in step.NextSteps)
                    processor.ListingOfAllSteps.Add(nextStep);
            }
        }
        #endregion
    }
}
