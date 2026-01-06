# AES Encryption Tool

A modern WPF desktop application for encrypting and decrypting text using AES encryption. Built for developers who need to quickly encrypt/decrypt sensitive data like phone numbers, client IDs, or other identifiers during debugging.

![.NET](https://img.shields.io/badge/.NET-6.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ Features

- **Double Encryption** - Encrypts text twice for extra security (configurable)
- **Modern Dark UI** - Sleek, eye-friendly dark theme with accent colors
- **History & Favorites** - Track all operations with ⭐ favorites support
- **Auto-detect** - Automatically detects if input is encrypted or plain text
- **Secure Key Storage** - Keys encrypted using Windows DPAPI (user-specific)
- **Quick Copy** - Double-click output to copy, auto-copy option available
- **Search History** - Find past operations quickly

## 📸 Screenshot

The application features a split-view layout with Encrypt and Decrypt panels side by side, a searchable history section, and quick-access recent items.

## 🚀 Getting Started

### Prerequisites

- Windows 10/11
- [.NET 6.0 Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) (or SDK for development)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/FaridSharaf/AESEncryptionTool.git
   cd AESEncryptionTool
   ```

2. **Build and run**
   ```bash
   dotnet build
   dotnet run --project ConsoleApp1
   ```

3. **Configure your keys** (first-time setup)
   - Click the 🔐 **Key/IV Configuration** expander
   - Click 👁️ to reveal the key fields
   - Enter your AES key (16, 24, or 32 characters)
   - Enter your IV (16 characters)
   - Click **💾 Save**

## 🔧 Configuration

### Key Requirements

| Key Type | Length | Description |
|----------|--------|-------------|
| **AES-128** | 16 characters | Standard security |
| **AES-192** | 24 characters | Enhanced security |
| **AES-256** | 32 characters | Maximum security |
| **IV** | 16 characters | Initialization Vector (always 16) |

### Key Storage

Keys are stored securely using **Windows DPAPI** (Data Protection API):
- Location: `%AppData%\AESEncryptionTool\config.encrypted`
- Encryption: User-specific (cannot be decrypted by other users/machines)
- To reset: Click **🔄 Reset** or delete the config file

## 📖 Usage

### Encrypt Text
1. Enter plain text in the **ENCRYPT** panel
2. Press **Enter** or click **🔐 Encrypt**
3. Output is automatically copied to clipboard (if enabled)

### Decrypt Text
1. Paste encrypted text in the **DECRYPT** panel
2. Press **Enter** or click **🔓 Decrypt**
3. Output is automatically copied to clipboard (if enabled)

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Enter` | Encrypt/Decrypt (in respective panel) |
| `Esc` | Clear current input |
| `Double-click output` | Copy to clipboard |

### History Features

- **Search**: Type in the search box to filter history
- **Favorites**: Click ⭐ to mark important items
- **Copy**: Click 📋 on any row to copy output
- **Filter**: Toggle between "All" and "Favorites" view

## 🏗️ Project Structure

```
ConsoleApp1/
├── Models/
│   ├── AppConfig.cs       # Key configuration model
│   ├── AppSettings.cs     # User preferences
│   └── HistoryEntry.cs    # History item model
├── Services/
│   ├── AESCryptography.cs # Encryption/decryption logic
│   ├── ConfigManager.cs   # Key & settings management
│   └── HistoryManager.cs  # History persistence
├── Views/
│   ├── MainWindow.xaml    # Main UI
│   ├── MainWindow.xaml.cs # Main logic
│   ├── SettingsWindow.xaml
│   └── NoteEditDialog.xaml
├── App.xaml
└── ConsoleApp1.csproj
```

## ⚙️ Settings

Access settings via the **⚙️ Settings** button:

- **Auto-copy**: Automatically copy results to clipboard
- **Auto-detect**: Show hints when input type is detected
- **Recent items count**: Number of items in quick-access bar

## 🔒 Security Notes

- Default keys in source code are **placeholders only**
- Never commit real encryption keys to version control
- Keys are stored encrypted locally using DPAPI
- History is stored in plain JSON (consider this for sensitive data)

## 🛠️ Development

### Building from Source

```bash
# Clone
git clone https://github.com/FaridSharaf/AESEncryptionTool.git

# Build
dotnet build

# Run
dotnet run --project ConsoleApp1

# Publish (single file)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Dependencies

- .NET 6.0 Windows Desktop Runtime
- WPF (Windows Presentation Foundation)
- System.Security.Cryptography (for AES & DPAPI)

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

Made with ❤️ for developers who debug encrypted data