# Release Notes

## Version 2.2.1 (Latest)

**UI Polish & Bug Fixes**

### 🐛 Bug Fixes

- **Tray Exit Crash**: Fixed crash when exiting via system tray "Exit" menu.
- **Recent Items Limit**: Fixed recent items limit not respecting user settings.

### 🛠 UI Improvements

- **Key/IV Header**: Replaced emoji icon with proper Fluent UI icon.
- **Recent Items**: Improved spacing and visual appearance (consistent button style).
- **Icon Consistency**: Replaced legacy emojis with standard text symbols for better cross-platform compatibility.
- **Dialog Modernization**: Updated NoteEditDialog and ProfileNameDialog to use WPF-UI components.
- **Settings Validation**: Added real-time validation for Recent Items limit (max 20).

### 🧹 Code Cleanup

- Removed unused `_pinButton` field.
- Improved null-safety in tray icon handling.

---

## Version 2.2.0

**Key Management & Batch Operations**

### 🔐 Multi-Profile Key Management

- **Key Profiles**: Create and switch between multiple encryption profiles (e.g., "Production", "Test", "Client A").
- **Seamless Switching**: Instantly change active keys without re-entering them.
- **Migration**: Existing legacy keys are automatically migrated to a "Default" profile.

### 🗑️ Batch Delete

- **Multi-Selection**: Use checkboxes to select multiple history or bookmark items.
- **Select All**: One-click selection for all visible items.
- **Dynamic Delete**: Button updates to show "Delete Selected (N)" or "Clear All" based on selection.

### 🛠 Improvements

- **Tray Notification**: Suppressed "Running in Tray" balloon tip (shows only once).
- **Dialogs**: Fixed text wrapping issues in confirmation dialogs.
- **UI Polish**: Cleaned up Profile Dialog and Message Boxes (removed unnecessary window controls).

---

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
