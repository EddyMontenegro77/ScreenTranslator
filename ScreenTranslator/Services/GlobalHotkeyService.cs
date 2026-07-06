using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ScreenTranslator.Services
{
	public class GlobalHotkeyService : IDisposable
	{
		[DllImport("user32.dll")]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll")]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		private const int WM_HOTKEY = 0x0312;

		// Modifiers (can be changed with OR |)
		public const uint MOD_ALT = 0x0001;
		public const uint MOD_CONTROL = 0x0002;
		public const uint MOD_SHIFT = 0x0004;
		public const uint MOD_WIN = 0x0008;

		private HwndSource? _source;
		private int _currentId;
		private readonly Dictionary<int, Action> _callbacks = new();

		public void Register(Window window, uint modifiers, uint virtualKey, Action onPressed)
		{
			var helper = new WindowInteropHelper(window);

			// Si la ventana todavía no tiene handle nativo (no se mostró aún), forzamos su creación
			if (helper.Handle == IntPtr.Zero)
				helper.EnsureHandle();

			if (_source == null)
			{
				_source = HwndSource.FromHwnd(helper.Handle);
				_source?.AddHook(WndProc);
			}

			int id = _currentId++;
			_callbacks[id] = onPressed;

			bool success = RegisterHotKey(helper.Handle, id, modifiers, virtualKey);

			if (!success)
				throw new InvalidOperationException(
					"No se pudo registrar el atajo global. Puede que ya esté en uso por otra aplicación.");
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_HOTKEY)
			{
				int id = wParam.ToInt32();
				if (_callbacks.TryGetValue(id, out var callback))
				{
					callback.Invoke();
					handled = true;
				}
			}
			return IntPtr.Zero;
		}

		public void Dispose()
		{
			if (_source != null)
			{
				var handle = _source.Handle;
				foreach (var id in _callbacks.Keys)
					UnregisterHotKey(handle, id);

				_source.RemoveHook(WndProc);
				_source = null;
			}
		}
	}
}