# AES Encryption Tool

A modern WPF desktop application for encrypting and decrypting text using **double AES encryption**. Built for developers who need to quickly encrypt/decrypt sensitive data like phone numbers, client IDs, or other identifiers during debugging.

![.NET](https://img.shields.io/badge/.NET-6.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
![License](https://img.shields.io/badge/License-MIT-green)
![Version](https://img.shields.io/badge/Version-2.0-orange)
![Tests](https://img.shields.io/badge/Tests-43%20Passing-brightgreen)

## ✨ Features

### 🔐 Double Encryption

The tool applies AES encryption **twice** for enhanced security:

1. **First Pass**: Encrypts plaintext → Base64 ciphertext
2. **Second Pass**: Encrypts the Base64 → Final encrypted output

This provides an extra layer of protection, making brute-force attacks significantly harder.

### Core Features

- **Auto-detect** - Automatically detects if input is encrypted or plain text
- **Quick Copy** - Double-click output to copy, auto-copy option available
- **Keyboard Shortcuts** - Press Enter to encrypt/decrypt

### UI & Organization

- **🌊 Deep Ocean Theme** - Modern dark UI with Sky Blue & Teal accents
- **📑 Tabbed Interface** - Organized into Operations, History, and Bookmarks tabs
- **🔍 Search** - Search through history and bookmarks

### Data Management

- **💾 Export/Import** - Backup your complete History and Bookmarks to JSON
- **📜 History** - Track all encryption/decryption operations
- **🔖 Bookmarks** - Mark important entries (stored separately)
- **⚙️ Configurable Limits** - Set max records for history (500) and bookmarks (100)
- **🧹 Clear Functions** - Clear history or bookmarks independently

### Security

- **🔐 Secure Key Storage** - Keys encrypted using Windows DPAPI
- **👁️ Masked Keys** - Keys partially masked (first 2 + last 4 chars visible)
- **📂 Separate Storage** - History and bookmarks in separate JSON files

## 🔒 How Double Encryption Works

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Plaintext  │ ──► │  1st AES    │ ──► │  2nd AES    │ ──► Final Output
│  "Hello"    │     │  Encrypt    │     │  Encrypt    │
└─────────────┘     └─────────────┘     └─────────────┘

Decrypt reverses the process:
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Encrypted  │ ──► │  1st AES    │ ──► │  2nd AES    │ ──► Plaintext
│  Input      │     │  Decrypt    │     │  Decrypt    │
└─────────────┘     └─────────────┘     └─────────────┘
```

## 📸 Screenshot

The application features a tabbed layout with:

- **Operations Tab**: Encrypt/Decrypt panels + Recent Items
- **Batch Tab**: Process Excel/CSV files in bulk with global drag & drop (from any tab), visual drop overlay, preview, and row count
- **History Tab**: Searchable history with bookmark/copy/delete actions
- **Bookmarks Tab**: Dedicated view for bookmarked items

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
   cd AESCryptoTool
   dotnet build
   dotnet run
   ```

3. **Configure your keys** (first-time setup)
   - Click the 🔐 **Key/IV Configuration** expander
   - Click 👁️ to reveal the key fields (shows partial mask)
   - Enter your AES key (32 characters for AES-256)
   - Enter your IV (16 characters)
   - Click **💾 Save**

## 🔧 Configuration

### Key Requirements

| Key Type    | Length        | Description                       |
| ----------- | ------------- | --------------------------------- |
| **AES-128** | 16 characters | Standard security                 |
| **AES-192** | 24 characters | Enhanced security                 |
| **AES-256** | 32 characters | Maximum security (recommended)    |
| **IV**      | 16 characters | Initialization Vector (always 16) |

### Data Storage

All data is stored in `%AppData%\AESEncryptionTool\`:

| File               | Description                           |
| ------------------ | ------------------------------------- |
| `config.encrypted` | Keys (DPAPI encrypted, user-specific) |
| `settings.json`    | User preferences                      |
| `history.json`     | All history entries                   |
| `bookmarks.json`   | Bookmarked entries (copies)           |

## 📖 Usage

### Encrypt / Decrypt

1. Go to **Operations** tab
2. Enter text in the respective panel
3. Optionally click 🏷️ to bookmark before processing
4. Press **Enter** or click the action button
5. Output is auto-copied (if enabled)

### Managing History

- **Search**: Filter by input, output, or note
- **Bookmark**: Click 🔖 to add to Bookmarks tab
- **Copy**: Click 📋 to copy output
- **Delete**: Click 🗑️ to remove entry

### Settings (⚙️)

- **Auto-copy**: Auto-copy results to clipboard
- **Auto-detect**: Show hints for input type detection
- **Recent items count**: Items in quick-access bar
- **Max History/Bookmarks**: Storage limits (max 1000)

## 🏗️ Project Structure

```
AESCryptoTool/
├── Models/
│   ├── AppConfig.cs       # Key configuration
│   ├── AppSettings.cs     # User preferences
│   └── HistoryEntry.cs    # History/Bookmark model
├── Services/
│   ├── AESCryptography.cs # Double encryption logic
│   ├── BatchProcessor.cs  # Bulk Excel/CSV processing
│   ├── ConfigManager.cs   # Key & settings management
│   └── HistoryManager.cs  # History & Bookmarks persistence
├── Views/
│   ├── MainWindow.xaml    # Main UI (Deep Ocean theme)
│   ├── MainWindow.xaml.cs # Event handlers
│   ├── SettingsWindow.xaml
│   └── NoteEditDialog.xaml
└── AESCryptoTool.csproj
```

## 🔒 Security Notes

- Default keys in source code are **placeholders only**
- Never commit real encryption keys to version control
- Keys are stored encrypted locally using Windows DPAPI
- Keys are displayed with partial masking for security
- **Double encryption** provides extra protection against attacks
- History/Bookmarks stored in plain JSON

## 🛠️ Development

### Building from Source

```bash
# Clone
git clone https://github.com/FaridSharaf/AESEncryptionTool.git

# Build
dotnet build

# Run
cd AESCryptoTool
dotnet run

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

**v1.0** - Made with ❤️ for developers
