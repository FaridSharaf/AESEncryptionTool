namespace AESCryptoTool.Models
{
    public class AppConfig
    {
        // Plaintext keys for double encryption (phone numbers)
        public string Key { get; set; } = string.Empty;
        public string IV { get; set; } = string.Empty;
        
        // Base64 keys for single encryption (ClientId/X-Message)
        public string KeyBase64 { get; set; } = string.Empty;
        public string IVBase64 { get; set; } = string.Empty;
    }
}


