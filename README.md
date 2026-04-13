# SadPencil.Ra2CsfFile

## .NET Library
This is a .NET Framework 4.0 library to load, edit, and save string table files (.csf) for **Red Alert 2** and **Yuri's Revenge**.  
It supports serialization to/from **.ini**, **.json**, **.yaml**, **.llf**, **.txt**, **.xlsx**, **.xls**, **.csv** and extraction of labels from map files (`.map`, `.mpr`, `.yrm`).  
Full support for **extra data (WRTS)**, **label ordering**, and **case‑insensitive** label names.

## Features
- ✅ Read/write binary CSF files (Westwood string tables)
- ✅ Convert to/from INI, JSON, YAML, LLF, TXT, Excel (XLSX/XLS), CSV
- ✅ Preserve extra data (WRTS blocks) – as plain text or Base64
- ✅ Maintain original label order or sort by key
- ✅ Multi‑line string support (LF line breaks)
- ✅ Windows‑1252 encoding workaround for original RA2 fonts
- ✅ Full metadata (version, language)
- ✅ Extract used labels from map files (`.map`, `.mpr`, `.yrm`)

## Usage Example

```csharp
using SadPencil.Ra2CsfFile;

// Load a CSF file
var csf = CsfFile.LoadFromCsfFile("ra2md.csf");

// Modify a label
csf.AddLabel("gui:hello", "Hello, Commander!");

// Add extra data (WRTS)
byte[] extraData = Encoding.UTF8.GetBytes("some extra info");
csf.SetExtra("gui:hello", extraData);

// Save as Excel (XLSX)
using (var fs = File.Create("output.xlsx"))
    csf.WriteExcelFile(fs);

// Save as CSV with semicolon delimiter
using (var fs = File.Create("output.csv"))
    csf.WriteCsvFile(fs, ";", Encoding.UTF8);

// Check missing labels in map files
var missing = CsfFileMapHelper.FindMissingLabels(csf, @"C:\maps");
foreach (var label in missing) Console.WriteLine(label);
```

## Helper Classes Overview

| Helper | Formats | Extra data | Metadata | Multi‑line |
|--------|---------|------------|----------|-------------|
| `CsfFileIniHelper` | `.ini` | ✅ (Base64/text) | ✅ | ✅ |
| `CsfFileJsonHelper` | `.json` | ✅ (Base64/text) | ✅ | ✅ |
| `CsfFileYamlHelper` | `.yaml`, `.yml` | ✅ (Base64/text) | ✅ | ✅ (literal style) |
| `CsfFileLlfHelper` | `.llf` | ✅ (comment `# extra:`) | ✅ (comments) | ✅ |
| `CsfFileTxtHelper` | `.txt` (CSFTool) | ✅ (`!extra\|`) | ✅ (`!metadata\|`) | ✅ (escaped) |
| `CsfFileExcelHelper` | `.xlsx`, `.xls` | ✅ (Base64/text) | ✅ (separate sheet) | ✅ |
| `CsfFileCsvHelper` | `.csv` | ✅ (Base64/text) | ✅ (`#version=`) | ✅ (RFC 4180) |
| `CsfFileMapHelper` | – | – | – | – (extracts labels from map INI) |

## Version History

```
v2.3.0: added Excel (XLSX/XLS), CSV support; full extra data (WRTS) support; label ordering;
        added CsfFileMapHelper for map label extraction; updated all helpers to preserve extra data.
v2.2.2: added TXT serialization support
v2.2.1: added LLF serialization support
v2.2.0: added JSON and YAML serialization support
v2.1.3: downgrade to .NET Framework 4.0 compatibility
v2.1.2: disable Encoding1252WriteWorkaround by default; add CLSCompliant attribute
v2.1.1: fix some label names not loading from .ini files
v2.1.0: api breaking change: Csf.AddLabel() now replaces existing label
v2.0.2: remove spaces around "=" in INI files
v2.0.1: fix loading CSF with non‑lowercase label names
v2.0.0: migrate to .NET Standard 2.0; replace MadMilkman.Ini with ini-parser-netstandard
...
```

## Dependencies
- `ini-parser-netstandard` (2.5.2)
- `Newtonsoft.Json` (13.0.3)
- `YamlDotNet` (13.7.1)
- `NPOI` (2.5.1) – for Excel support

## License
MIT