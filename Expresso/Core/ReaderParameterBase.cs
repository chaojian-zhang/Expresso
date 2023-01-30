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
using Microsoft.Data.Sqlite;

namespace Expresso.Core
{
    public abstract class ReaderDataQueryParameterBase : BaseNotifyPropertyChanged
    {
        #region Reflection Service
        private static Dictionary<string, Type> _ServiceProviders;
        public static Dictionary<string, Type> GetServiceProviders()
        {
            if (_ServiceProviders == null)
                _ServiceProviders = Assembly
                    .GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t != typeof(ReaderDataQueryParameterBase) && typeof(ReaderDataQueryParameterBase).IsAssignableFrom(t))
                    .ToDictionary(t => t.GetProperty(nameof(DisplayName)).GetValue(null) as string, t => t);

            return _ServiceProviders;
        }
        #endregion

        #region Base Property
        private string _Query = string.Empty;
        #endregion

        #region Data Binding Setup
        public string Query { get => _Query; set => SetField(ref _Query, value); }
        #endregion

        #region Static Metadata Properties
        public static string DisplayName => "Base Query";
        #endregion

        #region Query Interface
        /// <summary>
        /// Returns CSV string
        /// </summary>
        public abstract string MakeQuery();
        #endregion

        #region Serialization Interface
        public virtual void WriteToStream(BinaryWriter writer)
        {
            writer.Write(Query);
        }
        public virtual void ReadFromStream(BinaryReader reader)
        {
            Query = reader.ReadString();
        }
        #endregion
    }

    public sealed class ODBCReaderDataQueryParameter : ReaderDataQueryParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "ODBC";
        #endregion

        #region Base Property
        private string _DSN = string.Empty;
        #endregion

        #region Data Binding Setup
        public string DSN { get => _DSN; set => SetField(ref _DSN, value); }
        #endregion

        #region Query Interface
        public override string MakeQuery()
        {
            try
            {
                var odbcConnection = new OdbcConnection($"DSN={DSN}");
                odbcConnection.Open();
                var dataTable = new DataTable();
                dataTable.Load(new OdbcCommand(Query, odbcConnection).ExecuteReader());
                odbcConnection.Close();
                return dataTable.ToCSV();
            }
            catch (Exception e)
            {
                return $"Result,Message\nError,\"{e.Message}\"";
            }
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(DSN);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            DSN = reader.ReadString();
        }
        #endregion
    }

    public sealed class MicrosoftAnalysisServiceDataQueryParameter : ReaderDataQueryParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Microsoft Analysis Service";
        #endregion

        #region Base Property
        private string _ConnectionString = string.Empty;
        #endregion

        #region Data Binding Setup
        public string ConnectionString { get => _ConnectionString; set => SetField(ref _ConnectionString, value); }
        #endregion

        #region Query Interface
        public override string MakeQuery()
        {
            try
            {
                using AdomdConnection conn = new AdomdConnection(ConnectionString);
                conn.Open();
                using AdomdCommand cmd = new AdomdCommand(Query.TrimEnd(';'), conn);
                CellSet result = cmd.ExecuteCellSet();
                return result.CellSetToTableNew().ToCSVFull();
            }
            catch (Exception e)
            {
                return $"Result,Message\nError,\"{e.Message}\"";
            }
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(ConnectionString);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            ConnectionString = reader.ReadString();
        }
        #endregion
    }

    public sealed class CSVReaderDataQueryParameter : ReaderDataQueryParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "CSV";
        #endregion

        #region Base Property
        private string _FilePath = string.Empty;
        #endregion

        #region Data Binding Setup
        public string FilePath { get => _FilePath; set => SetField(ref _FilePath, value); }
        #endregion

        #region Query Interface
        public override string MakeQuery()
        {
            return File.ReadAllText(_FilePath);
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(FilePath);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            FilePath = reader.ReadString();
        }
        #endregion
    }

    public sealed class ExcelReaderDataQueryParameter : ReaderDataQueryParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Excel";
        #endregion

        #region Base Property
        private string _FilePath = string.Empty;
        private string _Worksheet = string.Empty;
        #endregion

        #region Data Binding Setup
        public string FilePath { get => _FilePath; set => SetField(ref _FilePath, value); }
        public string Worksheet { get => _Worksheet; set => SetField(ref _Worksheet, value); }
        #endregion

        #region Query Interface
        public override string MakeQuery()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(FilePath);
            writer.Write(Worksheet);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            FilePath = reader.ReadString();
            Worksheet = reader.ReadString();
        }
        #endregion
    }

    public sealed class SQLiteReaderDataQueryParameter : ReaderDataQueryParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "SQLite";
        #endregion

        #region Base Property
        private string _FilePath = string.Empty;
        #endregion

        #region Data Binding Setup
        public string FilePath { get => _FilePath; set => SetField(ref _FilePath, value); }
        #endregion

        #region Query Interface
        public override string MakeQuery()
        {
            try
            {
                using SqliteConnection SqliteConnection = new SqliteConnection($"Data Source={FilePath}");
                SqliteConnection.Open();

                var dataTable = new DataTable();
                dataTable.Load(new SqliteCommand(Query.TrimEnd(';'), SqliteConnection).ExecuteReader());

                SqliteConnection.Close();
                return dataTable.ToCSV();
            }
            catch (Exception e)
            {
                return $"Result,Message\nError,\"{e.Message}\"";
            }
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(FilePath);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            FilePath = reader.ReadString();
        }
        #endregion
    }

    public sealed class EnvironmentVariablesReaderDataQueryParameter : ReaderDataQueryParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Environment Variables";
        #endregion

        #region Base Property
        private string _SpecificVariable = string.Empty;
        #endregion

        #region Data Binding Setup
        public string SpecificVariable { get => _SpecificVariable; set => SetField(ref _SpecificVariable, value); }
        #endregion

        #region Query Interface
        public override string MakeQuery()
        {
            if (string.IsNullOrWhiteSpace(SpecificVariable))
            {
                string[] headers = new string[]
                {
                    "Environment Variable",
                    "Value"
                };

                List<string[]> lines = new();
                foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
                    lines.Add(new string[]
                    {
                        variable.Key.ToString(),
                        variable.Value.ToString()
                    });

                return CsvWriter.WriteToText(headers, lines);
            }
            else
            {
                string variable = Environment.GetEnvironmentVariable(SpecificVariable);
                if (variable == null)
                    return "Environment Variable,Value";

                string[] headers = new string[]
                {
                    "Item",
                    "Value"
                };
                string[] variableValues = variable.Split(';').Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
                List<string[]> lines = new();
                for (int i = 0; i < variableValues.Length; i++)
                {
                    string value = variableValues[i];
                    lines.Add(new string[]
                    {
                        i.ToString(),
                        value
                    });
                }

                return CsvWriter.WriteToText(headers, lines);
            }
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(SpecificVariable);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            SpecificVariable = reader.ReadString();
        }
        #endregion
    }

    public sealed class FolderFilePathsReaderDataQueryParameter : ReaderDataQueryParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Folder Filepaths";
        #endregion

        #region Base Property
        private string _FolderPath = string.Empty;
        #endregion

        #region Data Binding Setup
        public string FolderPath { get => _FolderPath; set => SetField(ref _FolderPath, value); }
        #endregion

        #region Query Interface
        public override string MakeQuery()
        {
            string[] headers = new string[]
            {
                "File Paths",
                "File Names",
                "Type/Extensions",
                "Sizes"
            };

            List<string[]> lines = new();
            foreach (FileSystemInfo item in new DirectoryInfo(FolderPath).EnumerateFileSystemInfos())
                lines.Add(new string[]
                {
                    item.FullName,
                    item.Name,
                    item is DirectoryInfo ? "Folder" : item.Extension,
                    item is FileInfo file ? file.Length.ToString() : string.Empty
                });

            return CsvWriter.WriteToText(headers, lines);
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(FolderPath);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            FolderPath = reader.ReadString();
        }
        #endregion
    }

    public sealed class ExpressorReaderDataQueryParameter : ReaderDataQueryParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Existing Readers";
        #endregion

        #region Base Property
        private string _ReaderName = string.Empty;
        #endregion

        #region Data Binding Setup
        public string ReaderName { get => _ReaderName; set => SetField(ref _ReaderName, value); }
        #endregion

        #region Query Interface
        public override string MakeQuery()
        {
            var current = ApplicationDataHelper.GetCurrentApplicationData();
            var reader = current.FindReaderWithName(ReaderName);
            if (reader != null)
                return reader.EvaluateTransform(out _, out _);
            else return $"Result,Message\nError,Cannot find reader.";
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(ReaderName);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            ReaderName = reader.ReadString();
        }
        #endregion
    }
}
