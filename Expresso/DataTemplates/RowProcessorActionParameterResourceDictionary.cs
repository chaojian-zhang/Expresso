using Expresso.Core;
using Expresso.PopUps;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Expresso.DataTemplates
{
    partial class RowProcessorActionParameterResourceDictionary : ResourceDictionary
    {
        #region Construction
        public RowProcessorActionParameterResourceDictionary()
        {
            InitializeComponent();
        }
        #endregion

        #region Event Handlers - ODBCCommandParameter
        private void ODBCCommandAvalonTextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterODBCCommandParameter parameters = editor.DataContext as OutputWriterODBCCommandParameter;
            if (parameters != null)
                editor.Text = parameters.Query;
        }
        private void ODBCCommandAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterODBCCommandParameter parameters = editor.DataContext as OutputWriterODBCCommandParameter;
            editor.Text = parameters.Query;
        }
        private void ODBCCommandAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            OutputWriterODBCCommandParameter parameters = editor.DataContext as OutputWriterODBCCommandParameter;
            parameters.Query = editor.Text;
        }
        #endregion
    }
}
