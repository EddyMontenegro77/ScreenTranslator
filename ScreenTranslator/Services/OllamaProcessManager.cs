using ScreenTranslator.Models.Translation;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

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
            // Avoid having simultaneous calls (ex. from multiple captures in a short time).
            // start the process at the same time.
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
                    $"Ollama could not be initiated from: '{executablePath}'. " +
                    "Is it installed correctly?", ex);
            }
        }

        private string FindOllamaExecutable()
        {
            // 1. Standard installation path for Ollama on Windows
            string standardPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Ollama", "ollama.exe");

            if (File.Exists(standardPath))
                return standardPath;

            // 2. If not found, check if it's in the PATH
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
                    // If the process has exited, it might be because another instance
                    // might be using the port. Let's try a few more times to check if it's running
                    // waiting before throwing an exception.
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
                        $"Ollama process ended unexpectedly (code {_process.ExitCode}). " +
                        (string.IsNullOrWhiteSpace(extra)
                            ? "Another instance might be using port 11434."
                            : extra));
                }

                if (await IsRunningAsync())
                    return;

                await Task.Delay(intervalMs);
                elapsed += intervalMs;
            }

            throw new TimeoutException(
                $"Ollama did not respond within {timeoutSeconds} seconds.");
        }

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = await client.GetAsync("http://localhost:11434/api/tags");
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OllamaTagsResponse>(body);

                return result?.Models.Select(m => m.Name).ToList() ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
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
                    // Not worth for the app to crash if we can't kill the process. It might have already exited or been killed by the user.
                }
            }

            _process?.Dispose();
            _lock.Dispose();
        }
    }
}
