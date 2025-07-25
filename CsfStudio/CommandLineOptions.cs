using System;
using System.Collections.Generic;
using System.Linq;

namespace CsfStudio
{
    class CommandLineOptions
    {
        public List<string> InputFiles { get; set; } = new List<string>();
        public string OutputFile { get; set; }
        public bool ToIni { get; set; }
        public bool ToJson { get; set; }
        public bool ToYaml { get; set; }
        public bool ToCsf { get; set; }
        public bool Merge { get; set; }
        public bool Subtract { get; set; }
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

                    case "--merge":
                        options.Merge = true;
                        break;

                    case "--subtract":
                        options.Subtract = true;
                        break;

                    case "-h":
                    case "--help":
                        options.ShowHelp = true;
                        break;

                    default:
                        throw new ArgumentException($"Unknown option: {arg}");
                }
            }

            if (!options.ShowHelp)
            {
                if (options.InputFiles.Count == 0)
                    throw new ArgumentException("Input file is required");

                if (string.IsNullOrEmpty(options.OutputFile))
                    throw new ArgumentException("Output file is required");

                if (options.InputFiles.Count < 2 && (options.Merge || options.Subtract))
                    throw new ArgumentException("Need at least 2 files for merge/subtract operations");
            }

            return options;
        }
    }
}