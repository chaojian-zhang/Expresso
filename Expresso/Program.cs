using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expresso
{
    public static class Program
    {
        public static readonly string ProgramVersion = "V1";
        public static readonly string BuildID = "V2023_01_30-78D8A6103C95";

        [STAThread]
        public static void Main()
        {
            // Remark-cs: per https://github.com/ExcelDataReader/ExcelDataReader, this is to deal with encoding issue
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
