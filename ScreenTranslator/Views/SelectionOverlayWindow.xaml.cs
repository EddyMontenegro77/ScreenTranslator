using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenTranslator.Views
{
    public partial class SelectionOverlayWindow : Window
    {
        private Point _startPoint;
        private bool _isSelecting;

        public Rect SelectedArea { get; private set;  }
        public bool WasCancelled { get; private set; } = true;

        public SelectionOverlayWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(this);
            _isSelecting = true;
            
            Canvas.SetLeft(SelectionRectangle, _startPoint.X);
            Canvas.SetTop(SelectionRectangle, _startPoint.Y);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
            SelectionRectangle.Visibility = Visibility.Visible;

        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting) return;

            var currentPoint = e.GetPosition(this);

            // Supports dragging from any direction
            double x = Math.Min(_startPoint.X, currentPoint.X);
            double y = Math.Min(_startPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - _startPoint.X);
            double height = Math.Abs(currentPoint.Y - _startPoint.Y);

            Canvas.SetLeft(SelectionRectangle, x);
            Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(!_isSelecting) return;

            _isSelecting = false;

            double x = Canvas.GetLeft(SelectionRectangle);
            double y = Canvas.GetTop(SelectionRectangle);

            SelectedArea = new Rect(x, y, SelectionRectangle.Width, SelectionRectangle.Height);
            // Avoids accidental click
            WasCancelled = SelectedArea.Width < 5 || SelectedArea.Height < 5;

            DialogResult = !WasCancelled;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                WasCancelled = true;
                DialogResult = false;
                Close();
            }
        }
    }
}