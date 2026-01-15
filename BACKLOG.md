# AESCryptoTool - Feature Backlog

## 🚀 V3 Priorities (Next Release)

Focus: Optimization, Modernization, and UI Polish.

| Feature                   | Category | Effort | Description                                                      |
| ------------------------- | -------- | ------ | ---------------------------------------------------------------- |
| **Upgrade to .NET 8**     | Core     | Easy   | Upgrade from .NET 6 for 30% performance boost & longevity.       |
| **App Size Optimization** | Core     | Easy   | Enable Trimming/Compression to reduce size (160MB -> ~60MB).     |
| **Material Design UI**    | UI       | Medium | Implement modern Material Design (Cards, Shadows, Ripples).      |
| **Multiple Key Profiles** | Workflow | Medium | Save & Switch between different encryption keys (e.g. Dev/Prod). |
| **Always on Top**         | UI       | Easy   | Button to keep window float on top.                              |

---

## 📦 Batch Processing Enchancements

**Current Status:** Phase 1 Complete (Drag & Drop, Preview, Row Count).

### Phase 2: Core Improvements (Medium)

| Feature                    | Description                              |
| -------------------------- | ---------------------------------------- |
| **Multi-Column Selection** | Encrypt/decrypt multiple columns at once |
| **Column Name in Output**  | Add a new column instead of replacing    |
| **Detailed Error Log**     | Export failed rows with reasons          |
| **Pause/Resume**           | Pause and resume batch processing        |
| **Progress ETA**           | Show estimated time remaining            |
| **Cancel Confirmation**    | Confirm before aborting mid-process      |

### Phase 3: Advanced (Hard)

| Feature                  | Description                                 |
| ------------------------ | ------------------------------------------- |
| **Template Presets**     | Save column/operation settings as templates |
| **Scheduling**           | Schedule batch jobs to run later            |
| **Watch Folder**         | Auto-process files dropped in a folder      |
| **Large File Streaming** | Process files > 100MB without memory issues |

---

## 🔐 Security Enhancements

| Feature                 | Description                                              | Effort |
| ----------------------- | -------------------------------------------------------- | ------ |
| Password Protection     | Optional password to open the app                        | Medium |
| Auto-lock               | Lock app after X minutes of inactivity                   | Medium |
| Clear Clipboard on Exit | Auto-clear clipboard when closing                        | Easy   |
| Encryption Algorithms   | Support for other algorithms (DES, Triple DES, Blowfish) | Hard   |

## 🎨 Future UI Ideas

| Feature            | Description                           | Effort |
| ------------------ | ------------------------------------- | ------ |
| Compact Mode       | Smaller UI for less screen space      | Medium |
| Keyboard Shortcuts | More shortcuts (Ctrl+E, Ctrl+D, etc.) | Easy   |

## ⚡ Workflow Improvements

| Feature              | Description                             | Effort |
| -------------------- | --------------------------------------- | ------ |
| Quick Actions        | Right-click context menu                | Medium |
| Clipboard Monitoring | Auto-detect encrypted text in clipboard | Medium |
| Compare Tool         | Compare two encrypted values            | Easy   |

## 📊 Analytics

| Feature          | Description                               | Effort |
| ---------------- | ----------------------------------------- | ------ |
| Statistics       | Total encryptions, decryptions, favorites | Easy   |
| Key Info Display | Show key strength/type (AES-128/192/256)  | Easy   |

---

## ✅ Completed (V1 - V2.1)

- [x] **System Tray** (V2.1)
- [x] **Scrollable UI Support** (V2.1)
- [x] **Import/Export History** (V2.0)
- [x] **Batch Drag & Drop** (V2.0)
- [x] **Batch Preview** (V2.0)
- [x] **Themes (Deep Ocean)** (V1.3)
- [x] **Double AES Encryption** (V1.0)
- [x] **History & Bookmarks** (V1.0)
