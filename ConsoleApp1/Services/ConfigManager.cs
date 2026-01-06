using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Models;

namespace ConsoleApp1.Services
{
    public class ConfigManager
    {
        private static readonly string ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AESEncryptionTool");

        private static readonly string ConfigFile = Path.Combine(ConfigDirectory, "config.encrypted");
        private static readonly string SettingsFile = Path.Combine(ConfigDirectory, "settings.json");
        private static readonly string UserDefaultsFile = Path.Combine(ConfigDirectory, "defaults.encrypted");

        // Default keys for double encryption (plaintext format)
        // These must be exactly 32 bytes for key and 16 bytes for IV
        public const string DefaultKey = "12345678901234567890123456789012"; // 32 chars = 32 bytes
        public const string DefaultIV = "1234567890123456"; // 16 chars = 16 bytes

        // Default keys for single encryption (Base64 format) - for X-Message/ClientId
        // These are placeholders - configure your actual keys via UI
        public const string DefaultKeyBase64 = "MTIzNDU2Nzg5MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTI="; // placeholder
        public const string DefaultIVBase64 = "MTIzNDU2Nzg5MDEyMzQ1Ng=="; // placeholder

        static ConfigManager()
        {
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }
        }

        /// <summary>
        /// Saves keys encrypted using Windows DPAPI
        /// </summary>
        public static void SaveKeys(string key, string iv, string keyBase64, string ivBase64)
        {
            var config = new AppConfig
            {
                Key = key,
                IV = iv,
                KeyBase64 = keyBase64,
                IVBase64 = ivBase64
            };

            string json = JsonSerializer.Serialize(config);
            byte[] data = Encoding.UTF8.GetBytes(json);

            byte[] encrypted = ProtectedData.Protect(
                data,
                null,
                DataProtectionScope.CurrentUser);

            File.WriteAllBytes(ConfigFile, encrypted);
        }

        /// <summary>
        /// Loads keys from encrypted config file
        /// </summary>
        public static AppConfig LoadKeys()
        {
            var (defaultKey, defaultIV) = GetDefaultKeys();
            
            if (!File.Exists(ConfigFile))
            {
                return new AppConfig
                {
                    Key = defaultKey,
                    IV = defaultIV,
                    KeyBase64 = DefaultKeyBase64,
                    IVBase64 = DefaultIVBase64
                };
            }

            try
            {
                byte[] encrypted = File.ReadAllBytes(ConfigFile);
                byte[] decrypted = ProtectedData.Unprotect(
                    encrypted,
                    null,
                    DataProtectionScope.CurrentUser);

                string json = Encoding.UTF8.GetString(decrypted);
                var config = JsonSerializer.Deserialize<AppConfig>(json);

                if (config == null)
                {
                    return new AppConfig
                    {
                        Key = defaultKey,
                        IV = defaultIV,
                        KeyBase64 = DefaultKeyBase64,
                        IVBase64 = DefaultIVBase64
                    };
                }

                // Ensure all values are set
                if (string.IsNullOrEmpty(config.Key)) config.Key = defaultKey;
                if (string.IsNullOrEmpty(config.IV)) config.IV = defaultIV;
                if (string.IsNullOrEmpty(config.KeyBase64)) config.KeyBase64 = DefaultKeyBase64;
                if (string.IsNullOrEmpty(config.IVBase64)) config.IVBase64 = DefaultIVBase64;

                return config;
            }
            catch
            {
                return new AppConfig
                {
                    Key = defaultKey,
                    IV = defaultIV,
                    KeyBase64 = DefaultKeyBase64,
                    IVBase64 = DefaultIVBase64
                };
            }
        }

        /// <summary>
        /// Resets keys to default values
        /// </summary>
        public static void ResetToDefaultKeys()
        {
            if (File.Exists(ConfigFile))
            {
                File.Delete(ConfigFile);
            }
            if (File.Exists(ConfigFile))
            {
                File.Delete(ConfigFile);
            }
        }

        /// <summary>
        /// Sets the current keys as the new default (user-defined defaults)
        /// </summary>
        public static void SetAsDefaultKeys(string key, string iv)
        {
            var userDefaults = new { Key = key, IV = iv };
            string json = JsonSerializer.Serialize(userDefaults);
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(UserDefaultsFile, encrypted);
        }

        /// <summary>
        /// Gets the default key (user-defined or hardcoded)
        /// </summary>
        public static (string Key, string IV) GetDefaultKeys()
        {
            if (File.Exists(UserDefaultsFile))
            {
                try
                {
                    byte[] encrypted = File.ReadAllBytes(UserDefaultsFile);
                    byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                    string json = Encoding.UTF8.GetString(decrypted);
                    var defaults = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (defaults != null && defaults.ContainsKey("Key") && defaults.ContainsKey("IV"))
                    {
                        return (defaults["Key"], defaults["IV"]);
                    }
                }
                catch { }
            }
            return (DefaultKey, DefaultIV);
        }

        /// <summary>
        /// Validates plaintext key length (must be 16, 24, or 32 bytes for AES)
        /// </summary>
        public static bool IsValidPlaintextKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            int byteCount = Encoding.UTF8.GetByteCount(key);
            return byteCount == 16 || byteCount == 24 || byteCount == 32;
        }

        /// <summary>
        /// Validates plaintext IV length (must be 16 bytes)
        /// </summary>
        public static bool IsValidPlaintextIV(string iv)
        {
            return !string.IsNullOrEmpty(iv) && Encoding.UTF8.GetByteCount(iv) == 16;
        }

        /// <summary>
        /// Validates Base64 key (must decode to 32 bytes for AES-256)
        /// </summary>
        public static bool IsValidBase64Key(string base64, int expectedBytes)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                return bytes.Length == expectedBytes;
            }
            catch
            {
                return false;
            }
        }

        public static void SaveSettings(AppSettings settings)
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }

        public static AppSettings LoadSettings()
        {
            if (!File.Exists(SettingsFile))
            {
                return new AppSettings();
            }

            try
            {
                string json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
    }
}
