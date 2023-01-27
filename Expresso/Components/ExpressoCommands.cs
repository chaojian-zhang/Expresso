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
        #region UI Commands
        public static readonly RoutedUICommand OpenSettingsCommand = 
            new(
                "Open application settings", 
                "Settings", 
                typeof(ExpressoCommands), 
                new InputGestureCollection { 
                    new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+O") 
                });
        #endregion
    }
}
