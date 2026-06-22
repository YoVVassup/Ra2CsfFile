using System;
using System.Collections.Generic;
using System.Text;

namespace CsfStudio
{
    /// <summary>
    /// Handles parsing and storage of command line arguments
    /// </summary>
    class CommandLineOptions
    {
        // List of input files to process
        public List<string> InputFiles { get; set; } = new List<string>();
        
        // Output file path
        public string OutputFile { get; set; }
        
        // Format conversion flags
        public bool ToIni { get; set; }
        public bool ToCsf { get; set; }
        public bool ToJson { get; set; }
        public bool ToYaml { get; set; }
        public bool ToLlf { get; set; }
        public bool ToTxt { get; set; }
        public bool ToExcel { get; set; }
        public bool ToCsv { get; set; }
        
        // Set operations
        public bool Merge { get; set; }
        public bool Subtract { get; set; }
        public bool Intersection { get; set; }
        public bool SymmetricDifference { get; set; }
        public bool OverrideCase { get; set; }
        public bool Diff { get; set; }
        public bool Stats { get; set; }
        public bool Validate { get; set; }
        public bool Batch { get; set; }
        public string BatchFolder { get; set; }
        public string OutputFolder { get; set; }
        public string MergeStrategy { get; set; } = "first-wins";
        public bool DryRun { get; set; }
        public string SearchPattern { get; set; }
        public bool ExportLabels { get; set; }
        public bool DiffStat { get; set; }
        public string OutputEncoding { get; set; }
        public bool Force { get; set; }
        public bool Recursive { get; set; }
        public bool Quiet { get; set; }
        public bool Verbose { get; set; }
        
        // Map check
        public bool CheckMaps { get; set; }
        public string MapFolder { get; set; }
        
        // Translation helpers
        public bool TranslationNew { get; set; }
        public bool TranslationTile { get; set; }
        public bool TranslationUpdate { get; set; }
        public bool TranslationOverride { get; set; }
        public string TranslationPlaceholder { get; set; } = "TODO_Translation_Needed";
        public string TranslationDeletePlaceholder { get; set; } = "TODO_Translation_Delete_Needed";
        
        // Encoding fix
        public string FixEncoding { get; set; }
        
        // CSV delimiter
        public string CsvDelimiter { get; set; } = "auto";
        
        // Order by key
        public bool OrderByKey { get; set; } = false;
        
        // Placeholder for intersection diff
        public string DiffPlaceholder { get; set; } = "TODO_Different_Value";
        
        public bool ShowHelp { get; set; }

        public static CommandLineOptions Parse(string[] args)
        {
            var options = new CommandLineOptions();
            var arguments = new Queue<string>(args);

            while (arguments.Count > 0)
            {
                var arg = arguments.Dequeue().ToLower();

                switch (arg)
                {
                    case "-i":
                    case "--input":
                        if (arguments.Count == 0) throw new ArgumentException("Missing input file path");
                        options.InputFiles.AddRange(arguments.Dequeue().Split(','));
                        break;

                    case "-o":
                    case "--output":
                        if (arguments.Count == 0) throw new ArgumentException("Missing output file path");
                        options.OutputFile = arguments.Dequeue();
                        break;

                    case "--to-ini": options.ToIni = true; break;
                    case "--to-csf": options.ToCsf = true; break;
                    case "--to-json": options.ToJson = true; break;
                    case "--to-yaml": options.ToYaml = true; break;
                    case "--to-llf": options.ToLlf = true; break;
                    case "--to-txt": options.ToTxt = true; break;
                    case "--to-excel": options.ToExcel = true; break;
                    case "--to-csv": options.ToCsv = true; break;

                    case "--merge": options.Merge = true; break;
                    case "--subtract": options.Subtract = true; break;
                    case "--intersection": options.Intersection = true; break;
                    case "--symmetric-difference": options.SymmetricDifference = true; break;
                    case "--override-case": options.OverrideCase = true; break;
                    case "--diff": options.Diff = true; break;
                    case "--stats": options.Stats = true; break;
                    case "--validate": options.Validate = true; break;

                    case "--batch": options.Batch = true; break;
                    case "--batch-folder":
                        if (arguments.Count == 0) throw new ArgumentException("Missing batch folder path");
                        options.BatchFolder = arguments.Dequeue();
                        break;
                    case "--output-folder":
                        if (arguments.Count == 0) throw new ArgumentException("Missing output folder path");
                        options.OutputFolder = arguments.Dequeue();
                        break;
                    case "--merge-strategy":
                        if (arguments.Count == 0) throw new ArgumentException("Missing merge strategy");
                        string strategy = arguments.Dequeue().ToLower();
                        if (strategy != "first-wins" && strategy != "last-wins" && strategy != "error")
                            throw new ArgumentException("Merge strategy must be: first-wins, last-wins, or error");
                        options.MergeStrategy = strategy;
                        break;
                    case "--dry-run": options.DryRun = true; break;

                    case "--search":
                        if (arguments.Count == 0) throw new ArgumentException("Missing search pattern");
                        options.SearchPattern = arguments.Dequeue();
                        break;
                    case "--export-labels": options.ExportLabels = true; break;
                    case "--diff-stat": options.DiffStat = true; break;
                    case "--output-encoding":
                        if (arguments.Count == 0) throw new ArgumentException("Missing output encoding");
                        options.OutputEncoding = arguments.Dequeue();
                        break;
                    case "--force": options.Force = true; break;
                    case "--recursive": options.Recursive = true; break;
                    case "--quiet": options.Quiet = true; break;
                    case "--verbose": options.Verbose = true; break;

                    case "--check-maps": options.CheckMaps = true; break;
                    case "--map-folder":
                        if (arguments.Count == 0) throw new ArgumentException("Missing map folder path");
                        options.MapFolder = arguments.Dequeue();
                        break;

                    case "--translation-new": options.TranslationNew = true; break;
                    case "--translation-tile": options.TranslationTile = true; break;
                    case "--translation-update": options.TranslationUpdate = true; break;
                    case "--translation-override": options.TranslationOverride = true; break;
                    case "--translation-placeholder":
                        if (arguments.Count == 0) throw new ArgumentException("Missing placeholder text");
                        options.TranslationPlaceholder = arguments.Dequeue();
                        break;
                    case "--translation-delete-placeholder":
                        if (arguments.Count == 0) throw new ArgumentException("Missing delete placeholder text");
                        options.TranslationDeletePlaceholder = arguments.Dequeue();
                        break;

                    case "--fix-encoding":
                        if (arguments.Count == 0) throw new ArgumentException("Missing encoding specification");
                        options.FixEncoding = arguments.Dequeue();
                        break;

                    case "--csv-delimiter":
                        if (arguments.Count == 0) throw new ArgumentException("Missing delimiter specification");
                        string delim = arguments.Dequeue().ToLower();
                        if (delim != "auto" && delim != "comma" && delim != "semicolon" && delim != "tab" && delim != "pipe" && delim != "space")
                            throw new ArgumentException("CSV delimiter must be: auto, comma, semicolon, tab, pipe, space");
                        options.CsvDelimiter = delim;
                        break;

                    case "--order-by-key": options.OrderByKey = true; break;
                    case "--diff-placeholder":
                        if (arguments.Count == 0) throw new ArgumentException("Missing placeholder text");
                        options.DiffPlaceholder = arguments.Dequeue();
                        break;

                    case "-h":
                    case "--help":
                        options.ShowHelp = true;
                        break;

                    default:
                        throw new ArgumentException($"Unknown option: {arg}");
                }
            }

            // Validation
            if (!options.ShowHelp)
            {
                if (options.InputFiles.Count == 0 && !options.Batch)
                    throw new ArgumentException("Input file is required");

                // Translation operations require specific number of inputs
                if (options.TranslationNew)
                {
                    if (options.InputFiles.Count != 1)
                        throw new ArgumentException("--translation-new requires exactly one input file (upstream)");
                    if (string.IsNullOrEmpty(options.OutputFile))
                        throw new ArgumentException("Output file is required for --translation-new");
                }
                else if (options.TranslationTile)
                {
                    if (options.InputFiles.Count != 2)
                        throw new ArgumentException("--translation-tile requires two input files (upstream, translated)");
                    if (string.IsNullOrEmpty(options.OutputFile))
                        throw new ArgumentException("Output file is required for --translation-tile");
                }
                else if (options.TranslationUpdate)
                {
                    if (options.InputFiles.Count != 3)
                        throw new ArgumentException("--translation-update requires three input files (old_upstream, new_upstream, old_translated)");
                    if (string.IsNullOrEmpty(options.OutputFile))
                        throw new ArgumentException("Output file is required for --translation-update");
                }
                else if (options.TranslationOverride)
                {
                    if (options.InputFiles.Count != 2)
                        throw new ArgumentException("--translation-override requires two input files (upstream, translated)");
                    if (string.IsNullOrEmpty(options.OutputFile))
                        throw new ArgumentException("Output file is required for --translation-override");
                }
                else if (options.CheckMaps)
                {
                    if (options.InputFiles.Count != 1)
                        throw new ArgumentException("--check-maps requires exactly one input file (CSF)");
                    if (string.IsNullOrEmpty(options.MapFolder))
                        throw new ArgumentException("--map-folder is required for --check-maps");
                }
                else if (options.Diff)
                {
                    if (options.InputFiles.Count != 2)
                        throw new ArgumentException("--diff requires exactly two input files (old, new)");
                }
                else if (options.Stats)
                {
                    if (options.InputFiles.Count != 1)
                        throw new ArgumentException("--stats requires exactly one input file");
                }
                else if (options.Validate)
                {
                    if (options.InputFiles.Count != 1)
                        throw new ArgumentException("--validate requires exactly one input file");
                }
                else if (options.Batch)
                {
                    if (string.IsNullOrEmpty(options.BatchFolder))
                        throw new ArgumentException("--batch requires --batch-folder");
                    if (!options.ToIni && !options.ToCsf && !options.ToJson && !options.ToYaml && !options.ToLlf && !options.ToTxt && !options.ToExcel && !options.ToCsv)
                        throw new ArgumentException("--batch requires a conversion operation (--to-ini, --to-csf, etc.)");
                }
                else if (!string.IsNullOrEmpty(options.SearchPattern))
                {
                    if (options.InputFiles.Count != 1)
                        throw new ArgumentException("--search requires exactly one input file");
                }
                else if (options.ExportLabels)
                {
                    if (options.InputFiles.Count != 1)
                        throw new ArgumentException("--export-labels requires exactly one input file");
                }
                else if (options.DiffStat)
                {
                    if (options.InputFiles.Count != 2)
                        throw new ArgumentException("--diff-stat requires exactly two input files (old, new)");
                }
                else
                {
                    // Output file required for conversions
                    bool needsOutput = !options.Merge && !options.Subtract && !options.Intersection && !options.SymmetricDifference && !options.OverrideCase && !options.Diff && !options.DiffStat && !options.Stats && !options.Validate && string.IsNullOrEmpty(options.SearchPattern) && !options.ExportLabels;
                    if (string.IsNullOrEmpty(options.OutputFile) && needsOutput)
                        throw new ArgumentException("Output file is required");
                }

                // Set operations input count
                int requiredInputCount = (options.Merge || options.Subtract || options.Intersection || options.SymmetricDifference || options.OverrideCase) ? 2 : 1;
                if (!options.TranslationNew && !options.TranslationTile && !options.TranslationUpdate && !options.TranslationOverride && !options.CheckMaps && !options.Diff && !options.Stats && !options.Validate && !options.Batch && !options.ExportLabels && !options.DiffStat && string.IsNullOrEmpty(options.SearchPattern) && options.InputFiles.Count < requiredInputCount)
                    throw new ArgumentException($"Need at least {requiredInputCount} input file(s) for this operation");

                // Only one operation allowed
                int opCount = (options.Merge ? 1 : 0) + (options.Subtract ? 1 : 0) + (options.Intersection ? 1 : 0) +
                              (options.SymmetricDifference ? 1 : 0) + (options.OverrideCase ? 1 : 0) +
                              (options.CheckMaps ? 1 : 0) + (options.TranslationNew ? 1 : 0) +
                              (options.TranslationTile ? 1 : 0) + (options.TranslationUpdate ? 1 : 0) +
                              (options.TranslationOverride ? 1 : 0) + (options.Diff ? 1 : 0) + (options.Stats ? 1 : 0) + (options.Validate ? 1 : 0) + (options.Batch ? 1 : 0) +
                              (!string.IsNullOrEmpty(options.SearchPattern) ? 1 : 0) + (options.ExportLabels ? 1 : 0) + (options.DiffStat ? 1 : 0);
                if (opCount > 1)
                    throw new ArgumentException("Only one operation can be specified");
            }

            return options;
        }

        public string GetCsvDelimiter()
        {
            switch (CsvDelimiter.ToLower())
            {
                case "comma": return ",";
                case "semicolon": return ";";
                case "tab": return "\t";
                case "pipe": return "|";
                case "space": return " ";
                default: return ",";
            }
        }

        public Encoding GetEncoding()
        {
            if (string.IsNullOrEmpty(FixEncoding))
                return null;

            switch (FixEncoding.ToLower())
            {
                case "gb18030": return Encoding.GetEncoding(54936);
                case "gb2312": return Encoding.GetEncoding(936);
                case "windows-1251": return Encoding.GetEncoding(1251);
                case "windows-1252": return Encoding.GetEncoding(1252);
                case "iso-8859-1": return Encoding.GetEncoding(28591);
                case "utf-8": return Encoding.UTF8;
                case "unicode": return Encoding.Unicode;
                default: throw new NotSupportedException($"Encoding {FixEncoding} is not supported");
            }
        }
    }
}