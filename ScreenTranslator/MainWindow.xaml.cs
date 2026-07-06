using ScreenTranslator.Services;
using System.Windows;
using System.Windows.Input;

namespace ScreenTranslator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Services.GlobalHotkeyService _hotkeyService = new();
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Executes function when shortcut is pressed (CTRL+SHIFT+T)
            _hotkeyService.Register(
                this,
                GlobalHotkeyService.MOD_CONTROL | GlobalHotkeyService.MOD_SHIFT,
                (uint)KeyInterop.VirtualKeyFromKey(Key.T),
                onPressed: () =>
                {
                    BtnCapture_Click(this, null!);
                });
        }

        protected override void OnClosed(EventArgs e)
        {
            // Free resources and unregister hotkeys when the window is closed
            _hotkeyService.Dispose(); 
            base.OnClosed(e);
        }

        private void BtnCapture_Click(object sender, RoutedEventArgs e)
        {
            var overlay = new Views.SelectionOverlayWindow();
            this.WindowState = WindowState.Minimized;

            bool? result = overlay.ShowDialog();

            if (result == true && !overlay.WasCancelled)
            {
                Rect selectedArea = overlay.SelectedArea;
                MessageBox.Show($"Selected Area: {selectedArea}");
            }
        }
    }
}