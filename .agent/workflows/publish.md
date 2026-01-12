---
description: Publish the application as a standalone single-file executable
---

This workflow builds a "Self-Contained" version of the app. This means it bundles the .NET Runtime inside the `.exe`, so users don't need to install anything. It runs as a portable app (no admin rights needed).

1. Run the publish command (Windows x64):
// turbo
```powershell
dotnet publish "d:\Laptop\dev\AESCryptoTool\AESCryptoTool\AESCryptoTool.csproj" -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "d:\Laptop\dev\AESCryptoTool\Publish"
```

2. The output file `AESCryptoTool.exe` will be located in `d:\Laptop\dev\AESCryptoTool\Publish`. You can ZIP this file or share it directly.
