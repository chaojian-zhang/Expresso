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
        public static readonly RoutedUICommand Statistics = 
            new(
                "Show PDF statistics", 
                "Statistics", 
                typeof(ExpressoCommands), 
                new InputGestureCollection { 
                    new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift, "Ctrl+Shift+S") 
                });
        #endregion
    }
}
