# Multicopy2
### Copy a folder to multiple USB drives simultaneously

A modern rebuild of [rphi/Multicopy](https://github.com/rphi/Multicopy) — original concept and code by Robert Phipps, 2016.
This fork is maintained by Justin Pulley and is unaffiliated with the original author.

---

## What's new vs. the original

**Bug fixes:**
- UI no longer freezes during copy (original called `Task.WaitAll` on the UI thread)
- Erase logic was broken — original used `VolumeLabel` as a filesystem path; now correctly clears the root directory

**New features:**
- Per-drive progress bars with live percentage and byte counts
- Copy speed (MB/s) and ETA per drive
- Overwrite existing files option
- Cancel in-progress copy (stops at the next file boundary)
- Validation before copy starts with clear error messages

---

## Requirements

- Windows 10 or 11 (64-bit)
- [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) — the installer will prompt you if it's missing

---

## Install

Download `Multicopy2-x.x.x-Setup.exe` from [Releases](https://github.com/jpect/Multicopy/releases) and run it.

---

## Build from source

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0), [Inno Setup 6](https://jrsoftware.org/isdl.php)

```powershell
# Run the app directly
& "C:\Program Files\dotnet\dotnet.exe" run --project Multicopy2\Multicopy2.csproj

# Build the installer
.\build-installer.ps1 -Version 1.0.0
# Output: installer\output\Multicopy2-1.0.0-Setup.exe
```

Or open `Multicopy2.sln` in Visual Studio 2022.

---

## Note on Windows SmartScreen

Until the installer is signed with a code-signing certificate, Windows may show a SmartScreen warning ("Windows protected your PC"). You can click **More info → Run anyway** to proceed. Signing requires purchasing an OV or EV certificate from a certificate authority such as DigiCert or Sectigo.

---

## License

MIT — see [LICENSE](LICENSE). Includes original work by Robert Phipps (MIT assumed, as no license was specified in the original repo).

---

## Original (Multicopy/)

The original .NET 4.6.1 WinForms project is preserved in `Multicopy/` for reference.
