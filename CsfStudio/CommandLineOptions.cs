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
        
        // Flag for CSF to INI conversion
        public bool ToIni { get; set; }
        
        // Flag for INI to CSF conversion
        public bool ToCsf { get; set; }
        
        // Flag for JSON conversion
        public bool ToJson { get; set; }
        
        // Flag for YAML conversion
        public bool ToYaml { get; set; }
        
        // Flag for LLF conversion
        public bool ToLlf { get; set; }
        
        // Flag for merge operation
        public bool Merge { get; set; }
        
        // Flag for subtract operation
        public bool Subtract { get; set; }
        
        // Encoding specification for fixing text encoding
        public string FixEncoding { get; set; }
        
        // Flag to show help message
        public bool ShowHelp { get; set; }

        /// <summary>
        /// Parses command line arguments into options
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Parsed options object</returns>
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

                    case "--to-ini":
                        options.ToIni = true;
                        break;
                    
                    case "--to-json":
                        options.ToJson = true;
                        break;

                    case "--to-yaml":
                        options.ToYaml = true;
                        break;

                    case "--to-csf":
                        options.ToCsf = true;
                        break;
                        
                    case "--to-llf":
                        options.ToLlf = true;
                        break;

                    case "--merge":
                        options.Merge = true;
                        break;

                    case "--subtract":
                        options.Subtract = true;
                        break;

                    case "--fix-encoding":
                        if (arguments.Count == 0) throw new ArgumentException("Missing encoding specification");
                        options.FixEncoding = arguments.Dequeue();
                        break;

                    case "-h":
                    case "--help":
                        options.ShowHelp = true;
                        break;

                    default:
                        throw new ArgumentException($"Unknown option: {arg}");
                }
            }

            // Validate arguments if not showing help
            if (!options.ShowHelp)
            {
                if (options.InputFiles.Count == 0)
                    throw new ArgumentException("Input file is required");

                // Output file is required except for merge/subtract operations
                if (string.IsNullOrEmpty(options.OutputFile)) 
                {
                    if (!options.Merge && !options.Subtract)
                        throw new ArgumentException("Output file is required");
                }

                // Need at least 2 files for merge/subtract operations
                if (options.InputFiles.Count < 2 && (options.Merge || options.Subtract))
                    throw new ArgumentException("Need at least 2 files for merge/subtract operations");

                // Only one input file allowed for encoding fix
                if (!string.IsNullOrEmpty(options.FixEncoding))
                {
                    if (options.InputFiles.Count != 1)
                        throw new ArgumentException("Only one input file is allowed for encoding fix");
                }
            }

            return options;
        }

        /// <summary>
        /// Gets the Encoding object based on the specified encoding name
        /// </summary>
        /// <returns>Encoding object or null if not specified</returns>
        public Encoding GetEncoding()
        {
            if (string.IsNullOrEmpty(FixEncoding))
                return null;

            switch (FixEncoding.ToLower())
            {
                case "gb18030":
                    return Encoding.GetEncoding(54936); // GB18030 code page
                case "gb2312":
                    return Encoding.GetEncoding(936);   // GB2312 compatibility
                case "windows-1251":
                    return Encoding.GetEncoding(1251);  // Cyrillic
                case "windows-1252":
                    return Encoding.GetEncoding(1252);  // Western European
                case "iso-8859-1":
                    return Encoding.GetEncoding(28591); // Latin-1
                case "utf-8":
                    return Encoding.UTF8;
                case "unicode":
                    return Encoding.Unicode;
                default:
                    throw new NotSupportedException($"Encoding {FixEncoding} is not supported");
            }
        }
    }
}