# CsfStudio - Red Alert 2 String Table Editor

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

CsfStudio is a powerful command-line tool for working with Red Alert 2 string table files (.csf). It supports conversion between multiple formats, merging of string tables, and subtraction operations.

## Features

- 🔄 Bidirectional conversion between CSF, INI, JSON, YAML, and LLF formats  
- 🧩 Merge multiple string tables into one  
- ✂️ Subtract labels from one string table using another  
- 📊 Preserve metadata (version, language) across conversions  
- ✅ Strict validation of label names and formats  
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
    "LABEL2": "Multi-linenvalue"  
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
# Filename
#Version: 3  
#Language: 0  
#csf count: 42  
#build time: 2025-08-01 14:30:00

KEY_SIMPLE: Simple value  
KEY_MULTILINE: >-  
    Multi-line  
    value  
```

## Command Line Usage

### Basic Syntax
```bash  
CsfStudio.exe -i INPUT -o OUTPUT --to-FORMAT  
CsfStudio.exe -i FILE1,FILE2 -o RESULT --operation  
```

### Operations
| Command       | Description                                  |  
|---------------|----------------------------------------------|  
| `--to-ini`    | Convert to INI format                        |  
| `--to-csf`    | Convert to CSF format                        |  
| `--to-json`   | Convert to JSON format                       |  
| `--to-yaml`   | Convert to YAML format                       |  
| `--to-llf`    | Convert to LLF format                        |  
| `--merge`     | Merge multiple files                         |  
| `--subtract`  | Remove labels present in other files         |  

### Examples
```bash
# Convert CSF to LLF
CsfStudio.exe -i ra2.csf -o ra2.llf --to-llf

# Convert LLF to INI
CsfStudio.exe -i ra2.llf -o ra2.ini --to-ini

# Merge two files
CsfStudio.exe -i file1.llf,file2.json -o merged.yaml --merge

# Subtract labels
CsfStudio.exe -i main.llf,remove.llf -o result.llf --subtract  
```
## Format Comparison Matrix

| Feature                 | CSF    | INI    | JSON   | YAML   | LLF    |
|-------------------------|--------|--------|--------|--------|--------|
| **Human-readable** | ❌     | ✅     | 🟡     | ✅     | ✅     |
| **Metadata support**| ✅     | ✅     | ✅     | ✅     | ✅     |
| **Multi-line values** | ✅  | ✅     | ✅     | ✅     | ✅     |
| **Language support**| ✅     | ✅     | ✅     | ✅     | ✅     |
| **Version support** | ✅     | ✅     | ✅     | ✅     | ✅     |
| **Edit complexity** | High   | Medium | Low    | Low    | Low    |
| **File size**       | Small  | Medium | Large  | Medium | Medium |
| **Best use case**   | Game   | Tools  | APIs   | Config | Editing|

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

## License
MIT License - see [LICENSE](https://github.com/YoVVassup/Ra2CsfFile/blob/cli/LICENSE) file for details

## Acknowledgments
- Westwood Studios for creating Command & Conquer: Red Alert 2
- The modding community for their documentation of the CSF format
- Contributors to the open-source libraries used in this project

---
<b>CsfStudio</b> - Your powerful tool for Red Alert 2 string table manipulation!