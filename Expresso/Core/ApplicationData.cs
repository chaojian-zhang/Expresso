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
    public class ApplicationOutputWriter : BaseNotifyPropertyChanged
    {
        private string _ServiceProvider = MainWindow.WriterDataServiceProviderNames.First();
        private string _DataSourceString = string.Empty;
        private string _Query = string.Empty;

        public string ServiceProvider { get => _ServiceProvider; set => SetField(ref _ServiceProvider, value); }
        public string DataSourceString { get => _DataSourceString; set => SetField(ref _DataSourceString, value); }
        public string Command { get => _Query; set => SetField(ref _Query, value); }
    }
    public class ApplicationWorkflow : BaseNotifyPropertyChanged
    {

    }
    public class ApplicationDataQuery : BaseNotifyPropertyChanged
    {
        private string _ServiceProvider = MainWindow.ReaderDataServiceProviderNames.First();
        private string _DataSourceString = string.Empty;
        private string _Query = string.Empty;

        public string ServiceProvider { get => _ServiceProvider; set => SetField(ref _ServiceProvider, value); }
        public string DataSourceString { get => _DataSourceString; set => SetField(ref _DataSourceString, value); }
        public string Query { get => _Query; set => SetField(ref _Query, value); }
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
        public ObservableCollection<ApplicationWorkflow> Workflows { get => _Workflows; set => SetField(ref _Workflows, value); }
        #endregion
    }
}
