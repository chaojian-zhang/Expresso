using Expresso.Components;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using Expresso.Services;

namespace Expresso.Core
{
    public abstract class WriterParameterBase : BaseNotifyPropertyChanged
    {
        //private static readonly Dictionary<string, Action<string, string, string>> WriterDataServiceProviders = new Dictionary<string, Action<string, string, string>>()
        //{
        //    { "Output Arbitrary Text", WriteArbitraryText },
        //    { "Write Reader to ODBC", WriteReaderToODBC },
        //    { "Write Reader to CSV", WriteReaderToCSV },
        //    { "Write Reader to SQLite", WriteReaderToSQLite },
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
        public abstract string PerformAction(List<ParcelDataGrid> overwriteInputs);
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

        #region Helpers
        protected List<ParcelDataGrid> FetchInputs(List<ParcelDataGrid> overwriteInputs, IEnumerable<string> inputTableNames)
        {
            var current = ApplicationDataHelper.GetCurrentApplicationData();

            List<ParcelDataGrid> writerInputs = new List<ParcelDataGrid>();
            foreach (string name in inputTableNames)
            {
                // Use overwrite from supply
                var overwrite = overwriteInputs.FirstOrDefault(oi => oi.TableName == name);
                if (overwrite != null)
                    writerInputs.Add(overwrite);

                // Use name search from existing readers
                var reader = current.FindReaderWithName(name);
                if (reader != null)
                {
                    reader.EvaluateTransform(out ParcelDataGrid readerOutput, out _);
                    if(readerOutput != null)
                        writerInputs.Add(readerOutput);
                }
            }

            return writerInputs;
        }
        #endregion
    }

    public sealed class OutputWriterSQLiteCommandParameter : WriterParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Perform SQLite Command";
        #endregion

        #region Base Property
        private string _FilePath = string.Empty;
        private string _Query = string.Empty;
        #endregion

        #region Data Binding Setup
        public string FilePath { get => _FilePath; set => SetField(ref _FilePath, value); }
        public string Query { get => _Query; set => SetField(ref _Query, value); }
        #endregion

        #region Query Interface
        public override string PerformAction(List<ParcelDataGrid> overwriteInputs)
        {
            string[] commandStrings = Query.Split(';');
            try
            {
                foreach (var commandString in commandStrings)
                {
                    SqliteConnection SqliteConnection = new SqliteConnection($"Data Source={FilePath}");
                    SqliteConnection.Open();

                    var command = new SqliteCommand(commandString, SqliteConnection);
                    command.ExecuteNonQuery();

                    SqliteConnection.Close();
                }
                return "Done.";
            }
            catch (Exception e)
            {
                return "Error: " + e.Message;
            }
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(FilePath);
            writer.Write(Query);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            FilePath = reader.ReadString();
            Query = reader.ReadString();
        }
        #endregion
    }

    public sealed class OutputWriterODBCCommandParameter : WriterParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Perform ODBC Command";
        #endregion

        #region Base Property
        private string _DSN = string.Empty;
        private string _Query = string.Empty;
        #endregion

        #region Data Binding Setup
        public string DSN { get => _DSN; set => SetField(ref _DSN, value); }
        public string Query { get => _Query; set => SetField(ref _Query, value); }
        #endregion

        #region Query Interface
        public override string PerformAction(List<ParcelDataGrid> overwriteInputs)
        {
            string[] commandStrings = Query.Split(';');

            try
            {
                foreach (var commandString in commandStrings)
                {
                    var odbcConnection = new OdbcConnection($"DSN={DSN}");
                    odbcConnection.Open();

                    var odbcCommand = new OdbcCommand(commandString, odbcConnection);
                    odbcCommand.ExecuteNonQuery();

                    odbcConnection.Close();
                }

                return "Done.";
            }
            catch (Exception e)
            {
                return "Error: " + e.Message;
            }
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(DSN);
            writer.Write(Query);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            DSN = reader.ReadString();
            Query = reader.ReadString();
        }
        #endregion
    }

    public sealed class OutputWriterODBCWriterParameter : WriterParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Write to ODBC";
        #endregion

        #region Base Property
        private string _DSN = string.Empty;
        private string _TargetTableName = string.Empty;
        private string _Transform = string.Empty;
        private ObservableCollection<string> _InputTableNames = new ();
        #endregion

        #region Data Binding Setup
        public string DSN { get => _DSN; set => SetField(ref _DSN, value); }
        public string TargetTableName { get => _TargetTableName; set => SetField(ref _TargetTableName, value); }
        public string Transform { get => _Transform; set => SetField(ref _Transform, value); }
        public ObservableCollection<string> InputTableNames { get => _InputTableNames; set => SetField(ref _InputTableNames, value); }
        #endregion

        #region Query Interface
        public override string PerformAction(List<ParcelDataGrid> overwriteInputs)
        {
            var writerInputs = FetchInputs(overwriteInputs, InputTableNames);
            if (writerInputs.Count != 0 || InputTableNames.Count == 0)
            {
                try
                {
                    if (string.IsNullOrEmpty(Transform))
                        InMemorySQLIte.InsertODBCData(TargetTableName, writerInputs.Last(), DSN);
                    else
                    {
                        var finalDataGrid = writerInputs.ProcessDataGrids(Transform, out _);
                        InMemorySQLIte.InsertODBCData(TargetTableName, finalDataGrid, DSN);
                    }
                    return $"Data written to {TargetTableName} ({DSN}).";
                }
                catch (Exception e)
                {
                    return "Error: " + e.Message;
                }
            }
            return "Cannot find input or inputs evaluate to empty.";
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(DSN);
            writer.Write(TargetTableName);
            writer.Write(Transform);
            writer.Write(InputTableNames.Count);
            foreach (var item in InputTableNames)
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
                InputTableNames.Add(reader.ReadString());
        }
        #endregion
    }

    public sealed class OutputWriterCSVWriterParameter : WriterParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Write to CSV";
        #endregion

        #region Base Property
        private string _FilePath = string.Empty;
        private string _Transform = string.Empty;
        private ObservableCollection<string> _InputTableNames = new();
        #endregion

        #region Data Binding Setup
        public string FilePath { get => _FilePath; set => SetField(ref _FilePath, value); }
        public string Transform { get => _Transform; set => SetField(ref _Transform, value); }
        public ObservableCollection<string> InputTableNames { get => _InputTableNames; set => SetField(ref _InputTableNames, value); }
        #endregion

        #region Query Interface
        public override string PerformAction(List<ParcelDataGrid> overwriteInputs)
        {
            List<ParcelDataGrid> writerInputs = FetchInputs(overwriteInputs, InputTableNames);
            if (writerInputs.Count != 0 || InputTableNames.Count == 0)
            {
                if (string.IsNullOrEmpty(Transform))
                    File.WriteAllText(FilePath, writerInputs.Last().ToCSV());
                else
                {
                    ParcelDataGrid finalDataGrid = writerInputs.ProcessDataGrids(Transform, out _);
                    File.WriteAllText(FilePath, finalDataGrid.ToCSV());
                }
                return $"File written to {FilePath}";
            }
            return "Cannot find input or inputs evaluate to empty.";
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(FilePath);
            writer.Write(Transform);
            writer.Write(InputTableNames.Count);
            foreach (var item in InputTableNames)
                writer.Write(item);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            FilePath = reader.ReadString();
            Transform = reader.ReadString();
            var inputDataNamesLength = reader.ReadInt32();
            for (int i = 0; i < inputDataNamesLength; i++)
                InputTableNames.Add(reader.ReadString());
        }
        #endregion
    }

    public sealed class OutputWriterExcelWriterParameter : WriterParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Write to Excel";
        #endregion

        #region Base Property
        private string _FilePath = string.Empty;
        private string _Transform = string.Empty;
        private ObservableCollection<string> _InputTableNames = new();
        #endregion

        #region Data Binding Setup
        public string FilePath { get => _FilePath; set => SetField(ref _FilePath, value); }
        public string Transform { get => _Transform; set => SetField(ref _Transform, value); }
        public ObservableCollection<string> InputTableNames { get => _InputTableNames; set => SetField(ref _InputTableNames, value); }
        #endregion

        #region Query Interface
        public override string PerformAction(List<ParcelDataGrid> overwriteInputs)
        {
            List<ParcelDataGrid> writerInputs = FetchInputs(overwriteInputs, InputTableNames);
            if (writerInputs.Count != 0 || InputTableNames.Count == 0)
            {
                if (string.IsNullOrEmpty(Transform))
                    WriteResult(writerInputs);
                else
                {
                    ParcelDataGrid finalDataGrid = writerInputs.ProcessDataGrids(Transform, out _);
                    WriteResult(new List<ParcelDataGrid> { finalDataGrid });
                }
                return $"File written to {FilePath}";
            }
            return "Cannot find input or inputs evaluate to empty.";

            void WriteResult(List<ParcelDataGrid> writerInputs)
            {
                DataSet ds = new("New_DataSet")
                {
                    Locale = System.Threading.Thread.CurrentThread.CurrentCulture
                };

                foreach (ParcelDataGrid grid in writerInputs)
                {
                    DataTable dt = new(grid.TableName)
                    {
                        Locale = System.Threading.Thread.CurrentThread.CurrentCulture
                    };

                    ds.Tables.Add(grid.ToDataTable());
                }

                ExcelLibrary.DataSetHelper.CreateWorkbook(FilePath, ds);
            }
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(FilePath);
            writer.Write(Transform);
            writer.Write(InputTableNames.Count);
            foreach (var item in InputTableNames)
                writer.Write(item);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            FilePath = reader.ReadString();
            Transform = reader.ReadString();
            var inputDataNamesLength = reader.ReadInt32();
            for (int i = 0; i < inputDataNamesLength; i++)
                InputTableNames.Add(reader.ReadString());
        }
        #endregion
    }
}
