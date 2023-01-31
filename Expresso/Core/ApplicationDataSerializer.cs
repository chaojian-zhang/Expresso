using K4os.Compression.LZ4.Streams;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Expresso.Core
{
    public static class ApplicationDataSerializer
    {
        #region Methods
        public static void Save(string filepath, ApplicationData data, bool compressed = true)
        {
            if (compressed)
            {
                using LZ4EncoderStream stream = LZ4Stream.Encode(File.Create(filepath));
                using BinaryWriter writer = new(stream, Encoding.UTF8, false);
                WriteToStream(writer, data);
            }
            else
            {
                using FileStream stream = File.Open(filepath, FileMode.Create);
                using BinaryWriter writer = new(stream, Encoding.UTF8, false);
                WriteToStream(writer, data);
            }
        }
        public static ApplicationData Load(string filepath, bool compressed = true)
        {
            if (compressed)
            {
                using LZ4DecoderStream source = LZ4Stream.Decode(File.OpenRead(filepath));
                using BinaryReader reader = new(source, Encoding.UTF8, false);
                return ReadFromStream(reader);
            }
            else
            {
                using FileStream stream = File.Open(filepath, FileMode.Open);
                using BinaryReader reader = new(stream, Encoding.UTF8, false);
                return ReadFromStream(reader);
            }
        }
        #endregion

        #region Routines
        private static void WriteToStream(BinaryWriter writer, ApplicationData data)
        {
            writer.Write(data.FileVersion);
            writer.Write(data.Name);
            writer.Write(data.Description);
            writer.Write(++data.Iteration);
            writer.Write(data.CreationTime.ToString("yyyy-MM-dd"));
            writer.Write(DateTime.Now.ToString("yyyy-MM-dd"));

            writer.Write(data.Conditionals.Count);
            foreach (var condition in data.Conditionals)
            {
                writer.Write(condition.Name);
                writer.Write((byte)condition.Type);
                writer.Write(condition.Description);
                writer.Write(condition.ReaderName);
            }

            writer.Write(data.DataReaders.Count);
            foreach (ApplicationDataReader dataReader in data.DataReaders)
            {
                writer.Write(dataReader.Name); 
                writer.Write(dataReader.Description);
                writer.Write(dataReader.DataQueries.Count);
                foreach (ApplicationDataQuery dataQuery in dataReader.DataQueries)
                {
                    writer.Write(dataQuery.Name);
                    writer.Write(dataQuery.ServiceProvider);

                    dataQuery.Parameters.WriteToStream(writer);
                }
                writer.Write(dataReader.Transform);
            }

            writer.Write(data.OutputWriters.Count);
            foreach (ApplicationOutputWriter outputWriter in data.OutputWriters)
            {
                writer.Write(outputWriter.Name);
                writer.Write(outputWriter.ServiceProvider);
                outputWriter.Parameters.WriteToStream(writer);
            }

            writer.Write(data.Variables.Count);
            foreach (ApplicationVariable variable in data.Variables)
            {
                writer.Write(variable.Name);
                writer.Write((byte)variable.Type);
                writer.Write(variable.Value);
                writer.Write(variable.IsIterator);
            }

            writer.Write(data.Processors.Count);
            foreach (ApplicationProcessor processor in data.Processors)
            {
                writer.Write(processor.Name);
                writer.Write(processor.Description);
                writer.Write(processor.StartingSteps.Count);
                foreach (ApplicationProcessorStep step in processor.StartingSteps)
                    WriteProcessorStep(writer, step);
            }

            writer.Write(data.Workflows.Count);
            foreach (ApplicationWorkflow workflow in data.Workflows)
            {
                writer.Write(workflow.Name);
                writer.Write(workflow.Description);
                writer.Write(workflow.StartingSteps.Count);
                foreach (ApplicationWorkflowStep step in workflow.StartingSteps)
                    WriteWorkflowStep(writer, step);
            }

            void WriteProcessorStep(BinaryWriter writer, ApplicationProcessorStep step)
            {
                writer.Write(step.Name);
                writer.Write(step.Inputs.Count);
                foreach (var input in step.Inputs)
                {
                    writer.Write(input.FromName);
                    writer.Write(input.AsName);
                }
                writer.Write(step.Action);
                writer.Write(step.Outputs.Count);
                foreach (var output in step.Outputs)
                {
                    writer.Write(output.FromName);
                    writer.Write(output.AsName);
                }
                writer.Write(step.IsFinalOutput);
                if (!step.IsFinalOutput)
                {
                    writer.Write(step.NextSteps.Count);
                    foreach (var next in step.NextSteps)
                        WriteProcessorStep(writer, next);
                }
            }
            void WriteWorkflowStep(BinaryWriter writer, ApplicationWorkflowStep step)
            {
                writer.Write(step.Name);
                writer.Write(step.ActionType);
                writer.Write(step.ActionItem);
                writer.Write(step.NextSteps.Count);
                foreach (var next in step.NextSteps)
                    WriteWorkflowStep(writer, next);
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
                        Name = reader.ReadString(),
                        Type = (ConditionType)reader.ReadByte(),
                        Description = reader.ReadString(),
                        ReaderName = reader.ReadString(),
                    };

                    applicationData.Conditionals.Add(conditional);
                }
            }

            {
                var dataReadersCount = reader.ReadInt32();
                for (int i = 0; i < dataReadersCount; i++)
                {
                    ApplicationDataReader dataReader = new()
                    {
                        Name = reader.ReadString(),
                        Description = reader.ReadString(),
                    };

                    var queriesCount = reader.ReadInt32();
                    for (int j = 0; j < queriesCount; j++)
                    {
                        var query = new ApplicationDataQuery()
                        {
                            Name = reader.ReadString(),
                            ServiceProvider = reader.ReadString()
                        };

                        var type = ReaderDataQueryParameterBase.GetServiceProviders()[query.ServiceProvider];
                        var paramters = (ReaderDataQueryParameterBase)Activator.CreateInstance(type);
                        paramters.ReadFromStream(reader);

                        query.Parameters = paramters;
                        dataReader.DataQueries.Add(query);
                    }
                    dataReader.Transform = reader.ReadString();

                    applicationData.DataReaders.Add(dataReader);
                }
            }

            {
                var outputWritersCount = reader.ReadInt32();
                for (int i = 0; i < outputWritersCount; i++)
                {
                    ApplicationOutputWriter outputWriter = new ApplicationOutputWriter()
                    {
                        Name = reader.ReadString(),
                        ServiceProvider = reader.ReadString()
                    };

                    var type = WriterParameterBase.GetServiceProviders()[outputWriter.ServiceProvider];
                    var paramters = (WriterParameterBase)Activator.CreateInstance(type);
                    paramters.ReadFromStream(reader);

                    outputWriter.Parameters = paramters;
                    applicationData.OutputWriters.Add(outputWriter);
                }
            }

            {
                var variableCount = reader.ReadInt32();
                for (int i = 0; i < variableCount; i++)
                {
                    ApplicationVariable variable = new()
                    {
                        Name = reader.ReadString(),
                        Type = (ApplicationVariable.VariableType)reader.ReadByte(),
                        Value = reader.ReadString(),
                        IsIterator = reader.ReadBoolean(),
                    };

                    applicationData.Variables.Add(variable);
                }
            }

            {
                var processorsCount = reader.ReadInt32();
                for (int i = 0; i < processorsCount; i++)
                {
                    ApplicationProcessor processor = new()
                    {
                        Name = reader.ReadString(),
                        Description = reader.ReadString(),
                    };

                    int processorSteps = reader.ReadInt32();
                    for (int j = 0; j < processorSteps; j++)
                        processor.StartingSteps.Add(ReadProcessorStep(reader));

                    applicationData.Processors.Add(processor);
                }
            }

            {
                var workflowCount = reader.ReadInt32();
                for (int i = 0; i < workflowCount; i++)
                {
                    ApplicationWorkflow workflow = new()
                    {
                        Name = reader.ReadString(),
                        Description = reader.ReadString()
                    };

                    int workflowSteps = reader.ReadInt32();
                    for (int j = 0; j < workflowSteps; j++)
                        workflow.StartingSteps.Add(ReadWorkflowStep(reader));

                    applicationData.Workflows.Add(workflow);
                }
            }

            return applicationData;

            ApplicationProcessorStep ReadProcessorStep(BinaryReader reader)
            {
                ApplicationProcessorStep step = new ApplicationProcessorStep();

                {
                    step.Name = reader.ReadString();
                }
                {
                    int inputsCount = reader.ReadInt32();
                    for (int i = 0; i < inputsCount; i++)
                        step.Inputs.Add(new ApplicationProcessorStep.ParameterMapping()
                        {
                            FromName = reader.ReadString(),
                            AsName = reader.ReadString(),
                        });
                }
                {
                    step.Action = reader.ReadString();
                }
                {
                    int outputsCount = reader.ReadInt32();
                    for (int i = 0; i < outputsCount; i++)
                    {
                        step.Outputs.Add(new ApplicationProcessorStep.ParameterMapping()
                        {
                            FromName = reader.ReadString(),
                            AsName = reader.ReadString(),
                        });
                    }
                }
                {
                    step.IsFinalOutput = reader.ReadBoolean();
                }
                {
                    if (!step.IsFinalOutput)
                    {
                        int subStepsCount = reader.ReadInt32();
                        for (int i = 0; i < subStepsCount; i++)
                            step.NextSteps.Add(ReadProcessorStep(reader));
                    }
                }
                return step;
            }
            ApplicationWorkflowStep ReadWorkflowStep(BinaryReader reader)
            {
                ApplicationWorkflowStep step = new();

                {
                    step.Name = reader.ReadString();
                    step.ActionType = reader.ReadString();
                    step.ActionItem = reader.ReadString();
                }
                {
                    int subStepsCount = reader.ReadInt32();
                    for (int i = 0; i < subStepsCount; i++)
                        step.NextSteps.Add(ReadWorkflowStep(reader));
                }
                return step;
            }
        }
        #endregion
    }
}
