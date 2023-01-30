using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expresso
{
    public class Program
    {
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
