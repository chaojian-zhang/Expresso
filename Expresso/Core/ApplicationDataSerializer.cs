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

                writer.Write(dataReader.Transforms.Count);
                foreach (ApplicationDataTransform transform in dataReader.Transforms)
                {

                }
            }

            writer.Write(data.OutputWriters.Count);
            foreach (ApplicationOutputWriter outputWriter in data.OutputWriters)
            {
                writer.Write(outputWriter.ServiceProvider);
                writer.Write(outputWriter.DataSourceString);
                writer.Write(outputWriter.Query);
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

            var dataReadersCount = reader.ReadInt32();
            for (int i = 0; i < dataReadersCount; i++)
            {
                ApplicationDataReader source = new();

                var queriesCount = reader.ReadInt32();
                for (int j = 0; j < queriesCount; j++)
                {
                    source.DataQueries.Add(new ApplicationDataQuery()
                    {
                        ServiceProvider = reader.ReadString(),
                        DataSourceString = reader.ReadString(),
                        Query = reader.ReadString()
                    });
                }

                var transformsCount = reader.ReadInt32();

                applicationData.DataReaders.Add(source);
            }

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

            return applicationData;
        }
        #endregion
    }
}
