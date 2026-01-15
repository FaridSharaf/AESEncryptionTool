namespace AESCryptoTool.Models
{
    public class AppConfig
    {
        // Profiles
        public System.Collections.Generic.List<KeyProfile> Profiles { get; set; } = new();
        public System.Guid SelectedProfileId { get; set; }

        // Plaintext keys for double encryption (phone numbers)

        [System.Obsolete("Use Profiles instead.")]
        public string Key { get; set; } = string.Empty;
        [System.Obsolete("Use Profiles instead.")]
        public string IV { get; set; } = string.Empty;
        
        // Base64 keys for single encryption (ClientId/X-Message)
        [System.Obsolete("Use Profiles instead.")]
        public string KeyBase64 { get; set; } = string.Empty;
        [System.Obsolete("Use Profiles instead.")]
        public string IVBase64 { get; set; } = string.Empty;
    }
}


