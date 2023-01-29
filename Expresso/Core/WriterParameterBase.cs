using Expresso.Components;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using Csv;
using System.Collections;
using Microsoft.AnalysisServices.AdomdClient;
using System.Data.SQLite;

namespace Expresso.Core
{
    public abstract class WriterParameterBase : BaseNotifyPropertyChanged
    {
        #region Reflection Service
        private static Dictionary<string, Type> _ServiceProviders;
        public static Dictionary<string, Type> GetServiceProviders()
        {
            if (_ServiceProviders == null)
                _ServiceProviders = Assembly
                    .GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t != typeof(WriterParameterBase) && typeof(WriterParameterBase).IsAssignableFrom(t))
                    .ToDictionary(t => t.GetProperty(nameof(DisplayName)).GetValue(null) as string, t => t);

            return _ServiceProviders;
        }
        #endregion

        #region Base Property
        private string _Output = string.Empty;
        #endregion

        #region Data Binding Setup
        public string Output { get => _Output; set => SetField(ref _Output, value); }
        #endregion

        #region Static Metadata Properties
        public static string DisplayName => "Basic Writer";
        #endregion

        #region Query Interface
        public abstract void PerformAction();
        #endregion

        #region Serialization Interface
        public virtual void WriteToStream(BinaryWriter writer)
        {
            writer.Write(Output);
        }
        public virtual void ReadFromStream(BinaryReader reader)
        {
            Output = reader.ReadString();
        }
        #endregion
    }
}
