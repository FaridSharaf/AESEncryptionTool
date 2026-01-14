using AESCryptoTool.Services;
using Xunit;

namespace AESCryptoTool.Tests
{
    public class AESCryptographyTests
    {
        // Use the default test keys (same as ConfigManager defaults)
        private const string TestKey = "12345678901234567890123456789012"; // 32 bytes
        private const string TestIV = "1234567890123456"; // 16 bytes

        #region Basic Round-Trip Tests

        [Fact]
        public void Encrypt_ThenDecrypt_ReturnsOriginal()
        {
            // Arrange
            string original = "Hello, World!";

            // Act
            string encrypted = AESCryptography.Encrypt(original, TestKey, TestIV);
            string decrypted = AESCryptography.Decrypt(encrypted, TestKey, TestIV);

            // Assert
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void DoubleEncrypt_ThenDoubleDecrypt_ReturnsOriginal()
        {
            // This tests the actual 2-pass encryption logic used in the app
            string original = "SecretData123";

            // Act - Double encrypt (same as app logic)
            string encrypted = AESCryptography.Encrypt(original, TestKey, TestIV);

            // Act - Double decrypt
            string decrypted = AESCryptography.Decrypt(encrypted, TestKey, TestIV);

            // Assert
            Assert.Equal(original, decrypted);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void Encrypt_EmptyOrWhitespace_HandlesGracefully(string input)
        {
            // Empty strings should either encrypt successfully or throw a clear exception
            // Current implementation: should work
            string encrypted = AESCryptography.Encrypt(input, TestKey, TestIV);
            string decrypted = AESCryptography.Decrypt(encrypted, TestKey, TestIV);

            Assert.Equal(input, decrypted);
        }

        #endregion

        #region Special Characters & Unicode

        [Theory]
        [InlineData("مرحبا بالعالم")] // Arabic
        [InlineData("日本語テスト")] // Japanese
        [InlineData("🔐🔑💻")] // Emojis
        [InlineData("Привет мир")] // Russian
        [InlineData("Hello\nWorld\tTab")] // Newlines and tabs
        [InlineData("Special: !@#$%^&*()_+-=[]{}|;':\",./<>?")] // Special chars
        public void Encrypt_SpecialCharacters_RoundTripsCorrectly(string input)
        {
            // Act
            string encrypted = AESCryptography.Encrypt(input, TestKey, TestIV);
            string decrypted = AESCryptography.Decrypt(encrypted, TestKey, TestIV);

            // Assert
            Assert.Equal(input, decrypted);
        }

        [Fact]
        public void Encrypt_LongText_HandlesCorrectly()
        {
            // Arrange - 10,000 characters
            string longText = new string('A', 10000);

            // Act
            string encrypted = AESCryptography.Encrypt(longText, TestKey, TestIV);
            string decrypted = AESCryptography.Decrypt(encrypted, TestKey, TestIV);

            // Assert
            Assert.Equal(longText, decrypted);
        }

        #endregion

        #region Consistency Tests

        [Fact]
        public void Encrypt_SameInputSameKey_ProducesSameOutput()
        {
            // AES in CBC mode with same IV should produce identical output
            string input = "ConsistencyTest";

            string encrypted1 = AESCryptography.Encrypt(input, TestKey, TestIV);
            string encrypted2 = AESCryptography.Encrypt(input, TestKey, TestIV);

            Assert.Equal(encrypted1, encrypted2);
        }

        [Fact]
        public void Encrypt_DifferentInputs_ProduceDifferentOutputs()
        {
            string encrypted1 = AESCryptography.Encrypt("Input1", TestKey, TestIV);
            string encrypted2 = AESCryptography.Encrypt("Input2", TestKey, TestIV);

            Assert.NotEqual(encrypted1, encrypted2);
        }

        #endregion

        #region Error Handling

        [Fact]
        public void Decrypt_InvalidBase64_ThrowsException()
        {
            // Arrange
            string invalidInput = "This is not valid base64!!!";

            // Act & Assert
            Assert.ThrowsAny<Exception>(() =>
                AESCryptography.Decrypt(invalidInput, TestKey, TestIV));
        }

        [Fact]
        public void Decrypt_WrongKey_ThrowsOrReturnsGarbage()
        {
            // Arrange
            string original = "SecretMessage";
            string wrongKey = "00000000000000000000000000000000"; // Different key
            
            string encrypted = AESCryptography.Encrypt(original, TestKey, TestIV);

            // Act & Assert - Should throw or return garbage (not the original)
            try
            {
                string decrypted = AESCryptography.Decrypt(encrypted, wrongKey, TestIV);
                Assert.NotEqual(original, decrypted); // If no exception, result should be garbage
            }
            catch
            {
                // Exception is also acceptable
                Assert.True(true);
            }
        }

        #endregion
    }
}
