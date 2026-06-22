# SadPencil.Ra2CsfFile

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)  
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**SadPencil.Ra2CsfFile** is a C# library for reading, editing, and writing **Red Alert 2** and **Yuri's Revenge** string table files (`.csf`). It supports **bidirectional conversion** between multiple formats, **set operations** on label collections, and **map label checking**.

---

## Features

- 🔄 **Format conversion** – CSF ↔ INI / JSON / YAML / LLF / TXT / Excel (XLSX/XLS) / CSV
- 🧩 **Set operations** – union, subtraction, intersection, symmetric difference, case override
- 🔍 **Diff & compare** – compare two CSF files for differences
- 🗺️ **Map label check** – scan `.map`, `.mpr`, `.yrm` files to find missing labels in CSF
- 🧠 **Preserves extra data (WRTS)** – all operations keep the optional binary block
- 📝 **Label ordering** – maintain original order or sort alphabetically
- 🔤 **Case-insensitive** – label names are case-insensitive per CSF specification

---

## Supported Formats

| Format | Read | Write | Extra data | Metadata | Multi-line |
|--------|------|-------|------------|----------|------------|
| CSF (binary) | ✅ | ✅ | ✅ (WRTS) | ✅ (version, language) | ✅ |
| INI | ✅ | ✅ | ✅ (text) | ✅ | ✅ |
| JSON | ✅ | ✅ | ✅ (text) | ✅ | ✅ |
| YAML | ✅ | ✅ | ✅ (text) | ✅ | ✅ (literal) |
| LLF | ✅ | ✅ | ✅ (comment) | ✅ (comments) | ✅ |
| TXT (CSFTool) | ✅ | ✅ | ✅ (`!extra|`) | ✅ (`!metadata|`) | ✅ (escaped) |
| Excel (XLSX/XLS) | ✅ | ✅ | ✅ (text) | ✅ (separate sheet) | ✅ |
| CSV | ✅ | ✅ | ✅ (text) | ✅ (`#version=`) | ✅ (RFC 4180) |

---

## Installation

### NuGet
```bash
Install-Package SadPencil.Ra2CsfFile
```

### Build from source
```bash
git clone https://github.com/YoVVassup/Ra2CsfFile.git
cd Ra2CsfFile
nuget restore SadPencil.Ra2CsfFile.sln
msbuild SadPencil.Ra2CsfFile.sln /p:Configuration=Release
```

---

## Quick Start

```csharp
using SadPencil.Ra2CsfFile;

// Load a CSF file
CsfFile csf;
using (var fs = File.OpenRead("stringtable.csf"))
    csf = CsfFile.LoadFromCsfFile(fs);

// Access labels
foreach (var label in csf.Labels)
    Console.WriteLine($"{label.Key} = {label.Value}");

// Add a label
csf.AddLabel("GUI:MyButton", "Click me");

// Save to different formats
using (var fs = File.Create("output.ini"))
    CsfFileIniHelper.WriteIniFile(csf, fs);

using (var fs = File.Create("output.json"))
    CsfFileJsonHelper.WriteJsonFile(csf, fs);
```

---

## API Reference

### CsfFile (Core)

```csharp
// Load from CSF
CsfFile csf = CsfFile.LoadFromCsfFile(stream);
CsfFile csf = CsfFile.LoadFromCsfFile(stream, options);

// Save to CSF
csf.WriteCsfFile(stream);

// Label operations
csf.AddLabel("NAME:Value", "text");           // Returns true if existed
csf.AddLabel("NAME:Value", "text", extra);    // With extra data
csf.RemoveLabel("NAME:Value");                // Returns true if removed
csf.GetExtra("NAME:Value");                   // Returns byte[] or null
csf.SetExtra("NAME:Value", extraBytes);       // Set extra data
csf.HasExtra("NAME:Value");                   // Check if has extra

// Utilities
csf.Clone();                                  // Deep copy
csf.OrderByKey();                             // Returns sorted copy
csf.GetLabelsInWriteOrder();                  // Ordered enumerable
csf.ValidateLabelName("NAME:Value");          // Static validation
csf.ValidateLengths();                        // Check for oversized data
```

### CsfFileIniHelper

```csharp
CsfFile csf = CsfFileIniHelper.LoadFromIniFile(stream);
CsfFile csf = CsfFileIniHelper.LoadFromIniFile(stream, options);
CsfFileIniHelper.WriteIniFile(csf, stream);
```

### CsfFileJsonHelper

```csharp
CsfFile csf = CsfFileJsonHelper.LoadFromJsonFile(stream);
CsfFile csf = CsfFileJsonHelper.LoadFromJsonFile(stream, options);
CsfFileJsonHelper.WriteJsonFile(csf, stream);
```

### CsfFileYamlHelper

```csharp
CsfFile csf = CsfFileYamlHelper.LoadFromYamlFile(stream);
CsfFile csf = CsfFileYamlHelper.LoadFromYamlFile(stream, options);
CsfFileYamlHelper.WriteYamlFile(csf, stream);
```

### CsfFileCsvHelper

```csharp
CsfFile csf = CsfFileCsvHelper.LoadFromCsvFile(stream);
CsfFile csf = CsfFileCsvHelper.LoadFromCsvFile(stream, delimiter, encoding, options);
CsfFileCsvHelper.WriteCsvFile(csf, stream);
CsfFileCsvHelper.WriteCsvFile(csf, stream, delimiter, encoding);
```

### CsfFileTxtHelper

```csharp
CsfFile csf = CsfFileTxtHelper.LoadFromTxtFile(stream);
CsfFile csf = CsfFileTxtHelper.LoadFromTxtFile(stream, options);
CsfFileTxtHelper.WriteTxtFile(csf, stream);
```

### CsfFileLlfHelper

```csharp
CsfFile csf = CsfFileLlfHelper.LoadFromLlfFile(stream);
CsfFile csf = CsfFileLlfHelper.LoadFromLlfFile(stream, options);
CsfFileLlfHelper.WriteLlfFile(csf, stream, fileName);
```

### CsfFileExcelHelper

```csharp
CsfFile csf = CsfFileExcelHelper.LoadFromExcelFile(stream);
CsfFile csf = CsfFileExcelHelper.LoadFromExcelFile(stream, options);
CsfFileExcelHelper.WriteExcelFile(csf, stream, xlsx);  // xlsx=true for XLSX, false for XLS
```

### CsfFileMapHelper

```csharp
HashSet<string> labels = CsfFileMapHelper.ExtractLabelsFromMapFolder(mapFolder);
List<string> missing = CsfFileMapHelper.FindMissingLabels(csf, mapFolder);
```

### CsfFileOptions

```csharp
var options = new CsfFileOptions
{
    OrderByKey = true,                      // Sort labels alphabetically
    Encoding1252ReadWorkaround = true,      // Fix Windows-1252 chars on read
    Encoding1252WriteWorkaround = false,    // Convert back on write (not recommended)
    ApplyEncoding1252ToExtra = false        // Apply workaround to extra data
};
```

---

## Set Operations

```csharp
// Union (merge)
CsfFile result = MergeOperation(file1, file2, "first-wins");

// Subtraction
CsfFile result = SubtractOperation(file1, file2);

// Intersection
CsfFile result = IntersectionOperation(file1, file2, "TODO_Different");

// Symmetric difference
CsfFile result = SymmetricDifferenceOperation(file1, file2, "TODO_Different");

// Override case
CsfFile result = OverrideCaseOperation(upstream, current);
```

---

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Newtonsoft.Json | 13.0.3 | JSON format support |
| YamlDotNet | 13.7.1 | YAML format support |
| NPOI | 2.5.1 | Excel format support (XLSX/XLS) |
| ini-parser-netstandard | 2.5.2 | INI format and map file parsing |

---

## Version History

### v2.3.1
- Added `ExtraDataHelper` for unified extra data handling
- Removed Base64 mode, extra data always stored as UTF-8 text
- Added `ValidateLengths()` method for label/value size warnings
- Fixed CSV encoding for Excel compatibility (`Encoding.Default`)
- Fixed `CheckMapsOperation` error handling
- Added `InternalsVisibleTo` for unit tests
- Added XML doc comments to `ExtraDataHelper`
- 139 unit tests added (100% pass rate)

### v2.3.0
- Added Excel (XLSX/XLS) format support via NPOI
- Added CSV format support
- Added extra data (WRTS) support across all formats
- Added label ordering option (`OrderByKey`)
- Added CSFTool TXT format support

### v2.2.0
- Added YAML format support
- Added LLF format support

### v2.1.0
- Added JSON format support

### v2.0.0
- Initial release with CSF and INI format support

---

## Notes

- Extra data (WRTS) is **preserved** in all operations.
- Extra data is stored as UTF-8 text in all text formats.
- Label names are case-insensitive per CSF specification.
- The library targets .NET Framework 4.0 for Windows XP compatibility.

---

## License

MIT License – see [LICENSE](https://github.com/YoVVassup/Ra2CsfFile/blob/main/LICENSE) for details.

### Acknowledgements

- **TXT format** (CSFTool format) is based on [CSFTool](https://github.com/Starkku/CSFTool) by Starkku, licensed under [GPL-3.0](https://github.com/Starkku/CSFTool/blob/master/LICENSE.txt).
