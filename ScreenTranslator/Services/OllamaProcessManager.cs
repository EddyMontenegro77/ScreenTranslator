using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenTranslator.Services
{
    public class OllamaProcessManager : IDisposable
    {
        private const string OllamaHealthCheckUrl = "http://127.0.0.1:11434";

        private Process? _process;
        private bool _weStartedIt;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private StringBuilder? _stderrBuffer;
        private StringBuilder? _stdoutBuffer;

        public async Task EnsureRunningAsync(int timeoutSeconds = 30)
        {
            // Evita que dos llamadas simultáneas (ej: dos capturas rápidas) intenten
            // arrancar el proceso al mismo tiempo
            await _lock.WaitAsync();
            try
            {
                if (await IsRunningAsync())
                    return;

                StartProcess();
                await WaitUntilReadyAsync(timeoutSeconds);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<bool> IsRunningAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await client.GetAsync(OllamaHealthCheckUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private void StartProcess()
        {
            string executablePath = FindOllamaExecutable();

            // Prepare buffers to capture output
            _stderrBuffer = new StringBuilder();
            _stdoutBuffer = new StringBuilder();

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "serve",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            _process.ErrorDataReceived += (s, e) => { if (e.Data != null) lock (_stderrBuffer) _stderrBuffer.AppendLine(e.Data); };
            _process.OutputDataReceived += (s, e) => { if (e.Data != null) lock (_stdoutBuffer) _stdoutBuffer.AppendLine(e.Data); };

            try
            {
                _process.Start();
                _weStartedIt = true;

                _process.BeginErrorReadLine();
                _process.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"No se pudo iniciar Ollama desde '{executablePath}'. " +
                    "¿Está instalado correctamente?", ex);
            }
        }

        private string FindOllamaExecutable()
        {
            // 1. Ruta estándar de instalación de Ollama en Windows
            string standardPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Ollama", "ollama.exe");

            if (File.Exists(standardPath))
                return standardPath;

            // 2. Si no está ahí, confiamos en que esté en el PATH del sistema
            return "ollama.exe";
        }

        private async Task WaitUntilReadyAsync(int timeoutSeconds)
        {
            var elapsed = 0;
            const int intervalMs = 500;

            while (elapsed < timeoutSeconds * 1000)
            {
                if (_process is { HasExited: true })
                {
                    // Si el proceso terminó inmediatamente, es posible que otra instancia
                    // haya ocupado el puerto simultáneamente. Reintentar varias veces
                    // a la espera de que el servicio responda antes de fallar.
                    const int recheckAttempts = 5;
                    const int recheckDelayMs = 200;

                    for (int i = 0; i < recheckAttempts; i++)
                    {
                        if (await IsRunningAsync())
                            return;

                        await Task.Delay(recheckDelayMs);
                    }

                    var stderr = _stderrBuffer?.ToString();
                    var stdout = _stdoutBuffer?.ToString();
                    var extra = string.Empty;
                    if (!string.IsNullOrWhiteSpace(stderr))
                        extra = "STDERR: " + stderr;
                    else if (!string.IsNullOrWhiteSpace(stdout))
                        extra = "STDOUT: " + stdout;

                    throw new InvalidOperationException(
                        $"El proceso de Ollama terminó inesperadamente (código {_process.ExitCode}). " +
                        (string.IsNullOrWhiteSpace(extra)
                            ? "Puede que ya hubiera una instancia usando el puerto 11434."
                            : extra));
                }

                if (await IsRunningAsync())
                    return;

                await Task.Delay(intervalMs);
                elapsed += intervalMs;
            }

            throw new TimeoutException(
                $"Ollama no respondió dentro de {timeoutSeconds} segundos.");
        }

        public void Dispose()
        {
            if (_weStartedIt && _process is { HasExited: false })
            {
                try
                {
                    _process.Kill();
                    _process.WaitForExit(2000);
                }
                catch
                {
                    // No vale la pena crashear el cierre de la app por esto
                }
            }

            _process?.Dispose();
            _lock.Dispose();
        }
    }
}
