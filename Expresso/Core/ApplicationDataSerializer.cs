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

        }
        private static ApplicationData ReadFromStream(BinaryReader reader)
        {
            return new ApplicationData();
        }
        #endregion
    }
}
