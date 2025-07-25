using SadPencil.Ra2CsfFile;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace CsfStudio
{
    class Program
    {
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

                if (options.Merge)
                {
                    ProcessFiles(options.InputFiles, options.OutputFile, MergeOperation);
                }
                else if (options.Subtract)
                {
                    ProcessFiles(options.InputFiles, options.OutputFile, SubtractOperation);
                }
                else if (options.ToIni)
                {
                    ConvertCsfToIni(options.InputFiles[0], options.OutputFile);
                }
                else if (options.ToCsf)
                {
                    ConvertIniToCsf(options.InputFiles[0], options.OutputFile);
                }
                else if (options.ToJson)
                {
                    if (Path.GetExtension(options.InputFiles[0]).Equals(".csf", StringComparison.OrdinalIgnoreCase))
                        ConvertCsfToJson(options.InputFiles[0], options.OutputFile);
                    else
                        ConvertJsonToCsf(options.InputFiles[0], options.OutputFile);
                }
                else if (options.ToYaml)
                {
                    if (Path.GetExtension(options.InputFiles[0]).Equals(".csf", StringComparison.OrdinalIgnoreCase))
                        ConvertCsfToYaml(options.InputFiles[0], options.OutputFile);
                    else
                        ConvertYamlToCsf(options.InputFiles[0], options.OutputFile);
                }
                else
                {
                    Console.WriteLine("Error: You must specify an operation (--to-ini, --to-csf, --to-json, --to-yaml, --merge or --subtract)");
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

        private static void ProcessFiles(List<string> inputPaths, string outputPath, Func<List<CsfFile>, CsfFile> operation)
        {
            var firstExt = Path.GetExtension(inputPaths.First()).ToLower();
            if (inputPaths.Any(f => Path.GetExtension(f).ToLower() != firstExt))
            {
                throw new InvalidOperationException("All files must be of the same type");
            }

            var files = new List<CsfFile>();
            foreach (var inputPath in inputPaths)
            {
                using (var inputStream = File.OpenRead(inputPath))
                {
                    if (firstExt == ".csf")
                    {
                        files.Add(CsfFile.LoadFromCsfFile(inputStream));
                    }
                    else if (firstExt == ".ini")
                    {
                        files.Add(CsfFileIniHelper.LoadFromIniFile(inputStream));
                    }
                    else if (firstExt == ".json")
                    {
                        files.Add(CsfFileJsonHelper.LoadFromJsonFile(inputStream));
                    }
                    else if (firstExt == ".yaml" || firstExt == ".yml")
                    {
                        files.Add(CsfFileYamlHelper.LoadFromYamlFile(inputStream));
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported file format: {firstExt}");
                    }
                }
            }

            var result = operation(files);

            using (var outputStream = File.Create(outputPath))
            {
                var outputExt = Path.GetExtension(outputPath).ToLower();
                if (outputExt == ".csf")
                {
                    result.WriteCsfFile(outputStream);
                }
                else if (outputExt == ".ini")
                {
                    CsfFileIniHelper.WriteIniFile(result, outputStream);
                }
                else if (outputExt == ".json")
                {
                    CsfFileJsonHelper.WriteJsonFile(result, outputStream);
                }
                else if (outputExt == ".yaml" || outputExt == ".yml")
                {
                    CsfFileYamlHelper.WriteYamlFile(result, outputStream);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported output format: {outputExt}");
                }
            }
        }

        private static CsfFile MergeOperation(List<CsfFile> files)
        {
            var result = new CsfFile();
            foreach (var file in files)
            {
                foreach (var label in file.Labels)
                {
                    result.AddLabel(label.Key, label.Value);
                }

                if (result.Labels.Count == file.Labels.Count)
                {
                    result.Version = file.Version;
                    result.Language = file.Language;
                }
            }
            return result;
        }

        private static CsfFile SubtractOperation(List<CsfFile> files)
        {
            if (files.Count < 2)
                throw new InvalidOperationException("Need at least 2 files for subtraction");

            var result = new CsfFile(files[0]);
            var labelsToRemove = new HashSet<string>();

            // Собираем все метки из последующих файлов для удаления
            for (int i = 1; i < files.Count; i++)
            {
                foreach (var label in files[i].Labels.Keys)
                {
                    labelsToRemove.Add(label);
                }
            }

            // Удаляем метки, которые есть в других файлах
            foreach (var label in labelsToRemove)
            {
                result.RemoveLabel(label);
            }

            return result;
        }

        private static void ConvertCsfToIni(string inputPath, string outputPath)
        {
            using (var inputStream = File.OpenRead(inputPath))
            using (var outputStream = File.Create(outputPath))
            {
                var csfFile = CsfFile.LoadFromCsfFile(inputStream);
                CsfFileIniHelper.WriteIniFile(csfFile, outputStream);
            }
        }

        private static void ConvertIniToCsf(string inputPath, string outputPath)
        {
            using (var inputStream = File.OpenRead(inputPath))
            using (var outputStream = File.Create(outputPath))
            {
                var csfFile = CsfFileIniHelper.LoadFromIniFile(inputStream);
                csfFile.WriteCsfFile(outputStream);
            }
        }
        
        private static void ConvertCsfToJson(string inputPath, string outputPath)
        {
            using (var inputStream = File.OpenRead(inputPath))
            using (var outputStream = File.Create(outputPath))
            {
                var csfFile = CsfFile.LoadFromCsfFile(inputStream);
                CsfFileJsonHelper.WriteJsonFile(csfFile, outputStream);
            }
        }

        private static void ConvertJsonToCsf(string inputPath, string outputPath)
        {
            using (var inputStream = File.OpenRead(inputPath))
            using (var outputStream = File.Create(outputPath))
            {
                var csfFile = CsfFileJsonHelper.LoadFromJsonFile(inputStream);
                csfFile.WriteCsfFile(outputStream);
            }
        }

        private static void ConvertCsfToYaml(string inputPath, string outputPath)
        {
            using (var inputStream = File.OpenRead(inputPath))
            using (var outputStream = File.Create(outputPath))
            {
                var csfFile = CsfFile.LoadFromCsfFile(inputStream);
                CsfFileYamlHelper.WriteYamlFile(csfFile, outputStream);
            }
        }

        private static void ConvertYamlToCsf(string inputPath, string outputPath)
        {
            using (var inputStream = File.OpenRead(inputPath))
            using (var outputStream = File.Create(outputPath))
            {
                var csfFile = CsfFileYamlHelper.LoadFromYamlFile(inputStream);
                csfFile.WriteCsfFile(outputStream);
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("CsfStudio - CSF/INI/JSON/YAML converter for Red Alert 2");
            Console.WriteLine("Usage:");
            Console.WriteLine("  Convert between formats:");
            Console.WriteLine("    CsfStudio.exe -i input.ext -o output.ext --to-[format]");
            Console.WriteLine("      Supported formats: ini, csf, json, yaml");
            Console.WriteLine("  Merge files:");
            Console.WriteLine("    CsfStudio.exe -i file1.ext,file2.ext -o merged.ext --merge");
            Console.WriteLine("  Subtract files (remove labels present in other files):");
            Console.WriteLine("    CsfStudio.exe -i file1.ext,file2.ext -o result.ext --subtract");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -i, --input    Input file path(s), comma-separated");
            Console.WriteLine("  -o, --output   Output file path");
            Console.WriteLine("  --to-ini       Convert to INI format");
            Console.WriteLine("  --to-csf       Convert to CSF format");
            Console.WriteLine("  --to-json      Convert to JSON format");
            Console.WriteLine("  --to-yaml      Convert to YAML format");
            Console.WriteLine("  --merge        Merge multiple files");
            Console.WriteLine("  --subtract     Subtract labels present in other files");
            Console.WriteLine("  -h, --help     Show this help message");
        }
    }
}