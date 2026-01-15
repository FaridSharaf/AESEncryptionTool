# Release Notes

## Version 2.1.0 (Latest)

**Performance & Modernization Update**

### 🚀 Improvements

- **Upgraded to .NET 8**: ~30% faster runtime performance and long-term support.
- **Smaller Build Size**: Executable reduced from 164MB to 72MB (56% smaller) via compression.
- **System Tray Support**: Added "Minimize to Tray" and "Close to Tray" options in Settings.
- **UI Polish**: Added scrollbars to Settings, Operations, and Batch tabs for small screens.
- **Dynamic Tooltips**: Minimize button now reflects tray behavior; improved error dialogs.
- **All 43 Tests Passing**: Verified compatibility with .NET 8.

---

## Version 2.0.0

**Major Release** - "Data Management & UI Polish"

### 🚀 New Features

- **Data Import/Export**: Backup and Restore your entire history and bookmarks to JSON files.
- **Deep Ocean Theme**: A refined dark interface with custom title bars and improved contrast.
- **Batch Processing UI**: Drag-and-drop batch processing for Excel/CSV files.
- **Improved Settings**: Dedicated configuration window with auto-copy and auto-detect toggles.

### 🛠 Improvements

- **Performance**: Optimized startup time and history loading.
- **Icons**: Switched to high-compatibility Emoji icons (removed font dependencies).
- **Stability**: Fixed potential null reference issues in batch processor.
- **Logging**: Enhanced status bar messages for specific operations.

### 🧪 Testing

- **Unit Tests**: Added comprehensive test suite with 43 tests.
- **Coverage**: AESCryptography, HistoryManager, ConfigManager fully tested.
- **CI Ready**: Run `dotnet test` to verify all functionality.

---

## Version 1.0 - 1.3

- Initial release with Double AES Encryption.
- History and Bookmark management.
- Basic Batch processing.
