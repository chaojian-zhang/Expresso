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

            writer.Write(data.DataSources.Count);
            foreach (ApplicationDataSource dataSource in data.DataSources)
            {
                writer.Write(dataSource.DataQueries.Count);
                foreach (ApplicationDataQuery dataQuery in dataSource.DataQueries)
                {
                    writer.Write(dataQuery.ServiceProvider);
                    writer.Write(dataQuery.DataSourceString);
                    writer.Write(dataQuery.Query);
                }

                writer.Write(dataSource.Transforms.Count);
                foreach (ApplicationDataTransform transform in dataSource.Transforms)
                {

                }
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

            var dataSourcesCount = reader.ReadInt32();
            for (int i = 0; i < dataSourcesCount; i++)
            {
                ApplicationDataSource source = new();

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

                applicationData.DataSources.Add(source);
            }

            return applicationData;
        }
        #endregion
    }
}
