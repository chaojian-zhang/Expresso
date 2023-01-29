﻿using System.Windows;
using System.Windows.Input;

namespace Expresso.PopUps
{
    public partial class ComboChoiceDialog : Window
    {
        #region Construction
        public ComboChoiceDialog(string title, string text, string defaultValue, string[] options, string furtherExplanation = null)
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(ComboChoiceDialogLoaded);

            DisplayText.Text = text;
            Title = title;

            foreach (var option in options)
                OptionComboBox.Items.Add(option);
            OptionComboBox.SelectedValue = defaultValue;

            if (!string.IsNullOrEmpty(furtherExplanation))
            {
                ExplanationText.Text = furtherExplanation;
                ExplanationText.Visibility = Visibility.Visible;
            }
        }
        #endregion

        #region Properties
        public string ResponseText => OptionComboBox.SelectedValue as string;
        #endregion

        #region Events
        void ComboChoiceDialogLoaded(object sender, RoutedEventArgs e)
        {
            OptionComboBox.Focus();
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelBUtton_Click(object sender, RoutedEventArgs e)
            => Close();
        #endregion

        #region Interface Method
        public static string Prompt(string title, string text, string defaultValue, string[] options, string furtherExplanation = null)
        {
            ComboChoiceDialog inst = new(title, text, defaultValue, options, furtherExplanation);
            inst.ShowDialog();
            if (inst.DialogResult == true)
                return inst.ResponseText;
            return null;
        }
        #endregion     
    }
}
