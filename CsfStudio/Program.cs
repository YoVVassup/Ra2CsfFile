using SadPencil.Ra2CsfFile;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

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
                    TreatExtraAsText = options.TreatExtraAsText,
                    OrderByKey = options.OrderByKey
                };

                // Dispatch based on operation type
                if (options.Merge)
                    ProcessFiles(options.InputFiles, options.OutputFile, MergeOperation, csfOptions, options);
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
            using (var outputStream = File.Create(options.OutputFile))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                CsfFileIniHelper.WriteIniFile(csf, outputStream);
            }
        }

        /// <summary>Converts input file to CSF format.</summary>
        private static void ConvertToCsf(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            using (var outputStream = File.Create(options.OutputFile))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                csf.WriteCsfFile(outputStream);
            }
        }

        /// <summary>Converts input file to JSON format.</summary>
        private static void ConvertToJson(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            using (var outputStream = File.Create(options.OutputFile))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                CsfFileJsonHelper.WriteJsonFile(csf, outputStream);
            }
        }

        /// <summary>Converts input file to YAML format.</summary>
        private static void ConvertToYaml(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            using (var outputStream = File.Create(options.OutputFile))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                CsfFileYamlHelper.WriteYamlFile(csf, outputStream);
            }
        }

        /// <summary>Converts input file to LLF format.</summary>
        private static void ConvertToLlf(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            using (var outputStream = File.Create(options.OutputFile))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                string fileName = Path.GetFileNameWithoutExtension(options.OutputFile);
                csf.WriteLlfFile(outputStream, fileName);
            }
        }

        /// <summary>Converts input file to TXT (CSFTool) format.</summary>
        private static void ConvertToTxt(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            using (var outputStream = File.Create(options.OutputFile))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                csf.WriteTxtFile(outputStream);
            }
        }

        /// <summary>Converts input file to Excel (XLSX/XLS) format.</summary>
        private static void ConvertToExcel(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            using (var outputStream = File.Create(options.OutputFile))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                bool xlsx = !options.OutputFile.ToLower().EndsWith(".xls");
                csf.WriteExcelFile(outputStream, xlsx);
            }
        }

        /// <summary>Converts input file to CSV format.</summary>
        private static void ConvertToCsv(CommandLineOptions options, CsfFileOptions csfOptions)
        {
            var inputExt = Path.GetExtension(options.InputFiles[0]).ToLower();
            using (var inputStream = File.OpenRead(options.InputFiles[0]))
            using (var outputStream = File.Create(options.OutputFile))
            {
                CsfFile csf = LoadCsfFromStream(inputStream, inputExt, csfOptions, options);
                string delimiter = options.GetCsvDelimiter();
                csf.WriteCsvFile(outputStream, delimiter, null);
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
            SaveCsfToFile(result, outputPath, cmdOptions);
        }

        /// <summary>Saves a CsfFile to any supported output format based on file extension.</summary>
        private static void SaveCsfToFile(CsfFile csf, string outputPath, CommandLineOptions cmdOptions)
        {
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
        private static CsfFile MergeOperation(List<CsfFile> files)
        {
            var result = new CsfFile();
            foreach (var file in files)
                foreach (var label in file.Labels)
                    result.AddLabel(label.Key, label.Value, file.GetExtra(label.Key));
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

        #region Check Maps Operation (using CsfFileMapHelper)

        /// <summary>Checks map files for missing labels in the given CSF file.</summary>
        private static void CheckMapsOperation(string csfFilePath, string mapFolder, string outputFilePath, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            // Path normalization
            string absoluteMapFolder;
            try
            {
                if (string.IsNullOrWhiteSpace(mapFolder))
                    throw new ArgumentException("Map folder path is empty.");

                // If the path is relative, combine with the current directory
                if (!Path.IsPathRooted(mapFolder))
                {
                    // Handling special cases of "." And ".."
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Invalid map folder path '{mapFolder}': {ex.Message}");
                return;
            }

            // Removing a possible trailing delimiter
            absoluteMapFolder = absoluteMapFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Console.WriteLine($"Map folder (absolute): {absoluteMapFolder}");

            if (!Directory.Exists(absoluteMapFolder))
            {
                Console.WriteLine($"Error: Map folder does not exist: {absoluteMapFolder}");
                return;
            }

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

        /// <summary>Creates a tiled comparison INI with upstream and translated lines side by side. Only INI output.</summary>
        private static void TranslationTileOperation(string upstreamPath, string translatedPath, string outputPath, string placeholder, CsfFileOptions options, CommandLineOptions cmdOptions)
        {
            CsfFile upstream, translated;
            using (var fs = File.OpenRead(upstreamPath))
                upstream = LoadCsfFromStream(fs, Path.GetExtension(upstreamPath).ToLower(), options, cmdOptions);
            using (var fs = File.OpenRead(translatedPath))
                translated = LoadCsfFromStream(fs, Path.GetExtension(translatedPath).ToLower(), options, cmdOptions);

            if (!outputPath.ToLower().EndsWith(".ini"))
                throw new NotSupportedException("--translation-tile only supports .ini output format");

            var allKeys = new HashSet<string>(upstream.Labels.Keys, StringComparer.InvariantCultureIgnoreCase);
            allKeys.UnionWith(translated.Labels.Keys);

            using (var sw = new StreamWriter(outputPath, false, new UTF8Encoding(false)))
            {
                sw.WriteLine("[SadPencil.Ra2CsfFile.Ini]");
                sw.WriteLine("IniVersion=3");
                sw.WriteLine($"CsfVersion={upstream.Version}");
                sw.WriteLine($"CsfLang={(int)upstream.Language}");
                sw.WriteLine();

                foreach (var key in allKeys)
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
            Console.WriteLine($"Tiled comparison saved to {outputPath}");
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
            Console.WriteLine();
            Console.WriteLine("    Examples:");
            Console.WriteLine("        CsfStudio.exe -i stringtable01.csf,stringtable02.csf -o stringtable_merged.csf --merge");
            Console.WriteLine("        CsfStudio.exe -i stringtable_upstream.csf,stringtable_current.csf -o stringtable_fixed.csf --override-case");
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
            Console.WriteLine("    --extra-mode <text|base64>       How to store extra data (WRTS) in text formats (default: text)");
            Console.WriteLine("    --csv-delimiter <delim>          CSV delimiter: auto, comma, semicolon, tab, pipe, space (default: auto)");
            Console.WriteLine("    --order-by-key                   Sort labels alphabetically when saving");
            Console.WriteLine("    --diff-placeholder <text>        Placeholder for differing values in intersection (default: TODO_Different_Value)");
            Console.WriteLine("    -h, --help                       Show this help");
            Console.WriteLine();
            Console.WriteLine("NOTES");
            Console.WriteLine("    - Extra data (WRTS) is preserved in all conversions.");
            Console.WriteLine("    - Use '--extra-mode base64' to store extra data as Base64 in text formats.");
            Console.WriteLine("    - --translation-tile only supports .ini output due to custom keys (UpstreamLine, TranslatedLine).");
            Console.WriteLine("    - When using --translation-update, labels removed in new upstream get a '_DELETE' suffix.");
            Console.WriteLine("    - For map label check, the tool parses UIName, Actions (type 11/103 with param 4), and Ranking section.");
            Console.WriteLine();
        }
        #endregion
    }
}