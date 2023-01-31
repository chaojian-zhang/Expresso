using Expresso.Components;
using System;
using System.Collections.ObjectModel;

namespace Expresso.Core
{
    public enum ConditionType
    {
        Binary,
        Switch
    }
    public enum VariableValueType
    {
        SingleValue,
        MultiValueArray,
        Iterator
    }
    public enum VariableSourceType
    {
        Fixed,
        Reader,
        CustomList
    }

    public class ApplicationExecutionConditional : BaseNotifyPropertyChanged
    {

        private string _Name = string.Empty;
        private ConditionType _Type;
        private string _Description = string.Empty;
        private string _ReaderName = string.Empty;

        public string Name { get => _Name; set => SetField(ref _Name, value); }
        public ConditionType Type { get => _Type; set => SetField(ref _Type, value); }
        public string Description { get => _Description; set => SetField(ref _Description, value); }
        public string ReaderName { get => _ReaderName; set => SetField(ref _ReaderName, value); }
    }
    public class ApplicationWorkflowStep : BaseNotifyPropertyChanged
    {
        private string _Name = string.Empty;
        private string _ActionType = string.Empty;
        private string _ActionItem = string.Empty;
        private ObservableCollection<ApplicationWorkflowStep> _NextSteps = new();

        public string Name { get => _Name; set => SetField(ref _Name, value); }
        public string ActionType { get => _ActionType; set => SetField(ref _ActionType, value); }
        public string ActionItem { get => _ActionItem; set => SetField(ref _ActionItem, value); }
        public ObservableCollection<ApplicationWorkflowStep> NextSteps { get => _NextSteps; set => SetField(ref _NextSteps, value); }
    }

    public class ApplicationProcessorStep : BaseNotifyPropertyChanged
    {
        public class ParameterMapping : BaseNotifyPropertyChanged
        {
            private string _FromName = string.Empty;
            private string _AsName = string.Empty;

            public string FromName { get => _FromName; set => SetField(ref _FromName, value); }
            public string AsName { get => _AsName; set => SetField(ref _AsName, value); }
        };

        private string _Name = string.Empty;
        private ObservableCollection<ParameterMapping> _Inputs = new();
        private string _Action = string.Empty;
        private ObservableCollection<ParameterMapping> _Outputs = new();
        private ObservableCollection<ApplicationProcessorStep> _NextSteps = new();
        private bool _IsFinalOutput = false;

        public string Name { get => _Name; set => SetField(ref _Name, value); }
        public ObservableCollection<ParameterMapping> Inputs { get => _Inputs; set => SetField(ref _Inputs, value); }
        public string Action { get => _Action; set => SetField(ref _Action, value); }
        public ObservableCollection<ParameterMapping> Outputs { get => _Outputs; set => SetField(ref _Outputs, value); }
        public bool IsFinalOutput { get => _IsFinalOutput; set => SetField(ref _IsFinalOutput, value); }
        public ObservableCollection<ApplicationProcessorStep> NextSteps { get => _NextSteps; set => SetField(ref _NextSteps, value); }
    }
    public class ApplicationProcessor: BaseNotifyPropertyChanged
    {
        private string _Name = string.Empty;
        private string _Description = string.Empty;
        private ObservableCollection<ApplicationProcessorStep> _StartingSteps = new ObservableCollection<ApplicationProcessorStep>();

        public string Name { get => _Name; set => SetField(ref _Name, value); }
        public string Description { get => _Description; set => SetField(ref _Description, value); }
        public ObservableCollection<ApplicationProcessorStep> StartingSteps { get => _StartingSteps; set => SetField(ref _StartingSteps, value); }

        #region View Use
        private ObservableCollection<ApplicationProcessorStep> _ListingOfAllSteps = new ObservableCollection<ApplicationProcessorStep>();
        public ObservableCollection<ApplicationProcessorStep> ListingOfAllSteps { get => _ListingOfAllSteps; set => SetField(ref _ListingOfAllSteps, value); }
        #endregion
    }
    public class ApplicationOutputWriter : BaseNotifyPropertyChanged
    {
        private string _Name = string.Empty;
        private string _ServiceProvider = null;
        private WriterParameterBase _Parameters = null;

        public string Name { get => _Name; set => SetField(ref _Name, value); }
        public string ServiceProvider
        {
            get => _ServiceProvider;
            set
            {
                if (_ServiceProvider != value)
                    _Parameters = (WriterParameterBase)Activator.CreateInstance(WriterParameterBase.GetServiceProviders()[value]);

                SetField(ref _ServiceProvider, value);
                NotifyPropertyChanged(nameof(Parameters));
            }
        }
        public WriterParameterBase Parameters { get => _Parameters; set => SetField(ref _Parameters, value); }
    }
    public class ApplicationWorkflow : BaseNotifyPropertyChanged
    {
        private string _Name = string.Empty;
        private string _Description = string.Empty;
        private ObservableCollection<ApplicationWorkflowStep> _StartingSteps = new();

        public string Name { get => _Name; set => SetField(ref _Name, value); }
        public string Description { get => _Description; set => SetField(ref _Description, value); }
        public ObservableCollection<ApplicationWorkflowStep> StartingSteps { get => _StartingSteps; set => SetField(ref _StartingSteps, value); }
    }
    public class ApplicationDataQuery : BaseNotifyPropertyChanged
    {
        private string _Name = string.Empty;
        private string _ServiceProvider = null;
        private ReaderDataQueryParameterBase _Parameters = null;

        public string Name { get => _Name; set => SetField(ref _Name, value); }
        public string ServiceProvider 
        { 
            get => _ServiceProvider;
            set
            {
                if (_ServiceProvider != value)
                    _Parameters = (ReaderDataQueryParameterBase)Activator.CreateInstance(ReaderDataQueryParameterBase.GetServiceProviders()[value]);

                SetField(ref _ServiceProvider, value);
                NotifyPropertyChanged(nameof(Parameters));
            }
        }
        public ReaderDataQueryParameterBase Parameters { get => _Parameters; set => SetField(ref _Parameters, value); }
    }
    public class ApplicationDataReader : BaseNotifyPropertyChanged
    {
        private string _Name = string.Empty;
        private string _Description = string.Empty;

        private ObservableCollection<ApplicationDataQuery> _DataQueries = new();
        private string _Transform = string.Empty;

        public string Name { get => _Name; set => SetField(ref _Name, value); }
        public string Description { get => _Description; set => SetField(ref _Description, value); }
        public ObservableCollection<ApplicationDataQuery> DataQueries { get => _DataQueries; set => SetField(ref _DataQueries, value); }
        public string Transform { get => _Transform; set => SetField(ref _Transform, value); }
    }
    public class ApplicationVariable: BaseNotifyPropertyChanged
    {
        private string _Name = string.Empty;
        private VariableValueType _ValueType;
        private VariableSourceType _SourceType;
        private string _Source = string.Empty;
        private string _ArrayJoinSeparator = ", ";
        private string _DefaultValue = string.Empty;

        public string Name { get => _Name; set => SetField(ref _Name, value); }
        public VariableValueType ValueType { get => _ValueType; set => SetField(ref _ValueType, value); }
        public VariableSourceType SourceType { get => _SourceType; set => SetField(ref _SourceType, value); }
        public string Source { get => _Source; set => SetField(ref _Source, value); }
        public string ArrayJoinSeparator { get => _ArrayJoinSeparator; set => SetField(ref _ArrayJoinSeparator, value); }
        public string DefaultValue { get => _DefaultValue; set => SetField(ref _DefaultValue, value); }
    }
    public class ApplicationData: BaseNotifyPropertyChanged
    {
        #region Metadata
        private string _FileVersion = Program.ProgramVersion;
        private string _Name = "New Docment";
        private string _Description = string.Empty;
        private long _Iteration = 0;
        private DateTime _CreationTime = DateTime.Now;
        private DateTime _LastModifiedTime = DateTime.Now;
        #endregion

        #region Main Configurations
        private ObservableCollection<ApplicationExecutionConditional> _Conditionals = new();
        private ObservableCollection<ApplicationDataReader> _DataReaders = new();
        private ObservableCollection<ApplicationOutputWriter> _OutputWriters = new();
        private ObservableCollection<ApplicationVariable> _Variables = new();
        private ObservableCollection<ApplicationProcessor> _Processors = new();
        private ObservableCollection<ApplicationWorkflow> _Workflows = new();
        #endregion

        #region Data Bindings
        public string FileVersion { get => _FileVersion; set => SetField(ref _FileVersion, value); }
        public string Name { get => _Name; set => SetField(ref _Name, value); }
        public string Description { get => _Description; set => SetField(ref _Description, value); }
        public long Iteration { get => _Iteration; set => SetField(ref _Iteration, value); }
        public DateTime CreationTime { get => _CreationTime; set => SetField(ref _CreationTime, value); }
        public DateTime LastModifiedTime { get => _LastModifiedTime; set => SetField(ref _LastModifiedTime, value); }
        public ObservableCollection<ApplicationExecutionConditional> Conditionals { get => _Conditionals; set => SetField(ref _Conditionals, value); }
        public ObservableCollection<ApplicationDataReader> DataReaders { get => _DataReaders; set => SetField(ref _DataReaders, value); }
        public ObservableCollection<ApplicationOutputWriter> OutputWriters { get => _OutputWriters; set => SetField(ref _OutputWriters, value); }
        public ObservableCollection<ApplicationVariable> Variables { get => _Variables; set => SetField(ref _Variables, value); }
        public ObservableCollection<ApplicationProcessor> Processors { get => _Processors; set => SetField(ref _Processors, value); }
        public ObservableCollection<ApplicationWorkflow> Workflows { get => _Workflows; set => SetField(ref _Workflows, value); }
        #endregion
    }
}
