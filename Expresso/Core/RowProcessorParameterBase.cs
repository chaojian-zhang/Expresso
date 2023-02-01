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
using System.Text.RegularExpressions;

namespace Expresso.Core
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
                    .ToDictionary(t => t.GetProperty(nameof(DisplayName)).GetValue(null) as string, t => t);

            return _ServiceProviders;
        }
        #endregion

        #region Static Metadata Properties
        public static string DisplayName => "No Action";
        #endregion

        #region Handling Procedure
        public abstract Dictionary<string, string> HandleInputs(Dictionary<string, string> inputs);
        #endregion

        #region Serialization Interface
        public virtual void WriteToStream(BinaryWriter writer)
        {
        }
        public virtual void ReadFromStream(BinaryReader reader)
        {
        }
        #endregion

        #region Documentation Interface
        public static string[] Inputs => Array.Empty<string>();
        public static string[] Outputs => Array.Empty<string>();
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

        #region Accessor
        public string PatternInterpolated => Pattern.InterpolateVariables();
        public string ReplacementInterpolated => Replacement.InterpolateVariables();
        #endregion

        #region Handling Procedure
        public override Dictionary<string, string> HandleInputs(Dictionary<string, string> inputs)
        {
            return inputs.ToDictionary(i => i.Key, i => Regex.Replace(i.Value, PatternInterpolated, ReplacementInterpolated));
        }
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

        #region Documentation Interface
        public static new string[] Inputs => new string[] { "Any" };
        public static new string[] Outputs => new string[] { "Any" };
        #endregion
    }

    public sealed class PassThroughRowProcessorParameterBase : RowProcessorParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Pass Through";
        #endregion

        #region Handling Procedure
        public override Dictionary<string, string> HandleInputs(Dictionary<string, string> inputs)
            => inputs;
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

        #region Documentation Interface
        public static new string[] Inputs => new string[] { "Any" };
        public static new string[] Outputs => new string[] { "Any" };
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

        #region Handling Procedure
        public override Dictionary<string, string> HandleInputs(Dictionary<string, string> inputs)
        {
            throw new NotFiniteNumberException();
        }
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

        #region Handling Procedure
        public override Dictionary<string, string> HandleInputs(Dictionary<string, string> inputs)
        {
            throw new NotFiniteNumberException();
        }
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

        #region Handling Procedure
        public override Dictionary<string, string> HandleInputs(Dictionary<string, string> inputs)
        {
            throw new NotFiniteNumberException();
        }
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

    public sealed class ExpressionEvaluatorRowProcessorParameterBase : RowProcessorParameterBase
    {
        #region Meta Data
        public static new string DisplayName => "Expression Evaluation";
        #endregion

        #region Base Property
        private string _Expression = string.Empty;
        #endregion

        #region Data Binding Setup
        public string Expression { get => _Expression; set => SetField(ref _Expression, value); }
        #endregion

        #region Accessor
        public string ExpressionInterpolated => Expression.InterpolateVariables();
        #endregion

        #region Handling Procedure
        public override Dictionary<string, string> HandleInputs(Dictionary<string, string> inputs)
        {
            if (string.IsNullOrWhiteSpace(Expression)) return PackResult("Missing expression.");

            Dictionary<string, double> parameters = inputs.ToDictionary(
                i => i.Key,
                i =>
                {
                    if (double.TryParse(i.Value, out double number))
                        return number;
                    else return 0;
                });

            try
            {
                var calc = new Sprache.Calc.XtensibleCalculator();
                var expr = calc.ParseExpression(ExpressionInterpolated, parameters);
                var func = expr.Compile();
                var finalResult = func().ToString();
                return PackResult(finalResult);
            }
            catch (Exception e)
            {
                return PackResult(e.Message);
            }

            static Dictionary<string, string> PackResult(string result)
            {
                return new Dictionary<string, string>()
                {
                    { "Result", result }
                };
            }
        }
        #endregion

        #region Serialization Interface
        public override void WriteToStream(BinaryWriter writer)
        {
            base.WriteToStream(writer);
            writer.Write(Expression);
        }
        public override void ReadFromStream(BinaryReader reader)
        {
            base.ReadFromStream(reader);
            Expression = reader.ReadString();
        }
        #endregion

        #region Documentation Interface
        public static new string[] Inputs => new string[] { "Any" };
        public static new string[] Outputs => new string[] { "Result" };
        #endregion
    }
}
