using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Teleprompter.Controls
{
    /// <summary>
    /// Interaction logic for QuickFilterControl.xaml
    /// </summary>
    public partial class QuickFilterControl : UserControl
    {
        public delegate void QuickFilterValueChanged(object sender, string filter);

        public event QuickFilterValueChanged FilterValueChanged;

        private string defaultText;
        private Brush defaultTextBrush;

        public QuickFilterControl()
        {
            this.InitializeComponent();
            this.defaultText = InputFilterText.Text;
            this.defaultTextBrush = InputFilterText.Foreground;
        }

        public string FilterText
        {
            get
            {
                return this.InputFilterText.Text;
            }
            set
            {
                this.InputFilterText.Text = value;
            }
        }

        private void OnTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            TextBox tv = sender as TextBox;
            if (tv != null)
            {
                FiterEventTextChanged(tv.Text);
            }
        }

        private void FiterEventTextChanged(string filter)
        {
            if (FilterValueChanged != null)
            {
                UiDispatcher.RunOnUIThread(() =>
                {
                    this.FilterValueChanged(this, filter);
                });
            }
        }

        private void OnInputFilterText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ClearFilter != null)
            {
                TextBox tb = sender as TextBox;
                if (tb != null && string.IsNullOrWhiteSpace(tb.Text) == false)
                {
                    ClearFilter.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    ClearFilter.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }


        private void OnClearFilterButton_Closed(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            this.InputFilterText.Text = string.Empty;
            FiterEventTextChanged(this.InputFilterText.Text);
        }

        internal void FocusTextBox()
        {
            InputFilterText.Focus();
            InputFilterText.SelectAll();
        }

        private void OnInputFilterGotFocus(object sender, RoutedEventArgs e)
        {
            if (this.defaultText == InputFilterText.Text)
            {
                InputFilterText.Text = "";
                InputFilterText.SetValue(TextBox.ForegroundProperty, DependencyProperty.UnsetValue);
            }
        }

        private void OnInputFilterLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(InputFilterText.Text))
            {
                InputFilterText.Text = this.defaultText;
                InputFilterText.Foreground = this.defaultTextBrush;
            }
        }
    }

}
