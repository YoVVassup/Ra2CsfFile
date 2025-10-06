# CsfStudio - Red Alert 2 String Table Editor

<<<<<<< HEAD
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

CsfStudio is a powerful command-line tool for working with Red Alert 2 string table files (.csf). It supports conversion between multiple formats, merging of string tables, and subtraction operations.

## Features

- 🔄 **Bidirectional Conversion**: Convert between CSF, INI, JSON, YAML, and LLF formats  
- 🧩 **Merge Operations**: Combine multiple string tables into one  
- ✂️ **Subtract Operations**: Remove labels present in other files  
- 🔠 **Encoding Fix**: Correct text encoding issues in existing files  
- 📊 **Metadata Preservation**: Maintain version and language information  
- ✅ **Validation**: Strict label name and format validation
- ⚡ Fast processing with .NET Framework 4.0

## Supported Formats

### CSF Format (Command & Conquer String File)
- **Extension**: `.csf`  
- **Type**: Binary game format  
- **Structure**:  
```  
Offset  Type      Description  
0x00    char[4]   Header " FSC"  
0x04    int32     Version (typically 3)  
0x08    int32     Label count  
0x0C    int32     String count  
0x10    int32     Unused  
0x14    int32     Language ID  
...     ...       Label entries:  
char[4]   " LBL" header  
int32     Value count  
int32     Label name length  
char[]    Label name (ASCII)  
char[4]   " RTS" or "WRTS"  
int32     Value length  
byte[]    Value (UTF-16 NOT encoded)  
```

### INI Format
- **Extension**: `.ini`  
- **Type**: Text-based configuration  
- **Structure**:  
```ini  
[SadPencil.Ra2CsfFile.Ini]  
IniVersion = 2  
CsfVersion = 3  
CsfLang = 0

[LABEL_NAME]  
Value = Single line value  
ValueLine2 = Additional line  
```

### JSON Format
- **Extension**: `.json`  
- **Type**: Standard data interchange  
- **Structure**:  
```json  
{  
  "version": 3,  
  "language": 0,  
  "labels": {  
    "LABEL1": "Value 1",  
    "LABEL2": "Multi-line\nvalue"  
  }  
}  
```

### YAML Format
- **Extension**: `.yaml` or `.yml`  
- **Type**: Human-readable data serialization  
- **Structure**:  
```yaml  
version: 3  
language: 0 
version_yaml: 1.2 
labels:  
  LABEL1: Value 1  
  LABEL2: |  
    Multi-line  
    value  
```

### LLF Format (Label Language File)
- **Extension**: `.llf`  
- **Type**: Human-readable editing format  
- **Structure**:  
```
# filename
# version: 3  
# language: 0  
# csf count: 42  
# build time: 2025-08-01 14:30:00

KEY_SIMPLE: Simple value  
KEY_MULTILINE: >-  
    Multi-line  
    value  
```

## Encoding Fix Feature

Fix incorrectly interpreted text encoding with `--fix-encoding`:

```bash
CsfStudio.exe -i input.csf -o fixed.csf --fix-encoding windows-1251
```

**Supported Encodings**:  
- `gb18030` (Chinese)  
- `gb2312` (Chinese)
- `windows-1251` (Cyrillic)  
- `windows-1252` (Western European)  
- `iso-8859-1` (Latin-1)  
- `utf-8`  
- `unicode` (UTF-16)

## Command Line Usage

### Basic Syntax
```bash  
CsfStudio.exe -i input.ext -o output.ext --to-format  
CsfStudio.exe -i file1.ext,file2.ext -o result.ext --operation  
```

### Operations
| Command           | Description                      |  
|-------------------|----------------------------------| 
| `-i`, `--input`   | Input file path(s)               | 
| `-o`, `--output`  | Output file path                 | 
| `--to-ini`        | Convert to INI format            |  
| `--to-csf`        | Convert to CSF format            |  
| `--to-json`       | Convert to JSON format           |  
| `--to-yaml`       | Convert to YAML format           |  
| `--to-llf`        | Convert to LLF format            |  
| `--merge`         | Merge multiple files             |  
| `--subtract`      | Subtract labels from other files |  
| `--fix-encoding`  | Fix text encoding                |  
| `-h`, `--help`    | Show help                        | 

### Examples
```bash
# Convert CSF to LLF
CsfStudio.exe -i stringtable01.csf -o stringtable01.llf --to-llf

# Convert LLF to INI
CsfStudio.exe -i stringtable01.llf -o stringtable01.ini --to-ini

# Merge two files
CsfStudio.exe -i stringtable01.json,stringtable02.json -o stringtable03.json --merge

# Subtract labels
CsfStudio.exe -i stringtable01.llf,stringtable02.llf -o stringtable03.llf --subtract  

# Fix Encoding
CsfStudio.exe -i stringtable01.csf -o stringtable02.csf --fix-encoding windows-1251

```
### Attention

Operations of `--merge`, `--subtract` and `--fix-encoding` only work within the same format! (`.ext`)
 
## Format Comparison Matrix

| Feature               | CSF    | INI  | JSON | YAML   | LLF  |
|-----------------------|--------|------|----|--------|------|
| **Human-readable**    | ❌     | ✅    | 🟡 | ✅      | ✅    |
| **Metadata support**  | ✅     | ✅    | ✅  | ✅      | ✅    |
| **Multi-line values** | ✅     | ✅    | ✅  | ✅      | ✅    |
| **Language support**  | ✅     | ✅    | ✅  | ✅      | ✅    |
| **Version support**   | ✅     | ✅    | ✅  | ✅      | ✅    |
| **Edit complexity**   | High   | Medium | Low | Low    | Low  |
| **File size**         | Small  | Medium | Large | Medium | Medium |
| **Best use case**     | Game   | Tools | APIs | Config | Editing |

## Technical Specifications

1. **Character Encoding**:  
- CSF: UTF-16 with bitwise NOT encoding  
- Text formats: UTF-8  
- Special handling for Windows-1252 characters (128-159)

2. **Label Rules**:  
- ASCII characters 32-126 only  
- Case-insensitive (stored in lowercase)  
- No explicit length limit

3. **Multi-line Handling**:  
- All formats support multi-line values  
- Line breaks normalized to LF (`\n`)  
- LLF and YAML provide most natural representation

4. **Metadata Preservation**:  
- Version (default 3) and language preserved in all conversions  
- First file's metadata used in merge operations  
- Language IDs: 0=EnglishUS, 1=EnglishUK, ..., 9=Chinese, -1=Unknown

## Building from Source

### Prerequisites
- .NET Framework 4.0  
- Visual Studio 2019 or later

### Steps
1. Clone repository:  
```bash  
git clone https://github.com/YoVVassup/Ra2CsfFile.git  
```  
2. Open `SadPencil.Ra2CsfFile.sln` in Visual Studio or Rider (JetBrains) 
3. Restore NuGet packages  
4. Build solution
=======
## .NET Library
This is a .Net Framework v4.0 Library to load, edit, and save string table files (.csf) for Red Alert 2. Also, (de)serialize the string table from/to .ini, .json, .llf, .txt and .yaml (.yml) files.
>>>>>>> main

## License
MIT License - see [LICENSE](https://github.com/YoVVassup/Ra2CsfFile/blob/cli/LICENSE) file for details

<<<<<<< HEAD
## Acknowledgments
- Westwood Studios for creating Command & Conquer: Red Alert 2
- The modding community for their documentation of the CSF format
- Contributors to the open-source libraries used in this project
=======
MIT

## Notes
Reference: https://modenc.renegadeprojects.com/CSF_File_Format  
Easy to get .Net Target Framework SDK: Run [Get_.NET_Target_Framework.ps1](https://github.com/YoVVassup/Ra2CsfFile/blob/main/Get_.NET_Target_Framework.ps1)  
TXT File format from [CSFTool](https://github.com/Starkku/CSFTool) is licensed under [GPL Version 3](https://github.com/Starkku/CSFTool/blob/master/LICENSE.txt)

## Version History

```
v2.2.2: added TXT serialization support
v2.2.1: added LLF serialization support
v2.2.0: added JSON and YAML serialization support
v2.1.3: downgrade to compatibility .NET Framework 4.0
v2.1.2: disable Encoding1252WriteWorkaround by default; add CLSCompliant attribute to namespace SadPencil.Ra2CsfFile
v2.1.1: fix that some label names are not loaded successfully from .ini files
v2.1.0: api breaking change: change the behavior of Csf.AddLabel() with Add&Replace, so that the original ra2.csf file can be loaded
v2.0.2: remove the space around the "=" sign of ini file to fix a bug processing values contains " = " pattern
v2.0.1: fix a bug that CSF file with non-lowercase label name can not be loaded
v2.0.0: migrate to .NET Standard 2.0; replace dependency MadMilkman.Ini with ini-parser-netstandard; add Csf.RemoveLabel() method.
v1.3.1: api breaking change: Labels.Add will be replaced with AddLabel; add encoding 1252 workaround options for the original RA2 fonts; add clone constructor for CsfFile. 
v1.2.2: space in labels is now tolerated so that the library will not complain about the string table file in RA2.
v1.2.1: fix a bug where some labels of the ini file is not loaded.
v1.2.0: api breaking change: CsfFile.Labels will now store only one value for a label, as the rest values (if any) are not used by the game; api change: deprecate CsfFile.GetCsfLang() with CsfLangHelper.GetCsfLang(); api change: deprecate CsfFile.LoadFromIniFile() with CsfFileIniHelper.LoadFromIniFile(); api change: deprecate CsfFile.WriteIniFile() with CsfFileIniHelper.WriteIniFile().
v1.1.1: add XML documentation; re-release the library with Release configuration.
v1.1.0: fix a bug where multi-line text will be trimmed mistakenly; invalid chars in label name will now be checked.
```
>>>>>>> main

---
<b>CsfStudio</b> - Your powerful tool for Red Alert 2 string table manipulation!