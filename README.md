# CsfStudio - Red Alert 2 String Table Editor

[![C#](https://img.shields.io/badge/C%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.0-512BD4.svg?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Windows](https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)](https://www.microsoft.com/windows/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](https://opensource.org/licenses/MIT)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/YoVVassup/Ra2CsfFile)

---

### Dependencies

[![Costura.Fody](https://img.shields.io/badge/Costura.Fody-4.1.0-9B59B6?style=flat)](https://github.com/Fody/Costura)
[![Newtonsoft.Json](https://img.shields.io/badge/Newtonsoft.Json-13.0.3-3553A5?style=flat)](https://www.newtonsoft.com/json)
[![YamlDotNet](https://img.shields.io/badge/YamlDotNet-13.7.1-E0115F?style=flat)](https://github.com/aaubry/YamlDotNet)
[![NPOI](https://img.shields.io/badge/NPOI-2.5.1-217346?style=flat&logo=microsoftexcel&logoColor=white)](https://github.com/nissl-lab/npoi)
[![SharpZipLib](https://img.shields.io/badge/SharpZipLib-0.86.0-1E90FF?style=flat)](https://github.com/icsharpcode/SharpZipLib)
[![BouncyCastle](https://img.shields.io/badge/BouncyCastle-1.8.6-333333?style=flat)](https://github.com/bcgit/bc-csharp)
[![MSTest](https://img.shields.io/badge/Tests-MSTest-512BD4?style=flat&logo=visualstudio&logoColor=white)](https://docs.microsoft.com/en-us/visualstudio/test/)

**CsfStudio** is a command‑line tool for working with **Red Alert 2** and **Yuri's Revenge** string table files (`.csf`). It supports **bidirectional conversion** between multiple formats, **set operations** on label collections, **map label checking**, **translation helpers**, and **encoding fixes**.

---

## Features

- 🔄 **Format conversion** – CSF ↔ INI / JSON / YAML / LLF / TXT / Excel (XLSX/XLS) / CSV
- 🧩 **Set operations** – union, subtraction, intersection, symmetric difference, case override
- 🔍 **Diff & compare** – full diff and short stat between two CSF files
- 📋 **Search & export** – find labels by pattern, export label lists
- 🗺️ **Map label check** – scan `.map`, `.mpr`, `.yrm` files to find missing labels in CSF
- 🌍 **Translation helpers** – generate translation templates, side-by-side comparisons, update helpers
- 🔠 **Encoding fix** – reinterpret CSF text using a different codepage (e.g. Windows-1251, GB18030)
- 📦 **Batch processing** – process all files in a folder at once
- 🧠 **Preserves extra data (WRTS)** – all operations keep the optional binary block
- 📝 **Label ordering** – maintain original order or sort alphabetically (`--order-by-key`)
- 👁️ **Dry-run mode** – preview output without writing files

---

## Supported Formats

| Format | Read | Write | Extra data | Metadata | Multi‑line |
|--------|------|-------|------------|----------|------------|
| CSF (binary) | ✅ | ✅ | ✅ (WRTS) | ✅ (version, language) | ✅ |
| INI | ✅ | ✅ | ✅ (text) | ✅ | ✅ |
| JSON | ✅ | ✅ | ✅ (text) | ✅ | ✅ |
| YAML | ✅ | ✅ | ✅ (text) | ✅ | ✅ (literal) |
| LLF | ✅ | ✅ | ✅ (comment) | ✅ (comments) | ✅ |
| TXT (CSFTool) | ✅ | ✅ | ✅ (`!extra\|`) | ✅ (`!metadata\|`) | ✅ (escaped) |
| Excel (XLSX/XLS) | ✅ | ✅ | ✅ (text) | ✅ (separate sheet) | ✅ |
| CSV | ✅ | ✅ | ✅ (text) | ✅ (`#version=`) | ✅ (RFC 4180) |

> All conversions preserve **label order** (original or sorted) and **extra data** (WRTS) where applicable.

---

## Installation & Build

### Prerequisites
- .NET Framework 4.0 or higher
- Visual Studio 2019 / 2022 (or any C# compiler)

### Build from source
```bash
git clone https://github.com/YoVVassup/Ra2CsfFile.git
cd Ra2CsfFile
nuget restore SadPencil.Ra2CsfFile.sln
msbuild SadPencil.Ra2CsfFile.sln /p:Configuration=Release
```

The executable `CsfStudio.exe` will be placed in `CsfStudio\bin\Release\`. All dependencies (NPOI, YamlDotNet, Newtonsoft.Json, etc.) are embedded using **Costura.Fody** – no extra DLLs required.

---

## Quick Start

```bash
# View file statistics
CsfStudio.exe -i stringtable01.csf --stats

# Convert CSF to INI
CsfStudio.exe -i stringtable01.csf -o stringtable01.ini --to-ini

# Compare two files
CsfStudio.exe -i old.csf,new.csf --diff

# Search for labels
CsfStudio.exe -i stringtable01.csf --search "Soviet"

# Validate a file
CsfStudio.exe -i stringtable01.csf --validate
```

---

## Command Line Reference

### Basic syntax
```text
CsfStudio.exe -i <input> [-i <input2> ...] -o <output> --<operation> [options]
```

---

### Format Conversion

Convert a single file from one format to another.

| Command | Output format |
|---------|---------------|
| `--to-csf` | `.csf` (binary game format) |
| `--to-ini` | `.ini` (human-readable) |
| `--to-json` | `.json` |
| `--to-yaml` | `.yaml` |
| `--to-llf` | `.llf` (Label Language File) |
| `--to-txt` | `.txt` (CSFTool format) |
| `--to-excel` | `.xlsx` or `.xls` |
| `--to-csv` | `.csv` |

```bash
CsfStudio.exe -i stringtable01.csf -o stringtable01.ini --to-ini
CsfStudio.exe -i stringtable01.ini -o stringtable01.xlsx --to-excel
CsfStudio.exe -i stringtable01.csv -o stringtable01.csf --to-csf --csv-delimiter semicolon
CsfStudio.exe -i stringtable01.csf -o output.json --to-json --dry-run
```

---

### Set Operations

Operations that combine or compare two or more input files.

| Command | Description |
|---------|-------------|
| `--merge` | Union: all labels from all files |
| `--subtract` | A minus B: labels in first but not in others |
| `--intersection` | Labels present in all files; differing values replaced with `--diff-placeholder` |
| `--symmetric-difference` | Labels present in exactly one file (values must match) |
| `--override-case` | Keep values from current file, but use label case from upstream file |

```bash
CsfStudio.exe -i stringtable01.csf,stringtable02.csf -o merged.csf --merge
CsfStudio.exe -i stringtable01.csf,stringtable02.csf -o common.csf --intersection
CsfStudio.exe -i upstream.csf,current.csf -o fixed.csf --override-case
CsfStudio.exe -i old.csf,new.csf -o merged.csf --merge --merge-strategy last-wins
```

---

### Diff & Compare

| Command | Description |
|---------|-------------|
| `--diff` | Full diff: show all added, removed, and changed labels |
| `--diff-stat` | Short summary: counts of added/removed/changed/unchanged |

```bash
CsfStudio.exe -i old.csf,new.csf --diff
CsfStudio.exe -i old.csf,new.csf -o diff_report.txt --diff
CsfStudio.exe -i old.csf,new.csf --diff-stat
```

---

### Search & Export

| Command | Description |
|---------|-------------|
| `--search <pattern>` | Find labels by substring (case-insensitive) |
| `--search regex:<pattern>` | Find labels by regular expression |
| `--export-labels` | Export sorted list of all label names |

```bash
CsfStudio.exe -i stringtable01.csf --search "Soviet"
CsfStudio.exe -i stringtable01.csf --search "regex:^UI:" -o matches.txt
CsfStudio.exe -i stringtable01.csf --export-labels -o labels.txt
```

---

### Info & Validation

| Command | Description |
|---------|-------------|
| `--stats` | Show file statistics (count, size, language, version) |
| `--validate` | Validate file for errors (exit code: 0=ok, 1=errors, 2=warnings) |

```bash
CsfStudio.exe -i stringtable01.csf --stats
CsfStudio.exe -i stringtable01.csf --validate
```

---

### Map Label Check

Scan map files (`.map`, `.mpr`, `.yrm`) and list all labels used in maps but missing from the CSF.

```bash
CsfStudio.exe -i ra2md.csf --check-maps --map-folder "C:\RA2\Maps"
CsfStudio.exe -i ra2md.csf --check-maps --map-folder "C:\RA2\Maps" -o missing_labels.txt
```

---

### Translation Helpers

| Command | Description | Inputs |
|---------|-------------|--------|
| `--translation-new` | Create translation template | 1 (upstream) |
| `--translation-tile` | Side-by-side comparison | 2 (upstream, translated) |
| `--translation-update` | Update translation after upstream changes | 3 (old_upstream, new_upstream, old_translated) |
| `--translation-override` | Merge: use translated if exists, otherwise upstream | 2 (upstream, translated) |

```bash
CsfStudio.exe -i upstream.csf -o template.ini --translation-new
CsfStudio.exe -i upstream.csf,translated.csf -o compare.ini --translation-tile
CsfStudio.exe -i old_up.csf,new_up.csf,old_trans.csf -o updated.ini --translation-update
CsfStudio.exe -i upstream.csf,translated.csf -o merged.csf --translation-override
```

---

### Encoding Fix

Reinterpret CSF text using a different codepage.

```bash
CsfStudio.exe -i broken.csf -o fixed.csf --fix-encoding windows-1251
CsfStudio.exe -i broken.csf -o fixed.csf --fix-encoding gb18030
```

Supported encodings: `gb18030`, `gb2312`, `windows-1251`, `windows-1252`, `iso-8859-1`, `utf-8`, `unicode`

---

### Batch Processing

Process all supported files in a folder at once.

```bash
CsfStudio.exe --batch --batch-folder "C:\RA2\Strings" --to-json
CsfStudio.exe --batch --batch-folder "C:\RA2\Strings" --output-folder "C:\Output" --to-ini
CsfStudio.exe --batch --batch-folder "C:\RA2\Strings" --to-csv --recursive
```

---

### General Options

| Option | Description |
|--------|-------------|
| `-i, --input <file>` | Input file(s), comma-separated for multiple |
| `-o, --output <file>` | Output file path |
| `--csv-delimiter <delim>` | CSV delimiter: `auto`, `comma`, `semicolon`, `tab`, `pipe`, `space` |
| `--order-by-key` | Sort labels alphabetically when saving |
| `--diff-placeholder <text>` | Placeholder for differing values (default: `TODO_Different_Value`) |
| `--merge-strategy <strategy>` | Conflict resolution: `first-wins` (default), `last-wins`, `error` |
| `--dry-run` | Preview output without writing files |
| `--force` | Overwrite existing output file without prompt |
| `--output-encoding <enc>` | Output encoding: `utf-8` (default), `ascii`, `unicode` |
| `--quiet` | Suppress non-essential output |
| `--verbose` | Show additional information |
| `--recursive` | Include subfolders in batch processing |
| `-h, --help` | Show help |

---

## Notes

- Extra data (WRTS) is **preserved** in all operations.
- Extra data is stored as UTF-8 text in all text formats.
- `--translation-tile` supports `.ini`, `.json`, `.yaml`, `.csv`, `.xlsx` output.
- When using `--translation-update`, removed labels get a `_DELETE` suffix.
- Map label check parses `UIName`, `Actions` (types 11/103), and `Ranking` section.

---

## License

MIT License – see [LICENSE](https://github.com/YoVVassup/Ra2CsfFile/blob/main/LICENSE) for details.

### Acknowledgements

- **TXT format** (CSFTool format) is based on [CSFTool](https://github.com/Starkku/CSFTool) by Starkku, licensed under [GPL-3.0](https://github.com/Starkku/CSFTool/blob/master/LICENSE.txt).
