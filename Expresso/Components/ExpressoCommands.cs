using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Expresso.Components
{
    public static class ExpressoCommands
    {
        #region Menu Commands
        public static readonly RoutedUICommand CreateVariableCommand = 
            new(
                "Create Variable", 
                "Variable", 
                typeof(ExpressoCommands), 
                new InputGestureCollection { 
                    new KeyGesture(Key.D1, ModifierKeys.Control, "Ctrl+1") 
                });
        public static readonly RoutedUICommand CreateConditionCommand =
            new(
                "Create Condition",
                "Condition",
                typeof(ExpressoCommands),
                new InputGestureCollection {
                    new KeyGesture(Key.D2, ModifierKeys.Control, "Ctrl+2")
                });
        public static readonly RoutedUICommand CreateReaderCommand =
            new(
                "Create Reader",
                "Reader",
                typeof(ExpressoCommands),
                new InputGestureCollection {
                    new KeyGesture(Key.D3, ModifierKeys.Control, "Ctrl+3")
                });
        public static readonly RoutedUICommand CreateWriterCommand =
            new(
                "Create Writer",
                "Writer",
                typeof(ExpressoCommands),
                new InputGestureCollection {
                    new KeyGesture(Key.D4, ModifierKeys.Control, "Ctrl+4")
                });
        public static readonly RoutedUICommand CreateRowProcessorCommand =
            new(
                "Create Row Processor",
                "Processor",
                typeof(ExpressoCommands),
                new InputGestureCollection {
                    new KeyGesture(Key.D5, ModifierKeys.Control, "Ctrl+5")
                });
        public static readonly RoutedUICommand CreateWorkflowCommand =
            new(
                "Create Workflow",
                "Workflow",
                typeof(ExpressoCommands),
                new InputGestureCollection {
                    new KeyGesture(Key.D6, ModifierKeys.Control, "Ctrl+6")
                });

        public static readonly RoutedUICommand RunEngineCommand =
            new(
                "Run Engine",
                "Engine",
                typeof(ExpressoCommands),
                new InputGestureCollection {
                    new KeyGesture(Key.E, ModifierKeys.Control, "Ctrl+E")
                });
        #endregion
    }
}
