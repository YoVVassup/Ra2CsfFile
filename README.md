# SadPencil.Ra2CsfFile

[![C#](https://img.shields.io/badge/C%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.0-512BD4.svg?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Windows](https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://www.microsoft.com/windows/)
[![NuGet](https://img.shields.io/badge/NuGet-Package-004880?style=for-the-badge&logo=nuget&logoColor=white)](https://www.nuget.org/packages/SadPencil.Ra2CsfFile/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](https://opensource.org/licenses/MIT)

---

### Dependencies

[![Newtonsoft.Json](https://img.shields.io/badge/Newtonsoft.Json-13.0.3-3553A5?style=flat)](https://www.newtonsoft.com/json)
[![YamlDotNet](https://img.shields.io/badge/YamlDotNet-13.7.1-E0115F?style=flat)](https://github.com/aaubry/YamlDotNet)
[![NPOI](https://img.shields.io/badge/NPOI-2.5.1-217346?style=flat&logo=microsoftexcel&logoColor=white)](https://github.com/nissl-lab/npoi)
[![ini-parser](https://img.shields.io/badge/ini--parser--netstandard-2.5.2-4CAF50?style=flat)](https://github.com/paillavel/iniparser-netstandard)

**SadPencil.Ra2CsfFile** is a C# library for reading, editing, and writing **Red Alert 2** and **Yuri's Revenge** string table files (`.csf`). It supports **bidirectional conversion** between multiple formats and **map label checking**.

---

## Features

- 🔄 **Format conversion** – CSF ↔ INI / JSON / YAML / LLF / TXT / Excel (XLSX/XLS) / CSV
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

### CsfFile Convenience Wrappers

```csharp
// Load from any supported format
CsfFile csf = CsfFile.LoadFromCsfFile(stream);
CsfFile csf = CsfFile.LoadFromIniFile(stream);          // Obsolete, use CsfFileIniHelper
CsfFile csf = CsfFile.LoadFromLlfFile(stream);
CsfFile csf = CsfFile.LoadFromTxtFile(stream);
CsfFile csf = CsfFile.LoadFromExcelFile(stream);
CsfFile csf = CsfFile.LoadFromCsvFile(stream);
CsfFile csf = CsfFile.LoadFromCsvFile(stream, delimiter, encoding);

// Save to any supported format
csf.WriteCsfFile(stream);
csf.WriteIniFile(stream);                               // Obsolete, use CsfFileIniHelper
csf.WriteLlfFile(stream, fileName);
csf.WriteTxtFile(stream);
csf.WriteExcelFile(stream, xlsx);
csf.WriteCsvFile(stream);
csf.WriteCsvFile(stream, delimiter, encoding);

// JSON/YAML require using the helper classes directly
CsfFileJsonHelper.LoadFromJsonFile(stream);
CsfFileJsonHelper.WriteJsonFile(csf, stream);
CsfFileYamlHelper.LoadFromYamlFile(stream);
CsfFileYamlHelper.WriteYamlFile(csf, stream);
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

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Newtonsoft.Json | 13.0.3 | JSON format support |
| YamlDotNet | 13.7.1 | YAML format support |
| NPOI | 2.5.1 | Excel format support (XLSX/XLS) |
| ini-parser-netstandard | 2.5.2 | INI format and map file parsing |

---

## Version History

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
