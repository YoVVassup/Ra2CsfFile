# CsfStudio - Red Alert 2 String Table Editor

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
CsfStudio.exe -i stringtable01.llf,stringtable02.json -o stringtable03.yaml --merge

# Subtract labels
CsfStudio.exe -i stringtable01.llf,stringtable02.llf -o stringtable03.llf --subtract  

#Fix Encoding
CsfStudio.exe -i stringtable01.csf -o stringtable02.csf --fix-encoding windows-1251

```
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

## License
MIT License - see [LICENSE](https://github.com/YoVVassup/Ra2CsfFile/blob/cli/LICENSE) file for details

## Acknowledgments
- Westwood Studios for creating Command & Conquer: Red Alert 2
- The modding community for their documentation of the CSF format
- Contributors to the open-source libraries used in this project

---
<b>CsfStudio</b> - Your powerful tool for Red Alert 2 string table manipulation!