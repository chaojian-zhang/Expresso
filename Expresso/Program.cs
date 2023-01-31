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
        public static readonly string BuildID = "0A0F4200-B0ED-4900-B355-78D8A6103C95";

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
