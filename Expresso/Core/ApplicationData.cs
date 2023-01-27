using Expresso.Components;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace Expresso.Core
{
    public class ApplicationExecutionTrigger : BaseNotifyPropertyChanged
    {

    }
    public class ApplicationOutputWriter : BaseNotifyPropertyChanged
    {

    }
    public class ApplicationSequentialWorkflow : BaseNotifyPropertyChanged
    {

    }
    public class ApplicationDataTransform : BaseNotifyPropertyChanged
    {

    }
    public class ApplicationDataQuery : BaseNotifyPropertyChanged
    {
        private string _ServiceProvider = "ODBC";
        private string _DataSourceString = string.Empty;
        private string _Query = string.Empty;

        public string ServiceProvider { get => _ServiceProvider; set => SetField(ref _ServiceProvider, value); }
        public string DataSourceString { get => _DataSourceString; set => SetField(ref _DataSourceString, value); }
        public string Query { get => _Query; set => SetField(ref _Query, value); }
    }
    public class ApplicationDataSource : BaseNotifyPropertyChanged
    {
        private ObservableCollection<ApplicationDataQuery> _DataQueries = new();
        private ObservableCollection<ApplicationDataTransform> _Transforms = new();

        public ObservableCollection<ApplicationDataQuery> DataQueries { get => _DataQueries; set => SetField(ref _DataQueries, value); }
        public ObservableCollection<ApplicationDataTransform> Transforms { get => _Transforms; set => SetField(ref _Transforms, value); }
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

        #region Triggers

        #endregion

        #region Data Sources
        private ObservableCollection<ApplicationDataSource> _DataSources = new();
        #endregion

        #region Data Bindings
        public string Name { get => _Name; set => SetField(ref _Name, value); }
        public string Description { get => _Description; set => SetField(ref _Description, value); }
        public long Iteration { get => _Iteration; set => SetField(ref _Iteration, value); }
        public DateTime CreationTime { get => _CreationTime; set => SetField(ref _CreationTime, value); }
        public DateTime LastModifiedTime { get => _LastModifiedTime; set => SetField(ref _LastModifiedTime, value); }
        public ObservableCollection<ApplicationDataSource> DataSources { get => _DataSources; set => SetField(ref _DataSources, value);
    }
    #endregion
}
}
