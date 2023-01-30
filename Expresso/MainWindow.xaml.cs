using Expresso.Components;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Expresso.Services;
using Microsoft.Win32;
using Expresso.Core;
using System.Collections.ObjectModel;
using System.Collections;
using System.Windows.Controls;
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
            Variable = 1,
            Condition = 2,
            Reader = 3,
            Writer = 4,
            Processor = 5,
            Workflow = 6
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
        public static string[] WriterDataServiceProviderNames => WriterParameterBase.GetServiceProviders().Keys.ToArray();
        public static string[] WorkflowActionTypes => new string[]
        {
            "Condition",
            "Reader",
            "Writer",
            "Row Processor",
        };

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
        private ApplicationWorkflowStep _CurrentSelectedWorkflowStep;
        public ApplicationWorkflowStep CurrentSelectedWorkflowStep { get => _CurrentSelectedWorkflowStep; set => SetField(ref _CurrentSelectedWorkflowStep, value); }
        private ApplicationVariable _CurrentSelectedVariable;
        public ApplicationVariable CurrentSelectedVariable { get => _CurrentSelectedVariable; set => SetField(ref _CurrentSelectedVariable, value); }
        private ApplicationExecutionConditional _CurrentSelectedCondition;
        public ApplicationExecutionConditional CurrentSelectedCondition { get => _CurrentSelectedCondition; set => SetField(ref _CurrentSelectedCondition, value); }
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
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                CurrentFilePath = files[0];
                ApplicationData = OpenFile(CurrentFilePath);
                WindowTitle = $"Expresso - {CurrentFilePath}";
            }
        }
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
                string resultCSV = reader.EvaluateTransform(out _, out _);
                ResultPreview = resultCSV.CSVToConsoleTable();
                ReaderResultsView = resultCSV.CSVToDataTable();
            }
        }
        private void WriterTestButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationOutputWriter writer = button.DataContext as ApplicationOutputWriter;

            var result = writer.Parameters.PerformAction(new List<ParcelDataGrid>());
            MessageBox.Show(result, "Test Result");
        }
        private void WriterDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationOutputWriter writer = button.DataContext as ApplicationOutputWriter;

            if (ApplicationData.OutputWriters.Remove(writer))
                ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.OutputWriters));

            if (ApplicationData.OutputWriters.Count == 0)
                MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Welcome;
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
        #endregion

        #region Events - Conditionals
        private void PickConditionSourceReaderButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationExecutionConditional condition = button.DataContext as ApplicationExecutionConditional;

            var currentApplicationData = ApplicationDataHelper.GetCurrentApplicationData();
            string[] readerNames = currentApplicationData.DataReaders
                .Select(r => r.Name).ToArray();
            if (readerNames.Length != 0)
                condition.ReaderName = ComboChoiceDialog.Prompt("Pick Reader", "Select reader to read data from:", readerNames.FirstOrDefault(), readerNames, "For binary conditions, readers that return non-zero rows are considered true; For switch conditions, the first element of the reader result will dictate subsequent step name.");
        }
        private void EvaluateCurrentConditionResultButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationExecutionConditional condition = button.DataContext as ApplicationExecutionConditional;

            if (condition != null && !string.IsNullOrEmpty(condition.ReaderName))
            {
                var reader = ApplicationData.FindReaderWithName(condition.ReaderName);
                if (reader != null)
                {
                    reader.EvaluateTransform(out ParcelDataGrid result, out _);
                    switch (condition.Type)
                    {
                        case ConditionType.Binary:
                            MessageBox.Show($"{result.RowCount >= 1}", "Evaluation Result");
                            break;
                        case ConditionType.Switch:
                            MessageBox.Show($"{result.Columns[0][0]}", "Evaluation Result");
                            break;
                        default:
                            throw new ArgumentNullException("Invalid condition type.");
                    }
                }
                else MessageBox.Show($"Cannot find reader.", "Invalid Reader Name");
            }
        }
        #endregion

        #region Events - Row Processors
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
        private void ProcessorTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = sender as TreeView;
            ApplicationProcessor processor = treeView.DataContext as ApplicationProcessor;
            ApplicationProcessorStep step = e.NewValue as ApplicationProcessorStep;

            if (step != null)
                ProcessorStepTabItemIndex = processor.ListingOfAllSteps.IndexOf(step);
        }
        #endregion

        #region Events - Workflows
        private void AddWorkflowStepButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationWorkflow workflow = button.DataContext as ApplicationWorkflow;

            ApplicationWorkflowStep step = new ()
            {
                Name = "Starting Step",
                ActionType = WorkflowActionTypes.First()
            };
            workflow.StartingSteps.Add(step);
        }
        private void DeleteWorkflowButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationWorkflow workflow = button.DataContext as ApplicationWorkflow;

            if (ApplicationData.Workflows.Remove(workflow))
                ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Workflows));

            if (ApplicationData.Workflows.Count == 0)
                MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Workflow;
        }

        private void ExecuteWorkflowButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationWorkflow workflow = button.DataContext as ApplicationWorkflow;

            throw new NotImplementedException();
        }

        private void WorkflowTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = sender as TreeView;
            ApplicationWorkflow workflow = treeView.DataContext as ApplicationWorkflow;
            CurrentSelectedWorkflowStep = e.NewValue as ApplicationWorkflowStep;
        }
        private void AddWorkflowStepSubstepButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationWorkflow workflow = button.DataContext as ApplicationWorkflow;
            ApplicationWorkflowStep step = CurrentSelectedWorkflowStep;

            if (CurrentSelectedWorkflowStep != null) 
            {
                ApplicationWorkflowStep nextStep = new ApplicationWorkflowStep()
                {
                    Name = "New",
                    ActionType = WorkflowActionTypes.First()
                };
                step.NextSteps.Add(nextStep);
                CurrentSelectedWorkflowStep = nextStep;
            }
        }
        private void RemoveWorkflowStepButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationWorkflow workflow = button.DataContext as ApplicationWorkflow;
            ApplicationWorkflowStep step = CurrentSelectedWorkflowStep;

            FindAndRemoveStep(workflow.StartingSteps, step);

            static void FindAndRemoveStep(ObservableCollection<ApplicationWorkflowStep> stepsCollection, ApplicationWorkflowStep stepToRemove)
            {
                if (!stepsCollection.Remove(stepToRemove))
                {
                    foreach (ApplicationWorkflowStep childStep in stepsCollection)
                        FindAndRemoveStep(childStep.NextSteps, stepToRemove);
                }
            }
        }
        private void WorkflowStepPickActionItemButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationWorkflowStep step = button.DataContext as ApplicationWorkflowStep;

            var currentApplicationData = ApplicationDataHelper.GetCurrentApplicationData();
            string[] entityNames = null;
            switch (step.ActionType)
            {
                case "Condition":
                    entityNames = currentApplicationData.Conditionals
                        .Select(r => r.Name).ToArray();
                    break;
                case "Reader":
                    entityNames = currentApplicationData.DataReaders
                        .Select(r => r.Name).ToArray();
                    break;
                case "Writer":
                    entityNames = currentApplicationData.OutputWriters
                        .Select(r => r.Name).ToArray();
                    break;
                case "Row Processor":
                    entityNames = currentApplicationData.Processors
                        .Select(r => r.Name).ToArray();
                    break;
                default:
                    throw new ArgumentException("Invalid Action Type.");
            }

            if (entityNames.Length != 0)
                step.ActionItem = ComboChoiceDialog.Prompt($"Pick {step.ActionType}", $"Select {step.ActionType} to use:", entityNames.FirstOrDefault(), entityNames);
        }
        #endregion

        #region Menu Items
        private void MenuItemFileNew_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "Expresso (*.eso)|*.eso|All (*.*)|*.*",
                AddExtension = true,
                Title = "Choose location to save file"
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
                Filter = "Expresso (*.eso)|*.eso|All (*.*)|*.*",
                Title = "Choose file to open"
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
                Name = "New Processor"
            });
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Processors));
        }
        private void MenuItemCreateWorkflow_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Workflow;

            ApplicationData.Workflows.Add(new ApplicationWorkflow()
            {
                Name = "New Workflow"
            });
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Workflows));
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
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Condition;

            ApplicationData.Conditionals.Add(new ApplicationExecutionConditional()
            {
                Name = "New Condition"
            });
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Conditionals));
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

            var writer = new ApplicationOutputWriter()
            {
                Name = "New Writer",
                ServiceProvider = WriterDataServiceProviderNames.First()
            };
            ApplicationData.OutputWriters.Add(writer);
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.OutputWriters));
        }
        private void MenuItemExportSQLite_Click(object sender, RoutedEventArgs e)
        {
            //using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
            //connection.Open();
            //foreach (ApplicationDataReader item in ApplicationData.DataReaders)
            //{
            //    SQLite.PopulateTable()
            //}
            //connection.
            //connection.Close();
        }
        private void MenuItemEngineRun_Click(object sender, RoutedEventArgs e)
        {
            var currentApplicationData = ApplicationDataHelper.GetCurrentApplicationData();
            string[] readerNames = currentApplicationData.Workflows
                .Select(r => r.Name).ToArray();
            if (readerNames.Length != 0)
            {
                var pick = ComboChoiceDialog.Prompt("Pick Workflow", "Select workflow to run:", readerNames.FirstOrDefault(), readerNames);
                if (pick != null)
                    throw new NotImplementedException();
            }
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
