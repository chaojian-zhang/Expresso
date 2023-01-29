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
using System.Text;
using Expresso.PopUps;
using Expresso.ReaderDataQueries;
using System.Reflection;

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

        #region Properties
        /// <summary>
        /// For caching remote data queries, used by specific data query readers
        /// </summary>
        private static DatabaseContext GlobalDatabaseContext = new DatabaseContext();
        /// <summary>
        /// For general management of file-scope readers
        /// </summary>
        private DatabaseContext SessionDatabaseContext;
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
        private static readonly Dictionary<string, Action<string, string, string>> WriterDataServiceProviders = new Dictionary<string, Action<string, string, string>>()
        {
            { "Execute ODBC Command", ExecuteODBCNonQuery },
            { "Execute SQLite Command", ExecuteSQLiteNonQuery },
            { "Output Arbitrary Text", WriteArbitraryText },
            { "Write Reader to ODBC", WriteReaderToODBC },
            { "Write Reader to CSV", WriteReaderToCSV },
            { "Write Reader to SQLite", WriteReaderToSQLite },
            { "Export Reader to CSV", WriteReaderToSQLite },
            { "Export Readers to SQLite", WriteReaderToSQLite },
            { "Export Readers to Excel", WriteReaderToSQLite },
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
        private string _WindowTitle = "Expressor (Idle)";
        public string WindowTitle { get => _WindowTitle; set => SetField(ref _WindowTitle, value); }
        private string _ResultPreview;
        public string ResultPreview { get => _ResultPreview; set => SetField(ref _ResultPreview, value); }
        public static string[] ReaderDataServiceProviderNames => ReaderDataQueryParameterBase.GetServiceProviders().Keys.ToArray();
        public static string[] WriterDataServiceProviderNames => WriterDataServiceProviders.Keys.ToArray();

        private ICollection _ReaderResultsView;
        public ICollection ReaderResultsView { get => _ReaderResultsView; set => SetField(ref _ReaderResultsView, value); }
        private ApplicationData _ApplicationData;
        public ApplicationData ApplicationData 
        { 
            get => _ApplicationData; 
            set
            {
                SetField(ref _ApplicationData, value);
                SessionDatabaseContext = new DatabaseContext();
            }
        }
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

        #region Events
        private void BackgroundLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            =>MenuItemFileOpen_Click(null, null);
        private void DeleteReaderButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationDataReader reader = button.DataContext as ApplicationDataReader;

            if (ApplicationData.DataReaders.Remove(reader))
                ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.DataReaders));

            if (ApplicationData.DataReaders.Count == 0)
                MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Welcome;
        }
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

            var newQuery = new ApplicationDataQuery()
            {
                Name = $"Query{reader.DataQueries.Count + 1}",
                ServiceProvider = ReaderDataServiceProviderNames.First()
            };
            reader.DataQueries.Add(newQuery);
            reader.NotifyPropertyChanged(nameof(ApplicationDataReader.DataQueries));
            newQuery.NotifyPropertyChanged(nameof(ApplicationDataQuery.Parameters));
        }
        private void DeleteDataQueryButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationDataReader reader = button.Tag as ApplicationDataReader;
            ApplicationDataQuery query = button.DataContext as ApplicationDataQuery;

            reader.DataQueries.Remove(query);
            reader.NotifyPropertyChanged(nameof(ApplicationDataReader.DataQueries));
        }
        private void ReaderQuerySubmitButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationDataQuery query = button.DataContext as ApplicationDataQuery;
            
            string resultCSV = query.Parameters.MakeQuery();
            ResultPreview = resultCSV.CSVToConsoleTable();
            ReaderResultsView = resultCSV.CSVToDataTable();
        }
        private void ReaderTransformSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationDataReader reader = button.DataContext as ApplicationDataReader;

            if (reader != null && !string.IsNullOrWhiteSpace(reader.Transform))
            {
                string resultCSV = reader.EvaluateTransform();
                ResultPreview = resultCSV.CSVToConsoleTable();
                ReaderResultsView = resultCSV.CSVToDataTable();
            }
        }

        private void WriterExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationOutputWriter writer = button.DataContext as ApplicationOutputWriter;

            if (!WriterDataServiceProviders.ContainsKey(writer.ServiceProvider))
                throw new ArgumentException("Invalid service provider");
            else
                WriterDataServiceProviders[writer.ServiceProvider](writer.DataSourceString, writer.AdditionalParameter, writer.Command);
        }
        private void ReaderTransformAvalonTextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ApplicationDataReader reader = editor.DataContext as ApplicationDataReader;
            if (reader != null)
                editor.Text = reader.Transform;
        }
        private void ReaderTransformAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ApplicationDataReader reader = editor.DataContext as ApplicationDataReader;
            editor.Text = reader.Transform;
        }
        private void ReaderTransformAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ApplicationDataReader reader = editor.DataContext as ApplicationDataReader;
            reader.Transform = editor.Text;
        }
        private void WriterAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ApplicationOutputWriter writer = editor.DataContext as ApplicationOutputWriter;
            editor.Text = writer.Command;
        }
        private void WriterAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ApplicationOutputWriter writer = editor.DataContext as ApplicationOutputWriter;
            writer.Command = editor.Text;
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

            ApplicationData.DataReaders.Add(new ApplicationDataReader()
            {
                Name = "New Reader"
            });
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.DataReaders));
        }
        private void MenuItemCreateWriter_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Writer;
            ApplicationData.OutputWriters.Add(new ApplicationOutputWriter()
            {
                Name = "New Writer"
            });
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.OutputWriters));
        }
        private void MenuItemExportSQLite_Click(object sender, RoutedEventArgs e)
        {
            //using SQLiteConnection connection = new SQLiteConnection("Data Source=:memory:");
            //connection.Open();
            //foreach (ApplicationDataReader item in ApplicationData.DataReaders)
            //{
            //    SQLite.PopulateTable()
            //}
            //connection.
            //connection.Close();
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
        private static string ExecuteExcelQuery(string connection, string query)
        {
            throw new NotImplementedException();
        }
        private static void WriteArbitraryText(string connection, string parmeter, string query)
        {
            throw new NotImplementedException();
        }
        private static void ExecuteODBCNonQuery(string connection, string parameter, string query)
        {
            throw new NotImplementedException();
        }
        private static void ExecuteSQLiteNonQuery(string connection, string parameter, string query)
        {
            throw new NotImplementedException();
        }
        private static void WriteReaderToODBC(string connection, string parameter, string query)
        {
            //try
            //{
            //    var oracleConnection = new OdbcConnection($"DSN={connection}");
            //    oracleConnection.Open();
                
            //    QueryContext.ExecuteQuery(query)

            //    var dt = new DataTable();
            //    dt.Load(new OdbcCommand(query, oracleConnection).ExecuteReader());
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message, "Error");
            //}
        }
        private static void WriteReaderToSQLite(string connection, string parameter, string query)
        {

            throw new NotImplementedException();
        }
        private static void WriteReaderToCSV(string connection, string parameter, string query)
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
