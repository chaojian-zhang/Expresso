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
using System.Text;
using System.Diagnostics;
using System.Windows.Documents;

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
            // Hide console
            Win32.HideConsoleWindow();

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
            Variable = 1,
            Condition = 2,
            Reader = 3,
            Writer = 4,
            Processor = 5,
            Programmer = 6,
            Workflow = 7
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
        private string _WindowTitle = "Expresso (Idle)";
        public string WindowTitle { get => _WindowTitle; set => SetField(ref _WindowTitle, value); }
        private string _ResultPreview;
        public string ResultPreview { get => _ResultPreview; set => SetField(ref _ResultPreview, value); }
        public static string[] ReaderDataServiceProviderNames => ReaderDataQueryParameterBase.GetServiceProviders().Keys.ToArray();
        public static string[] WriterDataServiceProviderNames => WriterParameterBase.GetServiceProviders().Keys.ToArray();
        public static string[] RowProcessorActionNames => RowProcessorParameterBase.GetServiceProviders().Keys.ToArray();

        private ICollection _ReaderResultsView;
        public ICollection ReaderResultsView { get => _ReaderResultsView; set => SetField(ref _ReaderResultsView, value); }
        public ApplicationData ApplicationData 
        { 
            get => ExpressoApplicationContext.ApplicationData; 
            set
            {
                ExpressoApplicationContext.Context.SetCurrentApplicationData(value);
                NotifyPropertyChanged(nameof(ApplicationData));
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
        private void MenuItemHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            HelpAboutPanel.Visibility = (HelpAboutPanel.Visibility == Visibility.Visible)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
        private void HelpAboutPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            if (HelpAboutPanel.Visibility == Visibility.Visible)
                HelpAboutPanel.Visibility = Visibility.Collapsed;
        }
        private void BackgroundLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            => MenuItemFileOpen_Click(null, null);
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
            CurrentSelectedVariable = new ApplicationVariable()
            {
                Name = $"variable{ApplicationData.Variables.Count + 1}"
            };
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

        #region Events - Window
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (CurrentFilePath != null)
            {
                string folder = System.IO.Path.GetDirectoryName(CurrentFilePath);
                string name = System.IO.Path.GetFileNameWithoutExtension(CurrentFilePath);
                string extension = System.IO.Path.GetExtension(CurrentFilePath);
                string autoSaveName = $"{name}_AutoSave";

                string autoSavePath = System.IO.Path.Combine(folder, $"{autoSaveName}{extension}");
                ApplicationDataSerializer.Save(autoSavePath, ApplicationData, isAutoSave: true);

                // In case no changes are made, don't create auto save file
                // Remark-cz: For slightly faster implementation, we can compare directly in-memory streams instead of actually writing to file first
                if (System.IO.File.Exists(CurrentFilePath) && CompareFileEquals(CurrentFilePath, autoSavePath))
                    System.IO.File.Delete(autoSavePath);
            }

            static bool CompareFileEquals(string path1, string path2)
            {
                byte[] file1 = System.IO.File.ReadAllBytes(path1);
                byte[] file2 = System.IO.File.ReadAllBytes(path2);
                if (file1.Length == file2.Length)
                {
                    for (int i = 0; i < file1.Length; i++)
                        if (file1[i] != file2[i])
                            return false;
                    return true;
                }
                return false;
            }
        }
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
        #endregion

        #region Events - Main Tab Control
        /// <remarks>
        /// We just need this event specifically for DependencyGraph because tab item otherwise doesn't have a way to get notified when it's clicked
        /// </remarks>
        private void MainGUITabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DependencyGraph.IsSelected)
                DependencyGraphContentPresenter.Content = new DependencyAnalyzer().Analyze(ApplicationData);

            // Don't mark it as handled so all other data binding processes etc. happen as usual
        }
        #endregion

        #region Events - Variables
        private void VariableGeneratePreviewButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationVariable variable = button.DataContext as ApplicationVariable;
            if (variable != null) 
            {
                string[] values = variable.EvaluateVariable();
                MessageBox.Show($"${{{variable.Name}}}" + Environment.NewLine + string.Join(Environment.NewLine, values), "Variable Preview");
            }
        }
        private void VariablePickSourceReaderButton_Click(object sender, RoutedEventArgs e)
        {
            string[] readerNames = ApplicationData.DataReaders
                .Select(r => r.Name).ToArray();
            if (readerNames.Length != 0)
            {
                string pick = ComboChoiceDialog.Prompt("Pick Reader", "Select reader to read data from:", readerNames.FirstOrDefault(), readerNames);
                if (pick != null)
                    CurrentSelectedVariable.Source = pick;
            }
        }
        #endregion

        #region Events - Conditionals
        private void PickConditionSourceReaderButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationExecutionConditional condition = button.DataContext as ApplicationExecutionConditional;

            var currentApplicationData = ExpressoApplicationContext.ApplicationData;
            string[] readerNames = currentApplicationData.DataReaders
                .Select(r => r.Name).ToArray();
            if (readerNames.Length != 0)
            {
                string pick = ComboChoiceDialog.Prompt("Pick Reader", "Select reader to read data from:", readerNames.FirstOrDefault(), readerNames, "For binary conditions, readers that return non-zero rows are considered true; For switch conditions, the first element of the reader result will dictate subsequent step name.");
                if (pick != null)
                    condition.ReaderName = pick;
            }
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
                            MessageBox.Show($"{result != null && result.RowCount >= 1}", "Evaluation Result");
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

        #region Events - Readers
        private void ExportReaderButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationDataReader reader = button.DataContext as ApplicationDataReader;

            string csv = reader.EvaluateTransform(out _, out _);
            if (!string.IsNullOrEmpty(csv))
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "CSV (*.csv)|*.csv|All (*.*)|*.*",
                    AddExtension = true,
                    Title = "Choose location to save file"
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(saveFileDialog.FileName, csv);
                }
            }
        }
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
            ReaderResultsView = resultCSV.CSVToDataView();
        }
        private void ReaderTransformSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationDataReader reader = button.DataContext as ApplicationDataReader;

            if (reader != null && !string.IsNullOrWhiteSpace(reader.Transform))
            {
                string resultCSV = reader.EvaluateTransform(out _, out _);
                ResultPreview = resultCSV.CSVToConsoleTable();
                ReaderResultsView = resultCSV.CSVToDataView();
            }
        }
        #endregion

        #region Events - Writers
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
        private void WriterOutputServiceProviderHelpButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationOutputWriter writer = button.DataContext as ApplicationOutputWriter;

            if (writer != null)
            {
                var documentation = writer.Parameters.GetPlainTextDocumentation();
                if (documentation != null)
                    MessageBox.Show(documentation, writer.ServiceProvider);
                else
                    MessageBox.Show($"No documentation has been provided on {writer.ServiceProvider} yet.", writer.ServiceProvider);
            };
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
                Name = $"Starting Step {processor.StartingSteps.Count + 1}",
                Action = RowProcessorActionNames.First()
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
                Name = $"Substep {step.NextSteps.Count + 1}",
                Action = RowProcessorActionNames.First()
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

            var results = Evaluation.TestProcessor(processor.StartingSteps, inputs);
            MessageBox.Show(string.Join('\n', results.Select(r => $"{r.Key}: {r.Value}")), "Evaluation Results");
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

        #region Events - Programs
        private void ProgramScriptAvalonTextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ApplicationScript script = editor.DataContext as ApplicationScript;
            if (script != null)
                editor.Text = script.Script;
        }
        private void ProgramScriptAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ApplicationScript script = editor.DataContext as ApplicationScript;
            editor.Text = script.Script;
        }
        private void ProgramScriptAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ApplicationScript script = editor.DataContext as ApplicationScript;
            script.Script = editor.Text;
        }
        #endregion

        #region Events - Workflows
        private void AddWorkflowStepButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationWorkflow workflow = button.DataContext as ApplicationWorkflow;

            ApplicationWorkflowStep step = new ()
            {
                Name = "Starting Step"
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
            workflow.ExecuteWorkflow(null);
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
                    Name = "New"
                };
                step.NextSteps.Add(nextStep);
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
        private void WorkflowStepCreateProgrammerButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationWorkflowStep step = button.DataContext as ApplicationWorkflowStep;
            ApplicationWorkflow workflow = button.Tag as ApplicationWorkflow;

            if (ApplicationData.Programs.Count == 0)
                MessageBox.Show("Expresso is not designed for extensive data processing. The Programmer feature is used to provide preliminary extensibility to the software. Be conservative.", "Usage Advice");

            ApplicationData.Programs.Add(new ApplicationScript()
            {
                Name = $"New Script {ApplicationData.Programs.Count + 1}",
                IOMode = ScriptIOMode.CSV
            });
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Processors));
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Programmer;
        }
        private void WorkflowStepPickActionItemButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ApplicationWorkflowStep step = button.DataContext as ApplicationWorkflowStep;
            ApplicationWorkflow workflow = button.Tag as ApplicationWorkflow;

            var currentApplicationData = ExpressoApplicationContext.ApplicationData;
            string[] entityNames = null;
            switch (step.ActionType)
            {
                case WorkflowActionType.Condition:
                    entityNames = currentApplicationData.Conditionals
                        .Select(c => c.Name).ToArray();
                    break;
                case WorkflowActionType.Reader:
                    entityNames = currentApplicationData.DataReaders
                        .Select(r => r.Name).ToArray();
                    break;
                case WorkflowActionType.Writer:
                    entityNames = currentApplicationData.OutputWriters
                        .Select(w => w.Name).ToArray();
                    break;
                case WorkflowActionType.RowProcessor:
                    entityNames = currentApplicationData.Processors
                        .Select(p => p.Name).ToArray();
                    break;
                case WorkflowActionType.Programmer:
                    entityNames = currentApplicationData.Programs
                        .Select(s => s.Name).ToArray();
                    break;
                case WorkflowActionType.Workflow:
                    entityNames = currentApplicationData.Workflows
                        .Except(new[] { workflow })
                        .Where(w => !ContainsWorkflowReference(w.StartingSteps, workflow.Name))
                        .Select(r => r.Name).ToArray();
                    break;
                default:
                    throw new ArgumentException("Invalid Action Type.");
            }

            if (entityNames.Length != 0)
            {
                string pick = ComboChoiceDialog.Prompt($"Pick {step.ActionType}", $"Select {step.ActionType} to use:", entityNames.FirstOrDefault(), entityNames);
                if (pick != null)
                    step.ActionItem = pick;
            }
            else
            {
                if (step.ActionType == WorkflowActionType.Workflow)
                    MessageBox.Show($"There is no suitable {step.ActionType} to pick from. Create a {step.ActionType} first. Notice that you cannot pick workflows that contains steps that references current workflow.", "No Suitable Pick");
                else
                    MessageBox.Show($"There is no suitable {step.ActionType} to pick from. Create a {step.ActionType} first.", "No Suitable Pick");
            }

            static bool ContainsWorkflowReference(ObservableCollection<ApplicationWorkflowStep> workflowSteps, string searchName)
            {
                foreach (ApplicationWorkflowStep step in workflowSteps)
                {
                    if (step.ActionType == WorkflowActionType.Workflow && step.ActionItem == searchName)
                        return true;
                    else if (ContainsWorkflowReference(step.NextSteps, searchName))
                        return true;
                }
                return false;
            }
        }
        #endregion

        #region Events - Engine
        private void EnginePageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ApplicationWorkflow workflow = comboBox.SelectedValue as ApplicationWorkflow;
            if (workflow != null)
            {
                List<(string Name, string Value)[]> permutations = ApplicationDataHelper.GatherVariablePermutations();
                DataTable dataTable = new DataTable();
                if (permutations.Count != 0)
                {
                    var headers = permutations.First().Select(p => p.Name);
                    foreach (string header in headers)
                        dataTable.Columns.Add(new DataColumn(header, typeof(string)));

                    foreach (var permutation in permutations)
                    {
                        string[] values = permutation.Select(p => p.Value).ToArray();

                        DataRow dataRow = dataTable.NewRow();
                        dataRow.ItemArray = values;
                        dataTable.Rows.Add(dataRow);
                    }
                }

                EnginePageDataGrid.ItemsSource = new DataView(dataTable);
            }
        }
        private void EnginePageRunWorkflowButton_Click(object sender, RoutedEventArgs e)
        {
            if (EnginePageComboBox.SelectedValue == null)
            {
                MessageBox.Show("Pick a workflow to run.");
                return;
            }

            var workflow = ApplicationData.Workflows.SingleOrDefault(w => w == EnginePageComboBox.SelectedValue as ApplicationWorkflow);
            if (workflow != null)
                workflow.ExecuteWorkflow(message => EnginePageWorkflowExecutionLog.Text += message + Environment.NewLine);
        }
        #endregion

        #region Events - About Panel
        private void AboutPageHyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Hyperlink hyperLink = (Hyperlink)sender;
            string navigateUri = hyperLink.NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri)
            {
                UseShellExecute = true
            });
            e.Handled = true;
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
                ApplicationData = new ApplicationData()
                {
                    Name = "New Analysis"
                };
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
        private void MenuItemFileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "Expresso (*.eso)|*.eso|All (*.*)|*.*",
                AddExtension = true,
                Title = "Choose location to save file as"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                CurrentFilePath = saveFileDialog.FileName;
                ApplicationDataSerializer.Save(CurrentFilePath, ApplicationData);
                WindowTitle = $"Expresso - {CurrentFilePath}";
            }
        }
        private void MenuItemCreateProcessor_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Processor;

            ApplicationData.Processors.Add(new ApplicationProcessor()
            {
                Name = $"New Processor {ApplicationData.Processors.Count + 1}"
            });
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Processors));
        }
        private void MenuItemCreateWorkflow_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Workflow;

            ApplicationData.Workflows.Add(new ApplicationWorkflow()
            {
                Name = $"New Workflow {ApplicationData.Workflows.Count + 1}"
            });
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Workflows));
        }
        private void MenuItemCreateVariable_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Variable;

            CurrentSelectedVariable = new ApplicationVariable()
            {
                Name = $"variable{ApplicationData.Variables.Count + 1}"
            };
            ApplicationData.Variables.Add(CurrentSelectedVariable);
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Variables));
        }
        private void MenuItemCreateCondition_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Condition;

            ApplicationData.Conditionals.Add(new ApplicationExecutionConditional()
            {
                Name = $"New Condition {ApplicationData.Conditionals.Count + 1}"
            });
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.Conditionals));
        }
        private void MenuItemCreateReader_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Reader;

            ApplicationData.DataReaders.Add(new ApplicationDataReader()
            {
                Name = $"New Reader {ApplicationData.DataReaders.Count + 1}"
            });
            ApplicationData.NotifyPropertyChanged(nameof(ApplicationData.DataReaders));
        }
        private void MenuItemCreateWriter_Click(object sender, RoutedEventArgs e)
        {
            MainTabControlTabIndex = (int)MainTabControlTabIndexMapping.Writer;

            var writer = new ApplicationOutputWriter()
            {
                Name = $"New Writer {ApplicationData.OutputWriters.Count + 1}",
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
        private void MenuItemExportScripts_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "Markdown (*.md)|*.md|All (*.*)|*.*",
                AddExtension = true,
                Title = "Choose location to save file"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string destination = saveFileDialog.FileName;
                int statistics = SaveApplicationDataScripts(ApplicationData, destination);
                MessageBox.Show($"{statistics} entries saved to {destination}", "File Save Result");
            }
        }
        private void MenuItemEngineRun_Click(object sender, RoutedEventArgs e)
        {
            var currentApplicationData = ApplicationData;
            string[] readerNames = currentApplicationData.Workflows
                .Select(r => r.Name).ToArray();
            if (readerNames.Length != 0)
            {
                string pick = ComboChoiceDialog.Prompt("Pick Workflow", "Select workflow to run:", readerNames.FirstOrDefault(), readerNames);
                if (pick != null)
                {
                    var workflow = ApplicationData.Workflows.FirstOrDefault(w => w.Name == pick);
                    workflow.ExecuteWorkflow(null);
                }
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

        private void CreateVariableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = CurrentFilePath != null;
        private void CreateVariableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
            => MenuItemCreateVariable_Click(null, null);
        private void CreateConditionCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = CurrentFilePath != null;
        private void CreateConditionCommand_Executed(object sender, ExecutedRoutedEventArgs e)
            => MenuItemCreateCondition_Click(null, null);
        private void CreateReaderCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = CurrentFilePath != null;
        private void CreateReaderCommand_Executed(object sender, ExecutedRoutedEventArgs e)
            => MenuItemCreateReader_Click(null, null);
        private void CreateWriterCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = CurrentFilePath != null;
        private void CreateWriterCommand_Executed(object sender, ExecutedRoutedEventArgs e)
            => MenuItemCreateWriter_Click(null, null);
        private void CreateRowProcessorCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = CurrentFilePath != null;
        private void CreateRowProcessorCommand_Executed(object sender, ExecutedRoutedEventArgs e)
            => MenuItemCreateProcessor_Click(null, null);
        private void CreateWorkflowCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
            => e.CanExecute = CurrentFilePath != null;
        private void CreateWorkflowCommand_Executed(object sender, ExecutedRoutedEventArgs e)
            => MenuItemCreateWorkflow_Click(null, null);

        private void RunEngineCommand_Executed(object sender, ExecutedRoutedEventArgs e)
            => MenuItemEngineRun_Click(null, null);
        private void RunEngineCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) 
            => e.CanExecute = CurrentFilePath != null && ApplicationData.Workflows.Count > 0;
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
        private int SaveApplicationDataScripts(ApplicationData applicationData, string destination)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(destination);
            string title = applicationData.Name;
            int savedItems = 0;

            StringBuilder markdownBuilder = new StringBuilder();
            markdownBuilder.AppendLine($"# {(string.IsNullOrWhiteSpace(title) ? "Analysis" : title)} - {fileName}\n");
            savedItems += SaveVariables(applicationData, markdownBuilder);
            savedItems += SaveReaders(applicationData, markdownBuilder);

            System.IO.File.WriteAllText(destination, markdownBuilder.ToString());
            return savedItems;

            static int SaveVariables(ApplicationData applicationData, StringBuilder markdownBuilder)
            {
                markdownBuilder.AppendLine($"## Variables\n");

                foreach (ApplicationVariable variable in applicationData.Variables)
                {
                    markdownBuilder.AppendLine($"{nameof(variable.Name)}: {variable.Name}  ");
                    markdownBuilder.AppendLine($"{nameof(variable.ValueType)}: {variable.ValueType}  ");
                    markdownBuilder.AppendLine($"{nameof(variable.SourceType)}: {variable.SourceType}  ");
                    if (!string.IsNullOrWhiteSpace(variable.Source))
                    {
                        var sourceLines = variable.Source.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (sourceLines.Length == 1)
                            markdownBuilder.AppendLine($"{nameof(variable.Source)}:  `{sourceLines.First()}`  ");
                        else
                            markdownBuilder.AppendLine($"{nameof(variable.Source)}:  \n{string.Join("  \n", sourceLines)}  ");
                    }
                    if (!string.IsNullOrWhiteSpace(variable.ArrayJoinSeparator))
                        markdownBuilder.AppendLine($"{nameof(variable.ArrayJoinSeparator)}: `{variable.ArrayJoinSeparator}`  ");
                    if (!string.IsNullOrWhiteSpace(variable.DefaultValue))
                        markdownBuilder.AppendLine($"{nameof(variable.DefaultValue)}: `{variable.DefaultValue}`  ");
                    markdownBuilder.AppendLine();
                }
                return applicationData.Variables.Count;
            }
            static int SaveReaders(ApplicationData applicationData, StringBuilder markdownBuilder)
            {
                foreach (ApplicationDataReader reader in applicationData.DataReaders)
                {
                    markdownBuilder.AppendLine($"## (Reader) {reader.Name}\n");
                    markdownBuilder.AppendLine($"{reader.Description}\n");
                    foreach (ApplicationDataQuery query in reader.DataQueries)
                    {
                        markdownBuilder.AppendLine($"### {query.ServiceProvider}: {query.Name}\n");
                        query.Parameters.BuildMarkdown(markdownBuilder);
                    }
                }
                return applicationData.DataReaders.Count;
            }
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
