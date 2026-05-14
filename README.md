# Multicopy
## Copy a folder to multiple USB drives simultaneously

Forked from [rphi/Multicopy](https://github.com/rphi/Multicopy) and rebuilt for modern Windows.

---

### Multicopy2 — Modern Rebuild (WPF · .NET 9)

`Multicopy2/` is a ground-up rewrite that fixes several issues in the original and adds new features:

**Bug fixes vs. original:**
- UI no longer freezes during copy — uses `async/await` throughout
- Erase logic was broken (used `VolumeLabel` as a path); now correctly clears root directory contents

**New features:**
- Per-drive progress bars with percentage, bytes copied / total
- Live copy speed (MB/s) and ETA per drive
- Overwrite existing files option
- Cancel button — stops all drives at the next file boundary
- Drives remember their checked state across rescans
- Validation before copy starts with clear error messages

**Requirements:**
- Windows 10 or 11 (64-bit)
- [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (or SDK to build)

**Build:**
```
dotnet build Multicopy2/Multicopy2.csproj -c Release
```

**Run:**
```
dotnet run --project Multicopy2/Multicopy2.csproj
```

Or open `Multicopy2.sln` in Visual Studio 2022.

---

### Original (Multicopy/)

The original .NET 4.6.1 WinForms project is preserved in `Multicopy/` for reference.
Tested on Windows 10 Pro 64-bit (version 1511 build 10586.420).
