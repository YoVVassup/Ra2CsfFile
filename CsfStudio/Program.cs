using SadPencil.Ra2CsfFile;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;

namespace CsfStudio
{
    /// <summary>
    /// Main application class for CSF/INI/JSON/YAML/LLF/TXT/Excel/CSV conversion tool.
    /// Supports format conversions, set operations, map label checking, and translation helpers.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns>Exit code (0 for success).</returns>
        static int Main(string[] args)
        {
            try
            {
                var options = CommandLineOptions.Parse(args);
                if (options == null || options.ShowHelp)
                {
                    ShowHelp();
                    return 0;
                }

                var csfOptions = new CsfFileOptions
                {
                    OrderByKey = options.OrderByKey
                };

                if (!options.Force && !string.IsNullOrEmpty(options.OutputFile) && !options.DryRun && File.Exists(options.OutputFile))
                    throw new InvalidOperationException($"Output file already exists: {options.OutputFile}. Use --force to overwrite.");

                // Dispatch based on operation type
                if (options.Merge)
                    ProcessFiles(options.InputFiles, options.OutputFile,
                        (files) => MergeOperation(files, options.MergeStrategy),
                        csfOptions, options);
                else if (options.Subtract)
                    ProcessFiles(options.InputFiles, options.OutputFile, SubtractOperation, csfOptions, options);
                else if (options.Intersection)
                    ProcessFiles(options.InputFiles, options.OutputFile,
                        (files) => IntersectionOperation(files, options.DiffPlaceholder),
                        csfOptions, options);
                else if (options.SymmetricDifference)
                    ProcessFiles(options.InputFiles, options.OutputFile,
                        (files) => SymmetricDifferenceOperation(files, options.DiffPlaceholder),
                        csfOptions, options);
                else if (options.OverrideCase)
                    ProcessFiles(options.InputFiles, options.OutputFile, OverrideCaseOperation, csfOptions, options);
                else if (options.Diff)
                    DiffOperation(options.InputFiles[0], options.InputFiles[1], options.OutputFile, csfOptions, options);
                else if (options.Stats)
                    StatsOperation(options.InputFiles[0], csfOptions, options);
                else if (options.Validate)
                {
                    int validateResult = ValidateOperation(options.InputFiles[0], csfOptions, options);
                    return validateResult;
                }
                else if (options.Batch)
                    BatchOperation(options, csfOptions);
                else if (!string.IsNullOrEmpty(options.SearchPattern))
                    SearchOperation(options.InputFiles[0], options.SearchPattern, options.OutputFile, csfOptions, options);
                else if (options.ExportLabels)
                    ExportLabelsOperation(options.InputFiles[0], options.OutputFile, csfOptions, options);
                else if (options.DiffStat)
                    DiffStatOperation(options.InputFiles[0], options.InputFiles[1], csfOptions, options);
                else if (options.CheckMaps)
                    CheckMapsOperation(options.InputFiles[0], options.MapFolder, options.OutputFile, csfOptions, options);
                else if (options.TranslationNew)
                    TranslationNewOperation(options.InputFiles[0], options.OutputFile, options.TranslationPlaceholder, csfOptions, options);
                else if (options.TranslationTile)
                    TranslationTileOperation(options.InputFiles[0], options.InputFiles[1], options.OutputFile, options.TranslationPlaceholder, csfOptions, options);
                else if (options.TranslationUpdate)
                    TranslationUpdateOperation(options.InputFiles[0], options.InputFiles[1], options.InputFiles[2], options.OutputFile,
                        options.TranslationPlaceholder, csfOptions, options);
                else if (options.TranslationOverride)
                    TranslationOverrideOperation(options.InputFiles[0], options.InputFiles[1], options.OutputFile, csfOptions, options);
                else if (options.ToIni)
                    ConvertToIni(options, csfOptions);
                else if (options.ToCsf)
                    ConvertToCsf(options, csfOptions);
                else if (options.ToJson)
                    ConvertToJson(options, csfOptions);
                else if (options.ToYaml)
                    ConvertToYaml(options, csfOptions);
                else if (options.ToLlf)
                    ConvertToLlf(options, csfOptions);
                else if (options.ToTxt)
                    ConvertToTxt(options, csfOptions);
                else if (options.ToExcel)
                    ConvertToExcel(options, csfOptions);
                else if (options.ToCsv)
                    ConvertToCsv(options, csfOptions);
                else if (!string.IsNullOrEmpty(options.FixEncoding))
                    FixEncoding(options.InputFiles[0], options.OutputFile, options.GetEncoding(), csfOptions, options);
                else
                {
                    Console.WriteLine("Error: You must specify an operation");
                    return 1;
                }

                if (!options.Quiet)
                    Console.WriteLine("Operation completed successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        #region Conversion Helpers

        /// <summary>Converts input file to INI format.</summary>
        private static void ConvertToIni(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                if (options.DryRun)
                {
                    DryRunOutput(csf, options.OutputFile, "ini");
                    return;
                }
                using (var outputStream = File.Create(options.OutputFile))
                    CsfFileIniHelper.WriteIniFile(csf, outputStream);
            }
        }

        /// <summary>Converts input file to CSF format.</summary>
        private static void ConvertToCsf(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                if (options.DryRun)
                {
                    DryRunOutput(csf, options.OutputFile, "csf");
                    return;
                }
                using (var outputStream = File.Create(options.OutputFile))
                    csf.WriteCsfFile(outputStream);
            }
        }

        /// <summary>Converts input file to JSON format.</summary>
        private static void ConvertToJson(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                if (options.DryRun)
                {
                    DryRunOutput(csf, options.OutputFile, "json");
                    return;
                }
                using (var outputStream = File.Create(options.OutputFile))
                    CsfFileJsonHelper.WriteJsonFile(csf, outputStream);
            }
        }

        /// <summary>Converts input file to YAML format.</summary>
        private static void ConvertToYaml(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                if (options.DryRun)
                {
                    DryRunOutput(csf, options.OutputFile, "yaml");
                    return;
                }
                using (var outputStream = File.Create(options.OutputFile))
                    CsfFileYamlHelper.WriteYamlFile(csf, outputStream);
            }
        }

        /// <summary>Converts input file to LLF format.</summary>
        private static void ConvertToLlf(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                if (options.DryRun)
                {
                    DryRunOutput(csf, options.OutputFile, "llf");
                    return;
                }
                using (var outputStream = File.Create(options.OutputFile))
                {
                    string fileName = Path.GetFileNameWithoutExtension(options.OutputFile);
                    csf.WriteLlfFile(outputStream, fileName);
                }
            }
        }

        /// <summary>Converts input file to TXT (CSFTool) format.</summary>
        private static void ConvertToTxt(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                if (options.DryRun)
                {
                    DryRunOutput(csf, options.OutputFile, "txt");
                    return;
                }
                using (var outputStream = File.Create(options.OutputFile))
                    csf.WriteTxtFile(outputStream);
            }
        }

        /// <summary>Converts input file to Excel (XLSX/XLS) format.</summary>
        private static void ConvertToExcel(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                if (options.DryRun)
                {
                    DryRunOutput(csf, options.OutputFile, "xlsx");
                    return;
                }
                using (var outputStream = File.Create(options.OutputFile))
                {
                    bool xlsx = !options.OutputFile.ToLower().EndsWith(".xls");
                    csf.WriteExcelFile(outputStream, xlsx);
                }
            }
        }

        /// <summary>Converts input file to CSV format.</summary>
        private static void ConvertToCsv(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                if (options.DryRun)
                {
                    DryRunOutput(csf, options.OutputFile, "csv");
                    return;
                }
                using (var outputStream = File.Create(options.OutputFile))
                {
                    string delimiter = options.GetCsvDelimiter();
                    csf.WriteCsvFile(outputStream, delimiter, null);
                }
            }
        }
        #endregion

        #region Generic File Loading

        /// <summary>Loads a CSF file from a stream based on file extension.</summary>
        private static CsfFile LoadCsfFromStream(Stream stream, string extension, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            switch (extension)
            {
                case ".csf": return CsfFile.LoadFromCsfFile(stream, options);
                case ".ini": return CsfFileIniHelper.LoadFromIniFile(stream, options);
                case ".json": return CsfFileJsonHelper.LoadFromJsonFile(stream, options);
                case ".yaml":
                case ".yml": return CsfFileYamlHelper.LoadFromYamlFile(stream, options);
                case ".llf": return CsfFile.LoadFromLlfFile(stream, options);
                case ".txt": return CsfFile.LoadFromTxtFile(stream, options);
                case ".xlsx":
                case ".xls": return CsfFileExcelHelper.LoadFromExcelFile(stream, options);
                case ".csv": return CsfFileCsvHelper.LoadFromCsvFile(stream, cmdOptions.GetCsvDelimiter(), null, options);
                default: throw new NotSupportedException($"Unsupported file format: {extension}");
            }
        }
        #endregion

        #region Set Operations

        /// <summary>Processes multiple files with a set operation and saves the result.</summary>
        private static void ProcessFiles(List<string> inputPaths, string outputPath, Func<List<CsfFile>, CsfFile> operation, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            var firstExt = Path.GetExtension(inputPaths.First()).ToLower();
            if (inputPaths.Any(f => Path.GetExtension(f).ToLower() != firstExt))
                throw new InvalidOperationException("All files must be of the same type");

            var files = new List<CsfFile>();
            foreach (var inputPath in inputPaths)
            {
                using (var inputStream = File.OpenRead(inputPath))
                    files.Add(LoadCsfFromStream(inputStream, firstExt, options, cmdOptions));
            }

            var result = operation(files);

            if (cmdOptions.DryRun)
            {
                Console.WriteLine($"[DRY RUN] Would write to: {outputPath}");
                Console.WriteLine($"  Labels: {result.Labels.Count}");
                Console.WriteLine($"  Language: {result.Language}");
                Console.WriteLine($"  Version: {result.Version}");
                return;
            }

            SaveCsfToFile(result, outputPath, cmdOptions);
        }

        /// <summary>Saves a CsfFile to any supported output format based on file extension.</summary>
        private static void SaveCsfToFile(CsfFile csf, string outputPath, CommandLineOptions cmdOptions)
        {
            var warnings = csf.ValidateLengths();
            if (warnings.Count > 0 && !cmdOptions.Quiet)
            {
                Console.WriteLine("Warning: Data length issues detected:");
                foreach (var w in warnings)
                    Console.WriteLine($"  - {w}");
            }

            var outputExt = Path.GetExtension(outputPath).ToLower();
            using (var outputStream = File.Create(outputPath))
            {
                if (outputExt == ".csf")
                    csf.WriteCsfFile(outputStream);
                else if (outputExt == ".ini")
                    CsfFileIniHelper.WriteIniFile(csf, outputStream);
                else if (outputExt == ".json")
                    CsfFileJsonHelper.WriteJsonFile(csf, outputStream);
                else if (outputExt == ".yaml" || outputExt == ".yml")
                    CsfFileYamlHelper.WriteYamlFile(csf, outputStream);
                else if (outputExt == ".llf")
                    csf.WriteLlfFile(outputStream, Path.GetFileNameWithoutExtension(outputPath));
                else if (outputExt == ".txt")
                    csf.WriteTxtFile(outputStream);
                else if (outputExt == ".xlsx" || outputExt == ".xls")
                    csf.WriteExcelFile(outputStream, outputExt == ".xlsx");
                else if (outputExt == ".csv")
                    csf.WriteCsvFile(outputStream, cmdOptions.GetCsvDelimiter(), null);
                else
                    throw new InvalidOperationException($"Unsupported output format: {outputExt}");
            }
        }

        /// <summary>Merges multiple files (union). All labels from all files are included.</summary>
        private static CsfFile MergeOperation(List<CsfFile> files, string strategy)
        {
            var result = new CsfFile();
            foreach (var file in files)
                foreach (var label in file.Labels)
                {
                    if (result.Labels.ContainsKey(label.Key))
                    {
                        if (strategy == "error")
                            throw new InvalidOperationException($"Conflict on label '{label.Key}': already exists in result");
                        else if (strategy == "last-wins")
                            result.AddLabel(label.Key, label.Value, file.GetExtra(label.Key));
                        // first-wins: do nothing, keep existing value
                    }
                    else
                    {
                        result.AddLabel(label.Key, label.Value, file.GetExtra(label.Key));
                    }
                }
            if (result.Labels.Count > 0 && files.Count > 0)
            {
                result.Version = files[0].Version;
                result.Language = files[0].Language;
            }
            return result;
        }

        /// <summary>Subtracts labels present in other files (A minus B).</summary>
        private static CsfFile SubtractOperation(List<CsfFile> files)
        {
            if (files.Count < 2) throw new InvalidOperationException("Need at least 2 files");
            var result = new CsfFile(files[0]);
            var toRemove = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            for (int i = 1; i < files.Count; i++)
                foreach (var label in files[i].Labels.Keys)
                    toRemove.Add(label);
            foreach (var label in toRemove) result.RemoveLabel(label);
            return result;
        }

        /// <summary>Intersection: keeps labels that exist in ALL files. Values differing become placeholder.</summary>
        private static CsfFile IntersectionOperation(List<CsfFile> files, string diffPlaceholder)
        {
            if (files.Count < 2) throw new InvalidOperationException("Need at least 2 files");
            var result = new CsfFile();
            var commonKeys = new HashSet<string>(files[0].Labels.Keys, StringComparer.InvariantCultureIgnoreCase);
            for (int i = 1; i < files.Count; i++)
                commonKeys.IntersectWith(files[i].Labels.Keys);
            foreach (var key in commonKeys)
            {
                string firstValue = files[0].Labels[key];
                bool allEqual = true;
                for (int i = 1; i < files.Count; i++)
                    if (!string.Equals(firstValue, files[i].Labels[key], StringComparison.InvariantCulture))
                    { allEqual = false; break; }
                result.AddLabel(key, allEqual ? firstValue : diffPlaceholder, files[0].GetExtra(key));
            }
            if (result.Labels.Count > 0) { result.Version = files[0].Version; result.Language = files[0].Language; }
            return result;
        }

        /// <summary>Symmetric difference: labels that appear in exactly one file (with consistent values).</summary>
        private static CsfFile SymmetricDifferenceOperation(List<CsfFile> files, string diffPlaceholder)
        {
            if (files.Count < 2) throw new InvalidOperationException("Need at least 2 files");
            var count = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
            var firstValue = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            var firstExtra = new Dictionary<string, byte[]>(StringComparer.InvariantCultureIgnoreCase);
            var conflict = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var file in files)
                foreach (var kvp in file.Labels)
                {
                    if (!count.ContainsKey(kvp.Key))
                    {
                        count[kvp.Key] = 0;
                        firstValue[kvp.Key] = kvp.Value;
                        firstExtra[kvp.Key] = file.GetExtra(kvp.Key);
                    }
                    else if (!string.Equals(firstValue[kvp.Key], kvp.Value, StringComparison.InvariantCulture))
                        conflict.Add(kvp.Key);
                    count[kvp.Key]++;
                }
            var result = new CsfFile();
            foreach (var kvp in count)
                if (kvp.Value == 1 && !conflict.Contains(kvp.Key))
                    result.AddLabel(kvp.Key, firstValue[kvp.Key], firstExtra[kvp.Key]);
            if (result.Labels.Count > 0 && files.Count > 0)
            { result.Version = files[0].Version; result.Language = files[0].Language; }
            return result;
        }

        /// <summary>Overrides label case from upstream file (first input).</summary>
        private static CsfFile OverrideCaseOperation(List<CsfFile> files)
        {
            if (files.Count < 2) throw new InvalidOperationException("Need upstream file first, then current file");
            var upstream = files[0];
            var current = files[1];
            var result = new CsfFile();
            foreach (var label in current.Labels)
            {
                string upstreamKey = upstream.Labels.Keys.FirstOrDefault(k => string.Equals(k, label.Key, StringComparison.InvariantCultureIgnoreCase));
                string finalKey = string.IsNullOrEmpty(upstreamKey) ? label.Key : upstreamKey;
                result.AddLabel(finalKey, label.Value, current.GetExtra(label.Key));
            }
            result.Version = current.Version;
            result.Language = current.Language;
            return result;
        }
        #endregion

        #region Diff Operation

        /// <summary>Compares two CSF files and reports added, removed, and changed labels.</summary>
        private static void DiffOperation(string oldPath, string newPath, string outputFilePath, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            CsfFile oldCsf, newCsf;
            var oldExt = Path.GetExtension(oldPath).ToLower();
            var newExt = Path.GetExtension(newPath).ToLower();

            using (var fs = File.OpenRead(oldPath))
                oldCsf = LoadCsfFromStream(fs, oldExt, options, cmdOptions);
            using (var fs = File.OpenRead(newPath))
                newCsf = LoadCsfFromStream(fs, newExt, options, cmdOptions);

            var added = new List<string>();
            var removed = new List<string>();
            var changed = new List<Tuple<string, string, string>>();

            var allKeys = new HashSet<string>(oldCsf.Labels.Keys, StringComparer.InvariantCultureIgnoreCase);
            allKeys.UnionWith(newCsf.Labels.Keys);

            foreach (var key in allKeys)
            {
                bool inOld = oldCsf.Labels.TryGetValue(key, out string oldValue);
                bool inNew = newCsf.Labels.TryGetValue(key, out string newValue);

                if (!inOld && inNew)
                    added.Add(key);
                else if (inOld && !inNew)
                    removed.Add(key);
                else if (!string.Equals(oldValue, newValue, StringComparison.InvariantCulture))
                    changed.Add(Tuple.Create(key, oldValue, newValue));
            }

            added.Sort(StringComparer.InvariantCultureIgnoreCase);
            removed.Sort(StringComparer.InvariantCultureIgnoreCase);
            changed.Sort((a, b) => StringComparer.InvariantCultureIgnoreCase.Compare(a.Item1, b.Item1));

            Console.WriteLine($"Diff: {oldPath} vs {newPath}");
            Console.WriteLine($"  Added:      {added.Count}");
            Console.WriteLine($"  Removed:    {removed.Count}");
            Console.WriteLine($"  Changed:    {changed.Count}");
            Console.WriteLine();

            if (added.Count > 0)
            {
                Console.WriteLine("Added labels:");
                foreach (var label in added) Console.WriteLine($"  + {label}");
                Console.WriteLine();
            }

            if (removed.Count > 0)
            {
                Console.WriteLine("Removed labels:");
                foreach (var label in removed) Console.WriteLine($"  - {label}");
                Console.WriteLine();
            }

            if (changed.Count > 0)
            {
                Console.WriteLine("Changed labels:");
                foreach (var t in changed)
                {
                    Console.WriteLine($"  ~ {t.Item1}");
                    Console.WriteLine($"      old: {TruncateForDisplay(t.Item2)}");
                    Console.WriteLine($"      new: {TruncateForDisplay(t.Item3)}");
                }
                Console.WriteLine();
            }

            if (added.Count == 0 && removed.Count == 0 && changed.Count == 0)
                Console.WriteLine("No differences found.");

            if (!string.IsNullOrEmpty(outputFilePath))
            {
                using (var sw = new StreamWriter(outputFilePath, false, Encoding.UTF8))
                {
                    sw.WriteLine($"# Diff: {oldPath} vs {newPath}");
                    sw.WriteLine($"# Added: {added.Count}");
                    sw.WriteLine($"# Removed: {removed.Count}");
                    sw.WriteLine($"# Changed: {changed.Count}");
                    sw.WriteLine();

                    if (added.Count > 0)
                    {
                        sw.WriteLine("[Added]");
                        foreach (var label in added) sw.WriteLine($"+{label}");
                        sw.WriteLine();
                    }
                    if (removed.Count > 0)
                    {
                        sw.WriteLine("[Removed]");
                        foreach (var label in removed) sw.WriteLine($"-{label}");
                        sw.WriteLine();
                    }
                    if (changed.Count > 0)
                    {
                        sw.WriteLine("[Changed]");
                        foreach (var t in changed)
                        {
                            sw.WriteLine($"~{t.Item1}");
                            sw.WriteLine($"  old={t.Item2}");
                            sw.WriteLine($"  new={t.Item3}");
                        }
                    }
                }
                Console.WriteLine($"Diff written to: {outputFilePath}");
            }
        }

        private static string TruncateForDisplay(string text, int maxLength = 80)
        {
            if (string.IsNullOrEmpty(text)) return "(empty)";
            text = text.Replace("\n", "\\n");
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        private static void DryRunOutput(CsfFile csf, string outputPath, string format)
        {
            Console.WriteLine($"[DRY RUN] Would write {csf.Labels.Count} labels to: {outputPath} ({format})");
            Console.WriteLine($"  Language: {csf.Language}");
            Console.WriteLine($"  Version: {csf.Version}");
            int withExtra = csf.Labels.Keys.Count(k => csf.HasExtra(k));
            if (withExtra > 0)
                Console.WriteLine($"  Labels with extra data: {withExtra}");
        }

        #endregion

        #region Stats Operation

        /// <summary>Displays statistics about a CSF file.</summary>
        private static void StatsOperation(string inputPath, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            var ext = Path.GetExtension(inputPath).ToLower();
            CsfFile csf;
            using (var fs = File.OpenRead(inputPath))
                csf = LoadCsfFromStream(fs, ext, options, cmdOptions);

            int totalLabels = csf.Labels.Count;
            int withExtra = csf.Labels.Keys.Count(k => csf.HasExtra(k));
            int multiLine = csf.Labels.Values.Count(v => v.Contains("\n"));
            long fileSize = new System.IO.FileInfo(inputPath).Length;

            Console.WriteLine($"File: {inputPath}");
            Console.WriteLine($"  Format:       {ext.TrimStart('.')}");
            Console.WriteLine($"  Size:         {fileSize} bytes");
            Console.WriteLine($"  CSF Version:  {csf.Version}");
            Console.WriteLine($"  Language:     {csf.Language} ({(int)csf.Language})");
            Console.WriteLine($"  Labels:       {totalLabels}");
            Console.WriteLine($"  With extra:   {withExtra}");
            Console.WriteLine($"  Multi-line:   {multiLine}");
            Console.WriteLine($"  Single-line:  {totalLabels - multiLine}");
        }

        #endregion

        #region Validate Operation

        /// <summary>Validates a CSF file and reports errors. Returns 0=ok, 1=errors, 2=warnings.</summary>
        private static int ValidateOperation(string inputPath, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            var ext = Path.GetExtension(inputPath).ToLower();
            int errors = 0;
            int warnings = 0;

            Console.WriteLine($"Validating: {inputPath}");

            try
            {
                CsfFile csf;
                using (var fs = File.OpenRead(inputPath))
                    csf = LoadCsfFromStream(fs, ext, options, cmdOptions);

                foreach (var label in csf.Labels)
                {
                    if (string.IsNullOrEmpty(label.Key))
                    {
                        Console.WriteLine($"  ERROR: Empty label name");
                        errors++;
                    }
                    else if (!CsfFile.ValidateLabelName(label.Key))
                    {
                        Console.WriteLine($"  ERROR: Invalid label name: {label.Key}");
                        errors++;
                    }

                    if (label.Value == null)
                    {
                        Console.WriteLine($"  WARNING: Null value for label: {label.Key}");
                        warnings++;
                    }
                    else if (label.Value.Length == 0)
                    {
                        Console.WriteLine($"  WARNING: Empty value for label: {label.Key}");
                        warnings++;
                    }
                }

                if (csf.Labels.Count == 0)
                {
                    Console.WriteLine("  WARNING: File contains no labels");
                    warnings++;
                }

                if (errors == 0 && warnings == 0)
                    Console.WriteLine("  OK: No errors found");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: Failed to parse file: {ex.Message}");
                errors++;
            }

            Console.WriteLine();
            Console.WriteLine($"Result: {errors} error(s), {warnings} warning(s)");

            if (errors > 0) return 1;
            if (warnings > 0) return 2;
            return 0;
        }

        #endregion

        #region Batch Operation

        /// <summary>Processes all CSF files in a folder with the specified conversion operation.</summary>
        private static void BatchOperation(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            string batchFolder = options.BatchFolder;
            if (!Directory.Exists(batchFolder))
                throw new DirectoryNotFoundException($"Batch folder does not exist: {batchFolder}");

            var extensions = new[] { ".csf", ".ini", ".json", ".yaml", ".yml", ".llf", ".txt", ".xlsx", ".xls", ".csv" };
            var searchOption = options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(batchFolder, "*.*", searchOption)
                .Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (files.Count == 0)
            {
                Console.WriteLine("No supported files found in batch folder.");
                return;
            }

            string outputFolder = options.OutputFolder;
            if (string.IsNullOrEmpty(outputFolder))
                outputFolder = batchFolder;
            else if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            string outputExt = GetOutputExtension(options);
            int success = 0;
            int failed = 0;

            foreach (var file in files)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(file) + outputExt;
                    string outputPath = Path.Combine(outputFolder, fileName);

                    var inputExt = Path.GetExtension(file).ToLower();
                    using (var inputStream = File.OpenRead(file))
                    using (var outputStream = File.Create(outputPath))
                    {
                        CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                        WriteCsfToStream(csf, outputStream, outputExt, options);
                    }

                    Console.WriteLine($"  OK: {Path.GetFileName(file)} -> {fileName}");
                    success++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  FAIL: {Path.GetFileName(file)} - {ex.Message}");
                    failed++;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Batch complete: {success} succeeded, {failed} failed");
        }

        private static string GetOutputExtension(CommandLineOptions options)
        {
            if (options.ToIni) return ".ini";
            if (options.ToCsf) return ".csf";
            if (options.ToJson) return ".json";
            if (options.ToYaml) return ".yaml";
            if (options.ToLlf) return ".llf";
            if (options.ToTxt) return ".txt";
            if (options.ToExcel) return ".xlsx";
            if (options.ToCsv) return ".csv";
            throw new InvalidOperationException("No output format specified");
        }

        private static void WriteCsfToStream(CsfFile csf, Stream stream, string ext, CommandLineOptions options)
        {
            switch (ext)
            {
                case ".csf": csf.WriteCsfFile(stream); break;
                case ".ini": CsfFileIniHelper.WriteIniFile(csf, stream); break;
                case ".json": CsfFileJsonHelper.WriteJsonFile(csf, stream); break;
                case ".yaml": CsfFileYamlHelper.WriteYamlFile(csf, stream); break;
                case ".llf": csf.WriteLlfFile(stream, "converted"); break;
                case ".txt": csf.WriteTxtFile(stream); break;
                case ".xlsx": csf.WriteExcelFile(stream, true); break;
                case ".csv": csf.WriteCsvFile(stream, options.GetCsvDelimiter(), null); break;
                default: throw new NotSupportedException($"Unsupported output format: {ext}");
            }
        }

        #endregion

        #region Search Operation

        /// <summary>Searches for labels matching a pattern (substring or regex).</summary>
        private static void SearchOperation(string inputPath, string pattern, string outputFilePath, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            var ext = Path.GetExtension(inputPath).ToLower();
            CsfFile csf;
            using (var fs = File.OpenRead(inputPath))
                csf = LoadCsfFromStream(fs, ext, options, cmdOptions);

            bool isRegex = pattern.StartsWith("regex:");
            string searchPattern = isRegex ? pattern.Substring(6) : pattern;
            var matches = new List<Tuple<string, string>>();

            foreach (var label in csf.Labels)
            {
                bool found;
                if (isRegex)
                    found = System.Text.RegularExpressions.Regex.IsMatch(label.Key, searchPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                else
                    found = label.Key.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase) >= 0;

                if (found)
                    matches.Add(Tuple.Create(label.Key, label.Value));
            }

            matches.Sort((a, b) => StringComparer.InvariantCultureIgnoreCase.Compare(a.Item1, b.Item1));

            Console.WriteLine($"Search: '{pattern}' in {inputPath}");
            Console.WriteLine($"Matches: {matches.Count}");
            Console.WriteLine();

            foreach (var m in matches)
                Console.WriteLine($"  {m.Item1} = {TruncateForDisplay(m.Item2)}");

            if (!string.IsNullOrEmpty(outputFilePath))
            {
                using (var sw = new StreamWriter(outputFilePath, false, Encoding.UTF8))
                {
                    sw.WriteLine($"# Search: '{pattern}' in {inputPath}");
                    sw.WriteLine($"# Matches: {matches.Count}");
                    sw.WriteLine();
                    foreach (var m in matches)
                        sw.WriteLine($"{m.Item1}={m.Item2}");
                }
                Console.WriteLine();
                Console.WriteLine($"Results written to: {outputFilePath}");
            }
        }

        #endregion

        #region Export Labels Operation

        /// <summary>Exports the list of label names from a CSF file.</summary>
        private static void ExportLabelsOperation(string inputPath, string outputFilePath, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            var ext = Path.GetExtension(inputPath).ToLower();
            CsfFile csf;
            using (var fs = File.OpenRead(inputPath))
                csf = LoadCsfFromStream(fs, ext, options, cmdOptions);

            var labels = csf.Labels.Keys.OrderBy(k => k, StringComparer.InvariantCultureIgnoreCase).ToList();

            Console.WriteLine($"Labels in {inputPath}: {labels.Count}");

            if (!string.IsNullOrEmpty(outputFilePath))
            {
                using (var sw = new StreamWriter(outputFilePath, false, Encoding.UTF8))
                    foreach (var label in labels)
                        sw.WriteLine(label);
                Console.WriteLine($"Written to: {outputFilePath}");
            }
            else
            {
                foreach (var label in labels)
                    Console.WriteLine(label);
            }
        }

        #endregion

        #region Diff Stat Operation

        /// <summary>Shows a short summary of differences between two CSF files.</summary>
        private static void DiffStatOperation(string oldPath, string newPath, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            CsfFile oldCsf, newCsf;
            var oldExt = Path.GetExtension(oldPath).ToLower();
            var newExt = Path.GetExtension(newPath).ToLower();

            using (var fs = File.OpenRead(oldPath))
                oldCsf = LoadCsfFromStream(fs, oldExt, options, cmdOptions);
            using (var fs = File.OpenRead(newPath))
                newCsf = LoadCsfFromStream(fs, newExt, options, cmdOptions);

            int added = 0, removed = 0, changed = 0;

            var allKeys = new HashSet<string>(oldCsf.Labels.Keys, StringComparer.InvariantCultureIgnoreCase);
            allKeys.UnionWith(newCsf.Labels.Keys);

            foreach (var key in allKeys)
            {
                bool inOld = oldCsf.Labels.TryGetValue(key, out string oldValue);
                bool inNew = newCsf.Labels.TryGetValue(key, out string newValue);

                if (!inOld && inNew) added++;
                else if (inOld && !inNew) removed++;
                else if (!string.Equals(oldValue, newValue, StringComparison.InvariantCulture)) changed++;
            }

            Console.WriteLine($"Diff stat: {oldPath} vs {newPath}");
            Console.WriteLine($"  Old: {oldCsf.Labels.Count} labels");
            Console.WriteLine($"  New: {newCsf.Labels.Count} labels");
            Console.WriteLine($"  + added:    {added}");
            Console.WriteLine($"  - removed:  {removed}");
            Console.WriteLine($"  ~ changed:  {changed}");
            Console.WriteLine($"  = unchanged: {allKeys.Count - added - removed - changed}");
        }

        #endregion

        #region Check Maps Operation (using CsfFileMapHelper)

        /// <summary>Checks map files for missing labels in the given CSF file.</summary>
        private static void CheckMapsOperation(string csfFilePath, string mapFolder, string outputFilePath, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            if (string.IsNullOrWhiteSpace(mapFolder))
                throw new ArgumentException("Map folder path is empty.");

            string absoluteMapFolder;
            if (!Path.IsPathRooted(mapFolder))
            {
                if (mapFolder == ".")
                    absoluteMapFolder = Directory.GetCurrentDirectory();
                else if (mapFolder == "..")
                    absoluteMapFolder = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
                else
                    absoluteMapFolder = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), mapFolder));
            }
            else
            {
                absoluteMapFolder = Path.GetFullPath(mapFolder);
            }

            absoluteMapFolder = absoluteMapFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (!Directory.Exists(absoluteMapFolder))
                throw new DirectoryNotFoundException($"Map folder does not exist: {absoluteMapFolder}");

            CsfFile csf;
            using (var fs = File.OpenRead(csfFilePath))
                csf = LoadCsfFromStream(fs, Path.GetExtension(csfFilePath).ToLower(), options, cmdOptions);

            var missing = CsfFileMapHelper.FindMissingLabels(csf, absoluteMapFolder);

            Console.WriteLine($"Missing labels in CSF: {missing.Count}");

            if (!string.IsNullOrEmpty(outputFilePath))
            {
                using (var sw = new StreamWriter(outputFilePath, false, Encoding.UTF8))
                {
                    sw.WriteLine($"# Missing labels in CSF: {csfFilePath}");
                    sw.WriteLine($"# Map folder: {absoluteMapFolder}");
                    sw.WriteLine($"# Missing: {missing.Count}");
                    sw.WriteLine();
                    foreach (var label in missing) sw.WriteLine(label);
                }
                Console.WriteLine($"Missing labels written to: {outputFilePath}");
            }
            else
            {
                Console.WriteLine("\nMissing labels:");
                foreach (var label in missing) Console.WriteLine($"  {label}");
            }
        }
        #endregion

        #region Translation Operations

        /// <summary>Creates a new translation template from upstream (all values replaced with placeholder).</summary>
        private static void TranslationNewOperation(string upstreamPath, string outputPath, string placeholder, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            CsfFile upstream;
            using (var fs = File.OpenRead(upstreamPath))
                upstream = LoadCsfFromStream(fs, Path.GetExtension(upstreamPath).ToLower(), options, cmdOptions);

            var result = new CsfFile();
            foreach (var label in upstream.Labels)
                result.AddLabel(label.Key, placeholder, upstream.GetExtra(label.Key));
            result.Version = upstream.Version;
            result.Language = upstream.Language;

            SaveCsfToFile(result, outputPath, cmdOptions);
            Console.WriteLine($"Translation template saved to {outputPath}");
        }

        /// <summary>Creates a tiled comparison with upstream and translated lines side by side.</summary>
        private static void TranslationTileOperation(string upstreamPath, string translatedPath, string outputPath, string placeholder, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            CsfFile upstream, translated;
            using (var fs = File.OpenRead(upstreamPath))
                upstream = LoadCsfFromStream(fs, Path.GetExtension(upstreamPath).ToLower(), options, cmdOptions);
            using (var fs = File.OpenRead(translatedPath))
                translated = LoadCsfFromStream(fs, Path.GetExtension(translatedPath).ToLower(), options, cmdOptions);

            var allKeys = new HashSet<string>(upstream.Labels.Keys, StringComparer.InvariantCultureIgnoreCase);
            allKeys.UnionWith(translated.Labels.Keys);
            var sortedKeys = allKeys.OrderBy(k => k, StringComparer.InvariantCultureIgnoreCase).ToList();

            var outputExt = Path.GetExtension(outputPath).ToLower();

            switch (outputExt)
            {
                case ".ini": WriteTileIni(upstream, translated, sortedKeys, outputPath, placeholder); break;
                case ".json": WriteTileJson(upstream, translated, sortedKeys, outputPath, placeholder); break;
                case ".yaml":
                case ".yml": WriteTileYaml(upstream, translated, sortedKeys, outputPath, placeholder); break;
                case ".csv": WriteTileCsv(upstream, translated, sortedKeys, outputPath, placeholder); break;
                case ".xlsx":
                case ".xls": WriteTileExcel(upstream, translated, sortedKeys, outputPath, placeholder); break;
                default:
                    throw new NotSupportedException($"--translation-tile supports only .ini, .json, .yaml, .csv, .xlsx output (got {outputExt})");
            }

            Console.WriteLine($"Tiled comparison saved to {outputPath}");
        }

        private static void WriteTileIni(CsfFile upstream, CsfFile translated, List<string> keys, string path, string placeholder)
        {
            using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                sw.WriteLine("[SadPencil.Ra2CsfFile.Ini]");
                sw.WriteLine("IniVersion=3");
                sw.WriteLine($"CsfVersion={upstream.Version}");
                sw.WriteLine($"CsfLang={(int)upstream.Language}");
                sw.WriteLine();

                foreach (var key in keys)
                {
                    sw.WriteLine($"[{key}]");

                    if (upstream.Labels.TryGetValue(key, out string upstreamValue))
                    {
                        var lines = upstreamValue.Split('\n');
                        for (int i = 0; i < lines.Length; i++)
                            sw.WriteLine($"UpstreamLine{(i == 0 ? "" : (i + 1).ToString())}={lines[i]}");
                    }

                    if (translated.Labels.TryGetValue(key, out string translatedValue))
                    {
                        var lines = translatedValue.Split('\n');
                        for (int i = 0; i < lines.Length; i++)
                            sw.WriteLine($"TranslatedLine{(i == 0 ? "" : (i + 1).ToString())}={lines[i]}");
                    }
                    else
                    {
                        sw.WriteLine($"TranslatedLine={placeholder}");
                    }

                    sw.WriteLine();
                }
            }
        }

        private static void WriteTileJson(CsfFile upstream, CsfFile translated, List<string> keys, string path, string placeholder)
        {
            var labelsDict = new Dictionary<string, object>();
            foreach (var key in keys)
            {
                upstream.Labels.TryGetValue(key, out string upVal);
                translated.Labels.TryGetValue(key, out string transVal);
                labelsDict[key] = new { upstream = upVal ?? "", translated = transVal ?? placeholder };
            }
            var output = new { upstream_version = upstream.Version, upstream_language = (int)upstream.Language, labels = labelsDict };
            using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
                sw.Write(JsonConvert.SerializeObject(output, Formatting.Indented));
        }

        private static void WriteTileYaml(CsfFile upstream, CsfFile translated, List<string> keys, string path, string placeholder)
        {
            var labelsDict = new Dictionary<string, object>();
            foreach (var key in keys)
            {
                upstream.Labels.TryGetValue(key, out string upVal);
                translated.Labels.TryGetValue(key, out string transVal);
                labelsDict[key] = new { upstream = upVal ?? "", translated = transVal ?? placeholder };
            }
            var output = new { upstream_version = upstream.Version, upstream_language = (int)upstream.Language, labels = labelsDict };
            var serializer = new SerializerBuilder().Build();
            using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
                sw.Write(serializer.Serialize(output));
        }

        private static void WriteTileCsv(CsfFile upstream, CsfFile translated, List<string> keys, string path, string placeholder)
        {
            using (var sw = new StreamWriter(path, false, new System.Text.UTF8Encoding(false)))
            {
                sw.WriteLine("Label,Upstream,Translated");
                foreach (var key in keys)
                {
                    upstream.Labels.TryGetValue(key, out string upVal);
                    translated.Labels.TryGetValue(key, out string transVal);
                    string escapedKey = key.Contains(",") || key.Contains("\"") ? $"\"{key.Replace("\"", "\"\"")}\"" : key;
                    string escapedUp = (upVal ?? "").Contains(",") || (upVal ?? "").Contains("\"") ? $"\"{(upVal ?? "").Replace("\"", "\"\"")}\"" : (upVal ?? "");
                    string escapedTrans = (transVal ?? placeholder).Contains(",") || (transVal ?? placeholder).Contains("\"") ? $"\"{(transVal ?? placeholder).Replace("\"", "\"\"")}\"" : (transVal ?? placeholder);
                    sw.WriteLine($"{escapedKey},{escapedUp},{escapedTrans}");
                }
            }
        }

        private static void WriteTileExcel(CsfFile upstream, CsfFile translated, List<string> keys, string path, string placeholder)
        {
            bool xlsx = !path.ToLower().EndsWith(".xls");
            IWorkbook workbook = xlsx ? (IWorkbook)new XSSFWorkbook() : new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Translation Tile");

            var header = sheet.CreateRow(0);
            header.CreateCell(0).SetCellValue("Label");
            header.CreateCell(1).SetCellValue("Upstream");
            header.CreateCell(2).SetCellValue("Translated");

            int rowIdx = 1;
            foreach (var key in keys)
            {
                upstream.Labels.TryGetValue(key, out string upVal);
                translated.Labels.TryGetValue(key, out string transVal);
                var row = sheet.CreateRow(rowIdx++);
                row.CreateCell(0).SetCellValue(key);
                row.CreateCell(1).SetCellValue(upVal ?? "");
                row.CreateCell(2).SetCellValue(transVal ?? placeholder);
            }

            sheet.AutoSizeColumn(0);
            sheet.AutoSizeColumn(1);
            sheet.AutoSizeColumn(2);

            using (var fs = File.Create(path))
                workbook.Write(fs);
        }

        /// <summary>Creates an update translation template based on old and new upstream and old translation.</summary>
        private static void TranslationUpdateOperation(string oldUpstreamPath, string newUpstreamPath, string oldTranslatedPath,
            string outputPath, string placeholder, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            CsfFile oldUpstream, newUpstream, oldTranslated;
            using (var fs = File.OpenRead(oldUpstreamPath))
                oldUpstream = LoadCsfFromStream(fs, Path.GetExtension(oldUpstreamPath).ToLower(), options, cmdOptions);
            using (var fs = File.OpenRead(newUpstreamPath))
                newUpstream = LoadCsfFromStream(fs, Path.GetExtension(newUpstreamPath).ToLower(), options, cmdOptions);
            using (var fs = File.OpenRead(oldTranslatedPath))
                oldTranslated = LoadCsfFromStream(fs, Path.GetExtension(oldTranslatedPath).ToLower(), options, cmdOptions);

            var result = new CsfFile();
            var allKeys = new HashSet<string>(oldUpstream.Labels.Keys, StringComparer.InvariantCultureIgnoreCase);
            allKeys.UnionWith(newUpstream.Labels.Keys);
            allKeys.UnionWith(oldTranslated.Labels.Keys);

            foreach (var key in allKeys)
            {
                bool oldUpHas = oldUpstream.Labels.TryGetValue(key, out string oldUpValue);
                bool newUpHas = newUpstream.Labels.TryGetValue(key, out string newUpValue);
                bool oldTransHas = oldTranslated.Labels.TryGetValue(key, out string oldTransValue);

                string finalValue;
                if (!newUpHas)
                    finalValue = placeholder + "_DELETE";
                else if (!oldUpHas || (oldUpHas && !string.Equals(oldUpValue, newUpValue, StringComparison.InvariantCulture)))
                    finalValue = placeholder;
                else
                    finalValue = oldTransHas ? oldTransValue : placeholder;

                result.AddLabel(key, finalValue, newUpstream.GetExtra(key));
            }

            result.Version = newUpstream.Version;
            result.Language = newUpstream.Language;

            SaveCsfToFile(result, outputPath, cmdOptions);
            Console.WriteLine($"Update translation template saved to {outputPath}");
        }

        /// <summary>Overrides translation: uses translated value if exists, otherwise upstream.</summary>
        private static void TranslationOverrideOperation(string upstreamPath, string translatedPath, string outputPath,
            CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            CsfFile upstream, translated;
            using (var fs = File.OpenRead(upstreamPath))
                upstream = LoadCsfFromStream(fs, Path.GetExtension(upstreamPath).ToLower(), options, cmdOptions);
            using (var fs = File.OpenRead(translatedPath))
                translated = LoadCsfFromStream(fs, Path.GetExtension(translatedPath).ToLower(), options, cmdOptions);

            var result = new CsfFile();
            var allKeys = new HashSet<string>(upstream.Labels.Keys, StringComparer.InvariantCultureIgnoreCase);
            allKeys.UnionWith(translated.Labels.Keys);

            foreach (var key in allKeys)
            {
                string value;
                byte[] extra;
                if (translated.Labels.TryGetValue(key, out string transValue))
                {
                    value = transValue;
                    extra = translated.GetExtra(key);
                }
                else
                {
                    value = upstream.Labels[key];
                    extra = upstream.GetExtra(key);
                }
                result.AddLabel(key, value, extra);
            }

            result.Version = upstream.Version;
            result.Language = upstream.Language;

            SaveCsfToFile(result, outputPath, cmdOptions);
            Console.WriteLine($"Override translation saved to {outputPath}");
        }
        #endregion

        #region Encoding Fix

        /// <summary>Fixes text encoding in a CSF file.</summary>
        private static void FixEncoding(string inputPath, string outputPath, Encoding sourceEncoding, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            var ext = Path.GetExtension(inputPath).ToLower();
            using (var inputStream = File.OpenRead(inputPath))
            using (var outputStream = File.Create(outputPath))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, ext, options, cmdOptions);
                FixCsfEncoding(csf, sourceEncoding);
                var outputExt = Path.GetExtension(outputPath).ToLower();
                if (outputExt == ".csf") csf.WriteCsfFile(outputStream);
                else if (outputExt == ".ini") CsfFileIniHelper.WriteIniFile(csf, outputStream);
                else if (outputExt == ".json") CsfFileJsonHelper.WriteJsonFile(csf, outputStream);
                else if (outputExt == ".yaml" || outputExt == ".yml") CsfFileYamlHelper.WriteYamlFile(csf, outputStream);
                else if (outputExt == ".llf") csf.WriteLlfFile(outputStream, Path.GetFileNameWithoutExtension(outputPath));
                else if (outputExt == ".txt") csf.WriteTxtFile(outputStream);
                else if (outputExt == ".xlsx" || outputExt == ".xls") csf.WriteExcelFile(outputStream, outputExt == ".xlsx");
                else if (outputExt == ".csv") csf.WriteCsvFile(outputStream, cmdOptions.GetCsvDelimiter(), null);
                else throw new NotSupportedException($"Unsupported output format: {outputExt}");
            }
        }

        /// <summary>Internal helper to fix encoding of individual labels.</summary>
        private static void FixCsfEncoding(CsfFile csfFile, Encoding sourceEncoding)
        {
            foreach (var label in csfFile.Labels.ToList())
            {
                try
                {
                    byte[] unicodeBytes = Encoding.Unicode.GetBytes(label.Value);
                    byte[] sourceBytes = new byte[unicodeBytes.Length / 2];
                    for (int i = 0; i < sourceBytes.Length; i++)
                        sourceBytes[i] = unicodeBytes[i * 2];
                    string fixedValue = sourceEncoding.GetString(sourceBytes);
                    csfFile.AddLabel(label.Key, fixedValue, csfFile.GetExtra(label.Key));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error fixing label {label.Key}: {ex.Message}");
                }
            }
        }
        #endregion

        #region Help

        /// <summary>Displays help information.</summary>
        private static void ShowHelp()
        {
            Console.WriteLine("CsfStudio - CSF/INI/JSON/YAML/LLF/TXT/Excel/CSV converter for Red Alert 2");
            Console.WriteLine("========================================================================");
            Console.WriteLine();
            Console.WriteLine("SYNOPSIS");
            Console.WriteLine("    CsfStudio.exe -i <input> [-i <input2> ...] -o <output> --<operation> [options]");
            Console.WriteLine();
            Console.WriteLine("DESCRIPTION");
            Console.WriteLine("    CsfStudio is a command-line tool for working with RA2/Yuri's Revenge string");
            Console.WriteLine("    table files (.csf). It supports conversion between multiple formats, set");
            Console.WriteLine("    operations on label sets, map label checking, and translation helpers.");
            Console.WriteLine();
            Console.WriteLine("FORMAT CONVERSION (one input file)");
            Console.WriteLine("    --to-csf      Convert input to .csf (binary game format)");
            Console.WriteLine("    --to-ini      Convert input to .ini (human-readable, supports multi-line)");
            Console.WriteLine("    --to-json     Convert input to .json");
            Console.WriteLine("    --to-yaml     Convert input to .yaml");
            Console.WriteLine("    --to-llf      Convert input to .llf (Label Language File)");
            Console.WriteLine("    --to-txt      Convert input to .txt (CSFTool format)");
            Console.WriteLine("    --to-excel    Convert input to .xlsx or .xls (Excel)");
            Console.WriteLine("    --to-csv      Convert input to .csv (comma-separated values)");
            Console.WriteLine();
            Console.WriteLine("    Examples:");
            Console.WriteLine("        CsfStudio.exe -i stringtable01.csf -o stringtable01.ini --to-ini");
            Console.WriteLine("        CsfStudio.exe -i stringtable01.ini -o stringtable01.xlsx --to-excel");
            Console.WriteLine();
            Console.WriteLine("SET OPERATIONS (two or more input files)");
            Console.WriteLine("    --merge                          Union: all labels from all files");
            Console.WriteLine("    --subtract                       A minus B: labels in first but not in others");
            Console.WriteLine("    --intersection                   Labels present in ALL files; differing values replaced");
            Console.WriteLine("    --symmetric-difference           Labels present in exactly ONE file (values must match)");
            Console.WriteLine("    --override-case                  Keep values from current file, but use label case from upstream");
            Console.WriteLine("    --diff                           Compare two files: show added, removed, changed labels");
            Console.WriteLine();
            Console.WriteLine("INFO OPERATIONS (one input file)");
            Console.WriteLine("    --stats                          Show file statistics (count, size, language, version)");
            Console.WriteLine("    --validate                       Validate file for errors (empty names, null values)");
            Console.WriteLine("    --search <pattern>               Search labels by substring (or regex: prefix)");
            Console.WriteLine("    --export-labels                  Export list of label names");
            Console.WriteLine();
            Console.WriteLine("DIFF OPERATIONS (two input files)");
            Console.WriteLine("    --diff-stat                      Short diff summary without full label list");
            Console.WriteLine();
            Console.WriteLine("BATCH OPERATIONS");
            Console.WriteLine("    --batch                          Process all supported files in a folder");
            Console.WriteLine("    --batch-folder <folder>          Folder containing files to process");
            Console.WriteLine("    --output-folder <folder>         Output folder (optional, defaults to input folder)");
            Console.WriteLine("    --recursive                      Include subfolders in batch processing");
            Console.WriteLine();
            Console.WriteLine("    Examples:");
            Console.WriteLine("        CsfStudio.exe -i stringtable01.csf,stringtable02.csf -o stringtable_merged.csf --merge");
            Console.WriteLine("        CsfStudio.exe -i stringtable_upstream.csf,stringtable_current.csf -o stringtable_fixed.csf --override-case");
            Console.WriteLine("        CsfStudio.exe -i old.csf,new.csf --diff");
            Console.WriteLine("        CsfStudio.exe -i old.csf,new.csf -o diff_report.txt --diff");
            Console.WriteLine();
            Console.WriteLine("        CsfStudio.exe -i stringtable01.csf --stats");
            Console.WriteLine("        CsfStudio.exe -i stringtable01.csf --validate");
            Console.WriteLine("        CsfStudio.exe -i stringtable01.csf --search \"GDI\"");
            Console.WriteLine("        CsfStudio.exe -i stringtable01.csf --search \"regex:^Units\\\\.\" --matches.txt");
            Console.WriteLine("        CsfStudio.exe -i stringtable01.csf --export-labels -o labels.txt");
            Console.WriteLine();
            Console.WriteLine("        CsfStudio.exe -i old.csf,new.csf --diff-stat");
            Console.WriteLine();
            Console.WriteLine("        CsfStudio.exe --batch --batch-folder \"C:\\RA2\\Strings\" --to-json");
            Console.WriteLine("        CsfStudio.exe --batch --batch-folder \"C:\\RA2\\Strings\" --output-folder \"C:\\Output\" --to-ini");
            Console.WriteLine();
            Console.WriteLine("        CsfStudio.exe -i old.csf,new.csf -o merged.csf --merge --merge-strategy last-wins");
            Console.WriteLine();
            Console.WriteLine("        CsfStudio.exe -i stringtable01.csf -o output.json --to-json --dry-run");
            Console.WriteLine();
            Console.WriteLine("MAP LABEL CHECK");
            Console.WriteLine("    --check-maps                     Scan map files (.map/.mpr/.yrm) and report missing labels");
            Console.WriteLine("    --map-folder <folder>            Folder containing map files (relative or absolute path)");
            Console.WriteLine("    -o <file>                        Output missing labels list (optional, otherwise prints to console)");
            Console.WriteLine();
            Console.WriteLine("    Example:");
            Console.WriteLine("        CsfStudio.exe -i ra2md.csf --check-maps --map-folder \"C:\\RA2\\maps\" -o missing_labels.txt");
            Console.WriteLine();
            Console.WriteLine("TRANSLATION HELPERS");
            Console.WriteLine("    --translation-new                Create translation template: all values replaced with placeholder");
            Console.WriteLine("    --translation-tile               Side-by-side comparison INI (upstream vs translated). Only .ini output.");
            Console.WriteLine("    --translation-update             Update translation based on old and new upstream and old translation");
            Console.WriteLine("    --translation-override           Merge: use translated if exists, else upstream");
            Console.WriteLine("    --translation-placeholder        Text to use for missing translations (default: TODO_Translation_Needed)");
            Console.WriteLine("    --translation-delete-placeholder Text for labels removed in new upstream (default: TODO_Translation_Delete_Needed)");
            Console.WriteLine();
            Console.WriteLine("    Examples:");
            Console.WriteLine("        CsfStudio.exe -i stringtable_upstream.csf -o stringtable_trans.ini --translation-new");
            Console.WriteLine("        CsfStudio.exe -i stringtable_upstream.csf,stringtable_translated.csf -o stringtable_compare.ini --translation-tile");
            Console.WriteLine("        CsfStudio.exe -i stringtable_old_up.csf,stringtable_new_up.csf,stringtable_old_trans.csf -o stringtable_update.ini --translation-update");
            Console.WriteLine("        CsfStudio.exe -i stringtable_upstream.csf,stringtable_translated.csf -o stringtable_merged.csf --translation-override");
            Console.WriteLine();
            Console.WriteLine("ENCODING FIX");
            Console.WriteLine("    --fix-encoding <enc>             Reinterpret CSF strings as given encoding (e.g., windows-1251, gb18030)");
            Console.WriteLine("    Supported encodings: gb18030, gb2312, windows-1251, windows-1252, iso-8859-1, utf-8, unicode");
            Console.WriteLine();
            Console.WriteLine("    Example:");
            Console.WriteLine("        CsfStudio.exe -i stringtable_broken.csf -o stringtable_fixed.csf --fix-encoding windows-1251");
            Console.WriteLine();
            Console.WriteLine("GENERAL OPTIONS");
            Console.WriteLine("    -i, --input <file>[,file2...]    Input file(s) (comma-separated)");
            Console.WriteLine("    -o, --output <file>              Output file (required for most operations)");
            Console.WriteLine("    --csv-delimiter <delim>          CSV delimiter: auto, comma, semicolon, tab, pipe, space (default: auto)");
            Console.WriteLine("    --order-by-key                   Sort labels alphabetically when saving");
            Console.WriteLine("    --diff-placeholder <text>        Placeholder for differing values in intersection (default: TODO_Different_Value)");
            Console.WriteLine("    --merge-strategy <strategy>      Merge conflict resolution: first-wins (default), last-wins, error");
            Console.WriteLine("    --dry-run                        Preview output without writing files");
            Console.WriteLine("    --force                          Overwrite existing output file without prompt");
            Console.WriteLine("    --output-encoding <enc>          Output encoding: utf-8 (default), ascii, unicode");
            Console.WriteLine("    --quiet                          Suppress non-essential output");
            Console.WriteLine("    --verbose                        Show additional information");
            Console.WriteLine("    -h, --help                       Show this help");
            Console.WriteLine();
            Console.WriteLine("NOTES");
            Console.WriteLine("    - Extra data (WRTS) is preserved in all conversions.");
            Console.WriteLine("    - When using --translation-update, labels removed in new upstream get a '_DELETE' suffix.");
            Console.WriteLine("    - For map label check, the tool parses UIName, Actions (type 11/103 with param 4), and Ranking section.");
            Console.WriteLine();
        }
        #endregion
    }
}