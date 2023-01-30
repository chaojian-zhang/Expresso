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
using ExcelDataReader;
using System.Text;
using System.Security.Policy;
using System.Net.Http;
using System.Threading.Tasks;

namespace Expresso.Core
{
    public enum SupportedWebRequestMethod
    {
        GET,
        POST
    }

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

        #region Script Exporting Interface
        public virtual void BuildMarkdown(StringBuilder builder)
        {
            if (!string.IsNullOrWhiteSpace(Query))
            {
                builder.AppendLine("```sql");
                builder.AppendLine(Query);
                builder.AppendLine("```\n");
            }
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

        #region Script Exporting Interface
        public override void BuildMarkdown(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(DSN)) 
                builder.AppendLine($"DSN: {DSN}\n");
            base.BuildMarkdown(builder);
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
        private string _Transform = string.Empty;
        #endregion

        #region Data Binding Setup
        public string ConnectionString { get => _ConnectionString; set => SetField(ref _ConnectionString, value); }
        public string Transform { get => _Transform; set => SetField(ref _Transform, value); }
        #endregion

        #region Query Interface
        public override string MakeQuery()
        {
            try
            {
                using AdomdConnection conn = new(ConnectionString);
                conn.Open();
                using AdomdCommand cmd = new(Query.TrimEnd(';'), conn);
                string mdxResult = cmd.ExecuteCellSet().CellSetToTableNew().ToCSVFull();
                conn.Close();

                if (!string.IsNullOrWhiteSpace(Transform))
                {
                    var current = ApplicationDataHelper.GetCurrentApplicationData();
                    string tableName = current.FindReaderDataQueryFromParameters(this).Name;
                    return SQLiteHelper.TransformCSV(tableName, mdxResult, Transform, out _, out _);
                }
                else return mdxResult;
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
            writer.Write(Transform);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            ConnectionString = reader.ReadString();
            Transform = reader.ReadString();
        }
        #endregion

        #region Script Exporting Interface
        public override void BuildMarkdown(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(ConnectionString))
                builder.AppendLine($"Connection String: `{ConnectionString}`\n");
            base.BuildMarkdown(builder);
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
            string csv = File.ReadAllText(_FilePath);

            if (!string.IsNullOrWhiteSpace(Query))
            {
                var current = ApplicationDataHelper.GetCurrentApplicationData();
                string tableName = current.FindReaderFromParameters(this).Name;
                return SQLiteHelper.TransformCSV(tableName, csv, Query, out _, out _);
            }
            else return csv;
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

        #region Script Exporting Interface
        public override void BuildMarkdown(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(FilePath))
                builder.AppendLine($"File Path: {FilePath}\n");
            base.BuildMarkdown(builder);
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
            using FileStream stream = File.Open(FilePath, FileMode.Open, FileAccess.Read);
            using IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream);
            DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });

            if (string.IsNullOrWhiteSpace(Worksheet))
                return result.Tables[0].ToCSV();
            else 
                return result.Tables[Worksheet].ToCSV();
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

        #region Script Exporting Interface
        public override void BuildMarkdown(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(FilePath))
                builder.AppendLine($"File Path: {FilePath}  ");
            if (!string.IsNullOrEmpty(Worksheet))
                builder.AppendLine($"Worksheet: {Worksheet}\n");
            base.BuildMarkdown(builder);
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

        #region Script Exporting Interface
        public override void BuildMarkdown(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(FilePath))
                builder.AppendLine($"File Path: {FilePath}\n");
            base.BuildMarkdown(builder);
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

        #region Script Exporting Interface
        public override void BuildMarkdown(StringBuilder builder)
        {
            if (!string.IsNullOrWhiteSpace(SpecificVariable))
                builder.AppendLine($"Specific Variable: {SpecificVariable}\n");
            base.BuildMarkdown(builder);
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

        #region Script Exporting Interface
        public override void BuildMarkdown(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(FolderPath))
                builder.AppendLine($"Folder Path: {FolderPath}\n");
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

        #region Script Exporting Interface
        public override void BuildMarkdown(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(ReaderName))
                builder.AppendLine($"Reader Name: {ReaderName}\n");
        }
        #endregion
    }

    public sealed class WebRequestReaderDataQueryParameter : ReaderDataQueryParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Web Request";
        #endregion

        #region Base Property
        private string _URL = string.Empty;
        private string _Method = nameof(SupportedWebRequestMethod.GET);
        #endregion

        #region Data Binding Setup
        public string URL { get => _URL; set => SetField(ref _URL, value); }
        public string Method { get => _Method; set => SetField(ref _Method, value); }
        #endregion

        #region Query Interface
        public override string MakeQuery()
        {
            // Remark-cz: Will make all MakeQuery into async by default in the future
            return MakeQueryAsync().Result;
        }
        private async Task<string> MakeQueryAsync()
        {
            try
            {
                SupportedWebRequestMethod method = Enum.Parse<SupportedWebRequestMethod>(Method);
                string responseBody = string.Empty;
                switch (method)
                {
                    case SupportedWebRequestMethod.GET:
                        responseBody = await MakeGETRequest(URL);
                        break;
                    case SupportedWebRequestMethod.POST:
                        responseBody = await MakePOSTRequest(URL, Query);
                        break;
                    default:
                        throw new ArgumentException($"Invalid method type: {method}");
                }
                return responseBody;
            }
            catch (Exception e)
            {
                return $"Result,Message\nError,\"{e.Message}\"";
            }

            static async Task<string> MakeGETRequest(string url)
            {
                using HttpClient client = new();
                return await client.GetStringAsync(url);
            }
            static async Task<string> MakePOSTRequest(string url, string payload)
            {
                using HttpClient client = new();
                using HttpResponseMessage response = await client.PostAsync(url, new StringContent(payload));
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(URL);
            writer.Write(Method);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            URL = reader.ReadString();
            Method = reader.ReadString();
        }
        #endregion

        #region Script Exporting Interface
        public override void BuildMarkdown(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(URL))
                builder.AppendLine($"URL: {URL}  ");
            if (!string.IsNullOrEmpty(Method))
                builder.AppendLine($"Method: {Method}\n");
            base.BuildMarkdown(builder);
        }
        #endregion
    }
}
