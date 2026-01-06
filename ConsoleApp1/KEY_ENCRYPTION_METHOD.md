# Key Encryption Method

## Overview
The AES Encryption Tool stores encryption keys securely using **Windows DPAPI (Data Protection API)**.

## How It Works

### Key Storage
- Keys are stored in: `%AppData%\AESEncryptionTool\config.encrypted`
- The config file is encrypted using Windows DPAPI with `DataProtectionScope.CurrentUser`
- This means:
  - Keys are encrypted using your Windows user account context
  - Keys cannot be decrypted on a different machine or by a different user
  - No admin permissions required
  - The encryption key is derived from your Windows user profile (not stored anywhere)

### Key Format Conversion
1. **User Input**: You enter keys as plain text (e.g., "54254455")
2. **Conversion**: The program converts plain text to Base64 format
   - Example: "54254455" → "NTQyNTQ0NTU="
3. **Storage**: Base64 keys are encrypted using DPAPI and saved to config file
4. **Usage**: When encrypting/decrypting, the program uses the Base64 format keys

### Default Keys
If no keys are configured, the program uses these default Base64 keys:
- **Key**: `c8o5P2jFmO9oJp2jwv7kougqK8yuTv8WJLXApFcP1Xo=`
- **IV**: `DSncouW2MffV+StRD0RZHg==`

### Technical Details
- **Encryption Method**: `ProtectedData.Protect()` / `ProtectedData.Unprotect()`
- **Scope**: `DataProtectionScope.CurrentUser`
- **Entropy**: None (uses Windows user context only)
- **File Format**: Encrypted binary file (cannot be manually edited)

### Security Notes
- The config file is encrypted and cannot be manually edited
- Keys are protected by Windows user account security
- If you need to share the program with pre-configured keys, you have two options:
  1. **First Run Setup**: Users enter keys on first launch (recommended)
  2. **Pre-configure**: Use the same Windows user account to set up keys, then distribute (keys will work only on that user account)

### For Distribution
If you need to distribute the tool with pre-configured keys:
1. Set up keys on your development machine
2. Copy the `config.encrypted` file to the target machine
3. **Important**: The keys will only work if:
   - The target machine uses the same Windows user account, OR
   - You provide a setup utility that allows users to enter keys on first run

### Recommended Approach
For distribution, it's recommended to:
1. Let users enter keys on first launch
2. Or provide a simple setup wizard/utility
3. Keys are then encrypted and stored securely on their machine



