using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expresso.Services
{
    public static class ConfigurationHelper
    {
        #region Getters
        public static string GetConfiguration(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }
        #endregion
    }
}
