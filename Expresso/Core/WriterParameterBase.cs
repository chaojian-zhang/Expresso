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
using System.Data.Entity.Infrastructure;
using System.Windows;
using System.Collections.ObjectModel;

namespace Expresso.Core
{
    public abstract class WriterParameterBase : BaseNotifyPropertyChanged
    {
        //private static readonly Dictionary<string, Action<string, string, string>> WriterDataServiceProviders = new Dictionary<string, Action<string, string, string>>()
        //{
        //    { "Execute ODBC Command", ExecuteODBCNonQuery },
        //    { "Execute SQLite Command", ExecuteSQLiteNonQuery },
        //    { "Output Arbitrary Text", WriteArbitraryText },
        //    { "Write Reader to ODBC", WriteReaderToODBC },
        //    { "Write Reader to CSV", WriteReaderToCSV },
        //    { "Write Reader to SQLite", WriteReaderToSQLite },
        //    { "Export Reader to CSV", WriteReaderToSQLite },
        //    { "Export Readers to SQLite", WriteReaderToSQLite },
        //    { "Export Readers to Excel", WriteReaderToSQLite },
        //};

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

    public sealed class ODBCWriterParameter : WriterParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Write Reader to ODBC";
        #endregion

        #region Base Property
        private string _DSN = string.Empty;
        private string _TargetTableName = string.Empty;
        private string _Transform = string.Empty;
        private ObservableCollection<string> _InputDataNames = new ();
        #endregion

        #region Data Binding Setup
        public string DSN { get => _DSN; set => SetField(ref _DSN, value); }
        public string TargetTableName { get => _TargetTableName; set => SetField(ref _TargetTableName, value); }
        public string Transform { get => _Transform; set => SetField(ref _Transform, value); }
        public ObservableCollection<string> InputDataNames { get => _InputDataNames; set => SetField(ref _InputDataNames, value); }
        #endregion

        #region Query Interface
        public override void PerformAction()
        {
            var current = ApplicationDataHelper.GetCurrentApplicationData();
            var reader = current.FindReaderWithName(TargetTableName);
            if (reader != null)
            {
                reader.EvaluateTransform(out ParcelDataGrid dataGrid, out _);

                try
                {
                    var odbcConnection = new OdbcConnection($"DSN={DSN}");
                    odbcConnection.Open();

                    InMemorySQLIte.InsertODBCData(dataGrid.TableName, dataGrid, DSN);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Error");
                }
            }
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(DSN);
            writer.Write(TargetTableName);
            writer.Write(Transform);
            writer.Write(InputDataNames.Count);
            foreach (var item in InputDataNames)
                writer.Write(item);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            DSN = reader.ReadString();
            TargetTableName = reader.ReadString();
            Transform = reader.ReadString();
            var inputDataNamesLength = reader.ReadInt32();
            for (int i = 0; i < inputDataNamesLength; i++)
                InputDataNames.Add(reader.ReadString());
        }
        #endregion
    }
}
