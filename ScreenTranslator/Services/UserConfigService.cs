using System.IO;
using ScreenTranslator.Models;

namespace ScreenTranslator.Services
{
    public class UserConfigService
    {
        private readonly string _configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScreenTranslator", "UserConfig.json");
        public UserConfig LoadUserConfig()
        {
            if (!File.Exists(_configFilePath))
            {
                var defaultConfig = new UserConfig();
                SaveUserConfig(defaultConfig);
                return defaultConfig;
            }
            var json = File.ReadAllText(_configFilePath);
            return System.Text.Json.JsonSerializer.Deserialize<UserConfig>(json) ?? new UserConfig();
        }
        public void SaveUserConfig(UserConfig config)
        {
            var directory = Path.GetDirectoryName(_configFilePath)!;
            Directory.CreateDirectory(directory);

            var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFilePath, json);
        }
    }
}