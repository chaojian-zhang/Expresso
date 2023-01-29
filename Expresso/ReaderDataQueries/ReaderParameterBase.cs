﻿using Expresso.Components;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Csv;

namespace Expresso.ReaderDataQueries
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
                    .ToDictionary(t => t.GetProperty(nameof(ReaderDataQueryParameterBase.DisplayName)).GetValue(null) as string, t => t);

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
                var oracleConnection = new OdbcConnection($"DSN={DSN}");
                oracleConnection.Open();
                var dataTable = new DataTable();
                dataTable.Load(new OdbcCommand(Query, oracleConnection).ExecuteReader());
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
            throw new NotImplementedException();
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
}