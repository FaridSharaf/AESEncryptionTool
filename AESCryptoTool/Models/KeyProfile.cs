using System;

namespace AESCryptoTool.Models
{
    public class KeyProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "New Profile";
        
        // Plaintext keys
        public string Key { get; set; } = "";
        public string IV { get; set; } = "";
        
        // Future proofing / alternative formats
        public string KeyBase64 { get; set; } = "";
        public string IVBase64 { get; set; } = "";
    }
}
