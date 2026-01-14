using AESCryptoTool.Services;
using Xunit;

namespace AESCryptoTool.Tests
{
    public class ConfigManagerTests
    {
        #region Key Validation Tests

        [Theory]
        [InlineData("1234567890123456", true)] // 16 bytes - AES-128
        [InlineData("123456789012345678901234", true)] // 24 bytes - AES-192
        [InlineData("12345678901234567890123456789012", true)] // 32 bytes - AES-256
        public void IsValidPlaintextKey_ValidLengths_ReturnsTrue(string key, bool expected)
        {
            bool result = ConfigManager.IsValidPlaintextKey(key);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("", false)] // Empty
        [InlineData("123456789012345", false)] // 15 bytes
        [InlineData("12345678901234567", false)] // 17 bytes
        [InlineData("1234567890123456789012345", false)] // 25 bytes
        [InlineData("123456789012345678901234567890123", false)] // 33 bytes
        public void IsValidPlaintextKey_InvalidLengths_ReturnsFalse(string key, bool expected)
        {
            bool result = ConfigManager.IsValidPlaintextKey(key);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("1234567890123456", true)] // 16 bytes - valid IV
        public void IsValidPlaintextIV_ValidLength_ReturnsTrue(string iv, bool expected)
        {
            bool result = ConfigManager.IsValidPlaintextIV(iv);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("", false)] // Empty
        [InlineData("123456789012345", false)] // 15 bytes
        [InlineData("12345678901234567", false)] // 17 bytes
        public void IsValidPlaintextIV_InvalidLength_ReturnsFalse(string iv, bool expected)
        {
            bool result = ConfigManager.IsValidPlaintextIV(iv);
            Assert.Equal(expected, result);
        }

        #endregion

        #region Load Keys Tests

        [Fact]
        public void LoadKeys_ReturnsNonNullConfig()
        {
            // Act
            var config = ConfigManager.LoadKeys();

            // Assert
            Assert.NotNull(config);
            Assert.False(string.IsNullOrEmpty(config.Key));
            Assert.False(string.IsNullOrEmpty(config.IV));
        }

        [Fact]
        public void LoadKeys_DefaultConfig_HasValidKeyLengths()
        {
            // Act
            var config = ConfigManager.LoadKeys();

            // Assert
            Assert.True(ConfigManager.IsValidPlaintextKey(config.Key));
            Assert.True(ConfigManager.IsValidPlaintextIV(config.IV));
        }

        #endregion

        #region Settings Tests

        [Fact]
        public void LoadSettings_ReturnsNonNullSettings()
        {
            // Act
            var settings = ConfigManager.LoadSettings();

            // Assert
            Assert.NotNull(settings);
        }

        [Fact]
        public void SaveSettings_ThenLoadSettings_PersistsValues()
        {
            // Arrange
            var settings = ConfigManager.LoadSettings();
            int originalCount = settings.RecentItemsCount;
            
            settings.RecentItemsCount = 999;
            
            // Act
            ConfigManager.SaveSettings(settings);
            var loadedSettings = ConfigManager.LoadSettings();

            // Assert
            Assert.Equal(999, loadedSettings.RecentItemsCount);

            // Cleanup - restore original
            loadedSettings.RecentItemsCount = originalCount;
            ConfigManager.SaveSettings(loadedSettings);
        }

        #endregion

        #region Base64 Validation Tests

        [Theory]
        [InlineData("MTIzNDU2Nzg5MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTI=", 32, true)] // Valid 32-byte key in Base64
        [InlineData("MTIzNDU2Nzg5MDEyMzQ1Ng==", 16, true)] // Valid 16-byte IV in Base64
        [InlineData("invalid base64!!!", 32, false)] // Invalid Base64
        [InlineData("", 32, false)] // Empty
        public void IsValidBase64Key_VariousInputs_ReturnsExpected(string base64, int expectedBytes, bool expected)
        {
            bool result = ConfigManager.IsValidBase64Key(base64, expectedBytes);
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
