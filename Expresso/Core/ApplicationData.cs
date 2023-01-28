using Expresso.Components;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;
using static Expresso.Core.ApplicationExecutionConditional;

namespace Expresso.Core
{
    public class ApplicationExecutionConditional : BaseNotifyPropertyChanged
    {
        public enum ConditionType
        {
            Binary,
            Switch
        }

        private ConditionType _Type;
        private string _Specification;
        private string _Parameter;

        public ConditionType Type { get => _Type; set => SetField(ref _Type, value); }
        public string Specification { get => _Specification; set => SetField(ref _Specification, value); }
        public string Parameter { get => _Parameter; set => SetField(ref _Parameter, value); }
    }
    /// <remarks>
    /// Root level step takes one or more inputs (with renaming) and just passes the inputs to outputs without any action
    /// </remarks>
    public class ApplicationProcessorStep : BaseNotifyPropertyChanged
    {
        public class ParameterMapping : BaseNotifyPropertyChanged
        {
            private string _FromName = string.Empty;
            private string _AsName = string.Empty;

            public string FromName { get => _FromName; set => SetField(ref _FromName, value); }
            public string AsName { get => _AsName; set => SetField(ref _AsName, value); }
        };

        private bool _IsStartingStep = false;
        private ObservableCollection<ParameterMapping> _Inputs = new();
        private string _Action = string.Empty;
        private ObservableCollection<ParameterMapping> _Outputs = new();
        private ObservableCollection<ApplicationProcessorStep> _NextSteps = new();
        private bool _IsFinalOutput = false;

        public bool IsStartingStep { get => _IsStartingStep; set => SetField(ref _IsStartingStep, value); }
        public ObservableCollection<ParameterMapping> Inputs { get => _Inputs; set => SetField(ref _Inputs, value); }
        public string Action { get => _Action; set => SetField(ref _Action, value); }
        public ObservableCollection<ParameterMapping> Outputs { get => _Outputs; set => SetField(ref _Outputs, value); }
        public ObservableCollection<ApplicationProcessorStep> NextSteps { get => _NextSteps; set => SetField(ref _NextSteps, value); }
        public bool IsFinalOutput { get => _IsFinalOutput; set => SetField(ref _IsFinalOutput, value); }
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
        private string _ServiceProvider = MainWindow.WriterDataServiceProviderNames.First();
        private string _DataSourceString = string.Empty;
        private string _Command = string.Empty;
        private string _AdditionalParameter = string.Empty;

        public string ServiceProvider { get => _ServiceProvider; set => SetField(ref _ServiceProvider, value); }
        public string DataSourceString { get => _DataSourceString; set => SetField(ref _DataSourceString, value); }
        public string Command { get => _Command; set => SetField(ref _Command, value); }
        public string AdditionalParameter { get => _AdditionalParameter; set => SetField(ref _AdditionalParameter, value); }
    }
    public class ApplicationWorkflow : BaseNotifyPropertyChanged
    {

    }
    public class ApplicationDataQuery : BaseNotifyPropertyChanged
    {
        private string _ServiceProvider = MainWindow.ReaderDataServiceProviderNames.First();
        private string _DataSourceString = string.Empty;
        private string _Query = string.Empty;
        private string _AdditionalParameter = string.Empty;

        public string ServiceProvider { get => _ServiceProvider; set => SetField(ref _ServiceProvider, value); }
        public string DataSourceString { get => _DataSourceString; set => SetField(ref _DataSourceString, value); }
        public string Query { get => _Query; set => SetField(ref _Query, value); }
        public string AdditionalParameter { get => _AdditionalParameter; set => SetField(ref _AdditionalParameter, value); }
    }
    public class ApplicationDataReader : BaseNotifyPropertyChanged
    {
        private string _Name = "Table";
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
        public enum VariableType
        {
            Fixed,
            CustomString,
            Integer,
            PickFromReaderQuery
        }

        private VariableType _Type;
        private string _Value;

        public VariableType Type { get => _Type; set => SetField(ref _Type, value); }
        public string Value { get => _Value; set => SetField(ref _Value, value); }
    }
    public class ApplicationData: BaseNotifyPropertyChanged
    {
        #region Metadata
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
