using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expresso.Core
{
    public static class ApplicationDataSerializer
    {
        #region Methods
        public static void Save(string filepath, ApplicationData data)
        {
            using FileStream stream = File.Open(filepath, FileMode.Create);
            using BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, false);
            WriteToStream(writer, data);
        }
        public static ApplicationData Load(string filepath)
        {
            using FileStream stream = File.Open(filepath, FileMode.Open);
            using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, false);
            return ReadFromStream(reader);
        }
        #endregion

        #region Routines
        private static void WriteToStream(BinaryWriter writer, ApplicationData data)
        {
            writer.Write(data.Name);
            writer.Write(data.Description);
            writer.Write(++data.Iteration);
            writer.Write(data.CreationTime.ToString("yyyy-MM-dd"));
            writer.Write(DateTime.Now.ToString("yyyy-MM-dd"));

            writer.Write(data.Conditionals.Count);
            foreach (var condition in data.Conditionals)
            {
                writer.Write((byte)condition.Type);
                writer.Write(condition.Specification);
                writer.Write(condition.Parameter);
            }

            writer.Write(data.DataReaders.Count);
            foreach (ApplicationDataReader dataReader in data.DataReaders)
            {
                writer.Write(dataReader.DataQueries.Count);
                foreach (ApplicationDataQuery dataQuery in dataReader.DataQueries)
                {
                    writer.Write(dataQuery.ServiceProvider);
                    writer.Write(dataQuery.DataSourceString);
                    writer.Write(dataQuery.Query);
                }
                writer.Write(dataReader.Transform);
            }

            writer.Write(data.OutputWriters.Count);
            foreach (ApplicationOutputWriter outputWriter in data.OutputWriters)
            {
                writer.Write(outputWriter.ServiceProvider);
                writer.Write(outputWriter.DataSourceString);
                writer.Write(outputWriter.Query);
            }

            writer.Write(data.Variables.Count);
            foreach (ApplicationVariable variable in data.Variables)
            {
                writer.Write((byte)variable.Type);
                writer.Write(variable.Value);
            }
        }
        private static ApplicationData ReadFromStream(BinaryReader reader)
        {
            ApplicationData applicationData = new()
            {
                Name = reader.ReadString(),
                Description = reader.ReadString(),
                Iteration = reader.ReadInt64(),
                CreationTime = DateTime.Parse(reader.ReadString()),
                LastModifiedTime = DateTime.Parse(reader.ReadString())
            };

            {
                var conditionalCount = reader.ReadInt32();
                for (int i = 0; i < conditionalCount; i++)
                {
                    ApplicationExecutionConditional conditional = new()
                    {
                        Type = (ApplicationExecutionConditional.ConditionType)reader.ReadByte(),
                        Specification = reader.ReadString(),
                        Parameter = reader.ReadString(),
                    };

                    applicationData.Conditionals.Add(conditional);
                }
            }

            {
                var dataReadersCount = reader.ReadInt32();
                for (int i = 0; i < dataReadersCount; i++)
                {
                    ApplicationDataReader dataReader = new();

                    var queriesCount = reader.ReadInt32();
                    for (int j = 0; j < queriesCount; j++)
                    {
                        dataReader.DataQueries.Add(new ApplicationDataQuery()
                        {
                            ServiceProvider = reader.ReadString(),
                            DataSourceString = reader.ReadString(),
                            Query = reader.ReadString()
                        });
                    }
                    dataReader.Transform = reader.ReadString();

                    applicationData.DataReaders.Add(dataReader);
                }
            }

            {
                var outputWritersCount = reader.ReadInt32();
                for (int i = 0; i < outputWritersCount; i++)
                {
                    ApplicationOutputWriter writer = new ApplicationOutputWriter()
                    {
                        ServiceProvider = reader.ReadString(),
                        DataSourceString = reader.ReadString(),
                        Query = reader.ReadString()
                    };
                    applicationData.OutputWriters.Add(writer);
                }
            }

            {
                var variableCount = reader.ReadInt32();
                for (int i = 0; i < variableCount; i++)
                {
                    ApplicationVariable variable = new()
                    {
                        Type = (ApplicationVariable.VariableType)reader.ReadByte(),
                        Value= reader.ReadString()
                    };

                    applicationData.Variables.Add(variable);
                }
            }

            return applicationData;
        }
        #endregion
    }
}
