using Expresso.Components;
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
    public abstract class RowProcessorParameterBase : BaseNotifyPropertyChanged
    {
        #region Reflection Service
        private static Dictionary<string, Type> _ServiceProviders;
        public static Dictionary<string, Type> GetServiceProviders()
        {
            if (_ServiceProviders == null)
                _ServiceProviders = Assembly
                    .GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t != typeof(RowProcessorParameterBase) && typeof(RowProcessorParameterBase).IsAssignableFrom(t))
                    .ToDictionary(t => t.GetProperty(nameof(RowProcessorParameterBase.DisplayName)).GetValue(null) as string, t => t);

            return _ServiceProviders;
        }
        #endregion

        #region Static Metadata Properties
        public static string DisplayName => "No Action";
        #endregion

        #region Serialization Interface
        public virtual void WriteToStream(BinaryWriter writer)
        {
        }
        public virtual void ReadFromStream(BinaryReader reader)
        {
        }
        #endregion
    }

    public sealed class RegularExpressionRowProcessorParameterBase : RowProcessorParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Regular Expression Replacement";
        #endregion

        #region Base Property
        private string _Pattern = string.Empty;
        private string _Replacement = string.Empty;
        #endregion

        #region Data Binding Setup
        public string Pattern { get => _Pattern; set => SetField(ref _Pattern, value); }
        public string Replacement { get => _Replacement; set => SetField(ref _Replacement, value); }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(Pattern);
            writer.Write(Replacement);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            Pattern = reader.ReadString();
            Replacement = reader.ReadString();
        }
        #endregion
    }

    public sealed class PassThroughRowProcessorParameterBase : RowProcessorParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Pass Through";
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
        }
        #endregion
    }

    public sealed class RunProgramRowProcessorParameterBase : RowProcessorParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Run Program";
        #endregion

        #region Base Property
        private string _ProgramPath = string.Empty;
        private string _ArgumentFormat = string.Empty;
        #endregion

        #region Data Binding Setup
        public string ProgramPath { get => _ProgramPath; set => SetField(ref _ProgramPath, value); }
        public string ArgumentFormat { get => _ArgumentFormat; set => SetField(ref _ArgumentFormat, value); }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(ProgramPath);
            writer.Write(ArgumentFormat);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            ProgramPath = reader.ReadString();
            ArgumentFormat = reader.ReadString();
        }
        #endregion
    }

    public sealed class WebRequestRowProcessorParameterBase : RowProcessorParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Web Request";
        #endregion

        #region Base Property
        private string _URL = string.Empty;
        private string _Method = string.Empty;
        private string _OptionalBody = string.Empty;
        #endregion

        #region Data Binding Setup
        public string URL { get => _URL; set => SetField(ref _URL, value); }
        public string Method { get => _Method; set => SetField(ref _Method, value); }
        public string OptionalBody { get => _OptionalBody; set => SetField(ref _OptionalBody, value); }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(URL);
            writer.Write(Method);
            writer.Write(OptionalBody);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            URL = reader.ReadString();
            Method = reader.ReadString();
            OptionalBody = reader.ReadString();
        }
        #endregion
    }

    public sealed class ReadFileContentRowProcessorParameterBase : RowProcessorParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Read File Content";
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
        }
        #endregion
    }
}
