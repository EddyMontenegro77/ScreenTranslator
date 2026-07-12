using System.Windows;

namespace ScreenTranslator.Views
{
    public partial class StatusIndicatorWindow
    {
        public StatusIndicatorWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => PositionBottomRight();
        }

        private void PositionBottomRight()
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 30;
            Top = workArea.Bottom - Height - 30;
        }

        public string Status
        {
            get => TxtStatus.Text;
            set => TxtStatus.Text = value;
        }

        public double ProgressValue
        {
            get => ProgressTranslate.Value;
            set
            {
                ProgressTranslate.IsIndeterminate = false;
                ProgressTranslate.Value = value;
            }
        }

        public double ProgressMaximum
        {
            get => ProgressTranslate.Maximum;
            set => ProgressTranslate.Maximum = value;
        }

        public void ProgressIndeterminate()
        {
            ProgressTranslate.IsIndeterminate = true;
            ProgressTranslate.Value = 0;
        }

        public void ShowProgress()
        {
            ProgressTranslate.Visibility = Visibility.Visible;
        }

        public void HideProgress()
        {
            ProgressTranslate.Visibility = Visibility.Collapsed;
        }

    }
}
