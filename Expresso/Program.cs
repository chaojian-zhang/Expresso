using Expresso.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Expresso
{
    public class ConsoleColorContext : IDisposable
    {
        readonly ConsoleColor OriginalColor;
        public ConsoleColorContext(ConsoleColor color)
        {
            OriginalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }
        public void Dispose()
        {
            Console.ForegroundColor = OriginalColor;
        }
    }

    public static class Program
    {
        #region Signature Engravings
        public static readonly string ProgramVersion = "V1";
        public static readonly string BuildID = "V2023_01_30-78D8A6103C95";
        public static readonly string Nickname = "SQL Analyst";
        public static readonly string Catchphrase = "Data analysis in a breeze.";
        #endregion

        #region Main
        [STAThread]
        public static void Main(string[] args)
        {
            // Remark-cs: per https://github.com/ExcelDataReader/ExcelDataReader, this is to deal with Excel writer encoding issue
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            if (args.Length == 0)
                GUIMode();
            else HeadlessMode(args);
        }
        #endregion

        #region Entrances
        private static void GUIMode()
        {
            // Show welcome since we are displaying the console anyway
            using (new ConsoleColorContext(ConsoleColor.Green))
            {
                Console.WriteLine("""
                ___________                                                 
                \_   _____/__  ________________   ____   ______ __________  
                 |    __)_\  \/  /\____ \_  __ \_/ __ \ /  ___//  ___/  _ \ 
                 |        \>    < |  |_> >  | \/\  ___/ \___ \ \___ (  <_> )
                /_______  /__/\_ \|   __/|__|    \___  >____  >____  >____/ 
                        \/      \/|__|               \/     \/     \/       
                """ + Environment.NewLine +
                $"""
                Expresso {ProgramVersion,-20}   {"by Charles Zhang, 2023",40}
                {Nickname} - {Catchphrase}
                Build ID {BuildID}
                """);
            }
            // Remark-cz: Since we are showing welcome, don't let it just flash away; Give people some time to read
            // But don't make it too long to affect their work
            System.Threading.Thread.Sleep(1200);

            ExpressoApplicationContext.Initialize();
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        private static void HeadlessMode(string[] args)
        {
            if (args.Length == 1 && args.Single().ToLower() == "--help")
                using (new ConsoleColorContext(ConsoleColor.DarkGray))
                {
                    Console.WriteLine("""
                    Command Line Format; 
                        Expresso --help|(<File Path> --workflow=<Workflow Name> [List of Variables])

                    Parameters:
                        --help: Print help message.
                        --workflow <Workflow Name>: Select workflow to execute.
                        [List of Variables]: --<variableName>=<variable value>

                    Example: 
                        Expresso MyFile.eso --workflow "Main Workflow" --iterations=5
                    """);
                }
            else
            {
                string filepath = args.First();
                (string Key, string Value)[] parameters = args.Skip(1)
                    .Select(v =>
                    {
                        var match = Regex.Match(v, "--(.*?)=(.*)");
                        return (match.Groups[1].Value, match.Groups[2].Value);
                    }).ToArray();

                if (filepath.StartsWith("-"))
                {
                    Console.WriteLine("Require file path.");
                    return;
                }
                if (!File.Exists(filepath))
                {
                    Console.WriteLine($"File {filepath} doesn't exist.");
                    return;
                }

                InitializeHeadlessMode(filepath, parameters);
            }
        }
        #endregion

        #region Routines
        private static void InitializeHeadlessMode(string filePath, (string Key, string Value)[] parameters)
        {
            ExpressoApplicationContext.Initialize();

            var appData = ApplicationDataSerializer.Load(filePath);
            if (appData.Workflows.Count == 0)
            {
                Console.WriteLine("There is no workflow defined in the document.");
                return;
            }

            string workflowName = null;
            if (parameters.Any(p => p.Key == "workflow"))
            {
                workflowName = parameters[0].Value;
                if (!appData.Workflows.Any(w => w.Name == workflowName))
                {
                    Console.WriteLine($"Cannot find specified workflow in document: {workflowName}");
                    return;
                }
            }
            else
            {
                if (appData.Workflows.Count != 1)
                {
                    Console.WriteLine("Specify workflow for execution.");
                    return;
                }
            }
            if (workflowName == null)
                throw new ApplicationException("Workflow cannot be null.");

            ExpressoApplicationContext.Context.SetCurrentApplicationData(appData);
            var workflow = appData.Workflows.FirstOrDefault(w => w.Name == workflowName);
            workflow.ExecuteWorkflow();
        }
        #endregion
    }

    internal sealed class Win32
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        public static void HideConsoleWindow()
        {
            IntPtr hConsole = GetConsoleWindow();

            if (hConsole != IntPtr.Zero)
                ShowWindow(hConsole, 0); // 0 = SW_HIDE
        }
    }
}
