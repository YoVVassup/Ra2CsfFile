# CsfStudio - Red Alert 2 String Table Editor

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)  
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**CsfStudio** is a powerful command‑line tool for working with **Red Alert 2** and **Yuri's Revenge** string table files (`.csf`). It supports **bidirectional conversion** between multiple formats, **set operations** on label collections, **map label checking**, **translation helpers**, and **encoding fixes**.

---

## Features

- 🔄 **Format conversion** – CSF ↔ INI / JSON / YAML / LLF / TXT / Excel (XLSX/XLS) / CSV
- 🧩 **Set operations** – union, subtraction, intersection, symmetric difference, case override
- 🗺️ **Map label check** – scan `.map`, `.mpr`, `.yrm` files to find missing labels in CSF
- 🌍 **Translation helpers** – generate translation templates, side‑by‑side comparisons, update helpers
- 🔠 **Encoding fix** – reinterpret CSF text using a different codepage (e.g. Windows‑1251, GB18030)
- 🧠 **Preserves extra data (WRTS)** – all operations keep the optional binary block
- 📝 **Label ordering** – maintain original order or sort alphabetically (`--order-by-key`)
- 🧹 **CSV flexibility** – supports custom delimiters and `sep=` line for Excel compatibility

---

## Supported Formats

| Format | Read | Write | Extra data | Metadata | Multi‑line |
|--------|------|-------|------------|----------|-------------|
| CSF (binary) | ✅ | ✅ | ✅ (WRTS) | ✅ (version, language) | ✅ |
| INI | ✅ | ✅ | ✅ (Base64 or text) | ✅ | ✅ |
| JSON | ✅ | ✅ | ✅ (Base64 or text) | ✅ | ✅ |
| YAML | ✅ | ✅ | ✅ (Base64 or text) | ✅ | ✅ (literal) |
| LLF | ✅ | ✅ | ✅ (comment) | ✅ (comments) | ✅ |
| TXT (CSFTool) | ✅ | ✅ | ✅ (`!extra\|`) | ✅ (`!metadata\|`) | ✅ (escaped) |
| Excel (XLSX/XLS) | ✅ | ✅ | ✅ (text/Base64) | ✅ (separate sheet) | ✅ |
| CSV | ✅ | ✅ | ✅ (text/Base64) | ✅ (`#version=`) | ✅ (RFC 4180) |

> All conversions preserve **label order** (original or sorted) and **extra data** (WRTS) where applicable.

---

## Installation & Build

### Prerequisites
- .NET Framework 4.0 or higher
- Visual Studio 2019 / 2022 (or any C# compiler)

### Build from source
```bash
git clone https://github.com/YoVVassup/Ra2CsfFile.git
cd Ra2CsfFile/CsfStudio
nuget restore
msbuild /p:Configuration=Release
```

The executable `CsfStudio.exe` will be placed in `bin\Release\`. All dependencies (NPOI, YamlDotNet, Newtonsoft.Json, etc.) are embedded using **Costura.Fody** – no extra DLLs required.

---

## Command Line Usage

### Basic syntax
```text
CsfStudio.exe -i <input> [-i <input2> ...] -o <output> --<operation> [options]
```

### Global options
| Option | Description |
|--------|-------------|
| `-i, --input <file>` | Input file path (comma‑separated for multiple) |
| `-o, --output <file>` | Output file path |
| `--extra-mode text\|base64` | How to store extra data in text formats (default: `text`) |
| `--csv-delimiter <delim>` | CSV delimiter: `auto`, `comma`, `semicolon`, `tab`, `pipe`, `space` |
| `--order-by-key` | Sort labels alphabetically (case‑insensitive) when saving |
| `--diff-placeholder <text>` | Placeholder for differing values in intersection (default: `TODO_Different_Value`) |
| `-h, --help` | Show detailed help |

---

## Format Conversion

Convert a single file from one format to another. The output format is determined by the file extension of `-o`.

### Commands
- `--to-csf`   → save as `.csf`
- `--to-ini`   → save as `.ini`
- `--to-json`  → save as `.json`
- `--to-yaml`  → save as `.yaml`
- `--to-llf`   → save as `.llf`
- `--to-txt`   → save as `.txt` (CSFTool format)
- `--to-excel` → save as `.xlsx` or `.xls`
- `--to-csv`   → save as `.csv`

### Examples
```bash
# CSF → INI
CsfStudio.exe -i stringtable01.csf -o stringtable01.ini --to-ini

# INI → Excel
CsfStudio.exe -i stringtable01.ini -o stringtable01.xlsx --to-excel

# CSV → CSF (with semicolon delimiter)
CsfStudio.exe -i stringtable01.csv -o stringtable01.csf --to-csf --csv-delimiter semicolon
```

---

## Set Operations (two or more input files)

| Command | Description |
|---------|-------------|
| `--merge` | Union: all labels from all files |
| `--subtract` | A minus B: labels in first but not in others |
| `--intersection` | Labels present in **all** files; differing values replaced with `--diff-placeholder` |
| `--symmetric-difference` | Labels present in exactly **one** file (values must be consistent) |
| `--override-case` | Keep values from current file, but use label case from upstream file |

### Examples
```bash
# Union
CsfStudio.exe -i stringtable01.csf,stringtable02.csf -o stringtable_merged.csf --merge

# Intersection (with custom placeholder)
CsfStudio.exe -i stringtable01.csf,stringtable02.csf -o stringtable_common.csf --intersection --diff-placeholder "DIFFERENT"

# Override case
CsfStudio.exe -i stringtable_upstream.csf,stringtable_current.csf -o stringtable_fixed.csf --override-case
```

---

## Map Label Check

Scan map files (`.map`, `.mpr`, `.yrm`) and list all labels that are used in maps but missing from the given CSF.

```bash
CsfStudio.exe -i ra2md.csf --check-maps --map-folder "C:\RA2\Maps" -o missing_labels.txt
```

If `-o` is omitted, the missing labels are printed to the console.

---

## Translation Helpers

| Command | Description | Input count | Output |
|---------|-------------|-------------|--------|
| `--translation-new` | Create translation template (all values replaced with placeholder) | 1 (upstream) | any format |
| `--translation-tile` | Side‑by‑side comparison (UpstreamLine / TranslatedLine) | 2 (upstream, translated) | only `.ini` |
| `--translation-update` | Update translation after upstream changes | 3 (old_upstream, new_upstream, old_translated) | any format |
| `--translation-override` | Merge: use translated if exists, otherwise upstream | 2 (upstream, translated) | any format |

### Options
- `--translation-placeholder <text>` – placeholder for missing translations (default: `TODO_Translation_Needed`)
- `--translation-delete-placeholder <text>` – placeholder for labels removed in new upstream (default: `TODO_Translation_Delete_Needed`)

### Examples
```bash
# Create new translation template
CsfStudio.exe -i stringtable_upstream.csf -o stringtable_trans.ini --translation-new

# Side‑by‑side comparison (INI only)
CsfStudio.exe -i stringtable_upstream.csf,stringtable_translated.csf -o stringtable_compare.ini --translation-tile

# Update translation after upstream changes
CsfStudio.exe -i stringtable_old_up.csf,stringtable_new_up.csf,stringtable_old_trans.csf -o stringtable_update.ini --translation-update

# Override: prefer translated value
CsfStudio.exe -i stringtable_upstream.csf,stringtable_translated.csf -o stringtable_merged.csf --translation-override
```

---

## Encoding Fix

Reinterpret the string values of a CSF file using a different source encoding. Useful when a CSF was saved with a legacy codepage (e.g., Windows‑1251 for Cyrillic, GB18030 for Chinese).

```bash
CsfStudio.exe -i stringtable_broken.csf -o stringtable_fixed.csf --fix-encoding windows-1251
```

### Supported encodings
- `gb18030`, `gb2312` – Chinese
- `windows-1251` – Cyrillic
- `windows-1252` – Western European
- `iso-8859-1` – Latin‑1
- `utf-8`, `unicode` (UTF‑16)

---

## Notes

- Extra data (WRTS) is **preserved** in all operations.
- Use `--extra-mode base64` to store extra data as Base64 in text formats (INI, JSON, YAML, TXT, LLF).
- `--translation-tile` only supports `.ini` output because it adds custom keys (`UpstreamLineN`, `TranslatedLineN`).
- When using `--translation-update`, labels that were removed in the new upstream receive a `_DELETE` suffix.
- Map label check parses `UIName` in any section, `Actions` (action types 11/103 with parameter 4), and the `Ranking` section.

---

## License

MIT License – see [LICENSE](https://github.com/YoVVassup/Ra2CsfFile/blob/main/LICENSE) file for details.

---

**CsfStudio** – your complete tool for Red Alert 2 string table manipulation.