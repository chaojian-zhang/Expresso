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
    partial class OutputWriterParameterResourceDictionary : ResourceDictionary
    {
        #region Construction
        public OutputWriterParameterResourceDictionary()
        {
            InitializeComponent();
        }
        #endregion

        #region Event Handlers
        private void ODBCWriterAvalonTextEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ODBCWriterParameter parameters = editor.DataContext as ODBCWriterParameter;
            if (parameters != null)
                editor.Text = parameters.Transform;
        }
        private void ODBCWriterAvalonTextEditor_Initialized(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ODBCWriterParameter parameters = editor.DataContext as ODBCWriterParameter;
            editor.Text = parameters.Transform;
        }
        private void ODBCWriterAvalonTextEditor_OnTextChanged(object sender, EventArgs e)
        {
            TextEditor editor = sender as TextEditor;
            ODBCWriterParameter parameters = editor.DataContext as ODBCWriterParameter;
            parameters.Transform = editor.Text;
        }
        #endregion
    }
}
