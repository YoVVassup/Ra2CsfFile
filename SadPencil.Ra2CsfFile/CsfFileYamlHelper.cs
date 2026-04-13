using YamlDotNet.Serialization;
using YamlDotNet.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Helper class for working with YAML files representing CSF string tables.
    /// Supports extra data (WRTS) as a Base64-encoded string or plain text.
    /// Supports label ordering via CsfFileOptions.OrderByKey.
    /// </summary>
    public static class CsfFileYamlHelper
    {
        private class YamlCsfModel
        {
            [YamlMember(Alias = "version")]
            public int CSFVersion { get; set; }
            
            [YamlMember(Alias = "language")]
            public int Language { get; set; }
            
            [YamlMember(Alias = "version_yaml")]
            public string YamlVersion { get; set; } = "1.2";

            [YamlMember(Alias = "labels")]
            public Dictionary<string, YamlLabel> Labels { get; set; }
        }

        private class YamlLabel
        {
            [YamlMember(Alias = "value")]
            public string Value { get; set; }

            [YamlMember(Alias = "extra", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
            public string Extra { get; set; }
        }

        // Custom emitter for multi-line strings
        private class MultilineStringEventEmitter : YamlDotNet.Serialization.EventEmitters.ChainedEventEmitter
        {
            public MultilineStringEventEmitter(IEventEmitter nextEmitter)
                : base(nextEmitter)
            {
            }

            public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
            {
                if (eventInfo.Source.Type == typeof(string) && 
                    eventInfo.Source.Value is string value && 
                    value != null && 
                    value.Contains("\n"))
                {
                    eventInfo.Style = ScalarStyle.Literal;
                }
                
                base.Emit(eventInfo, emitter);
            }
        }

        private static readonly ISerializer _serializer = new SerializerBuilder()
            .WithEventEmitter(next => new MultilineStringEventEmitter(next))
            .Build();

        private static readonly IDeserializer _deserializer = new DeserializerBuilder()
            .Build();

        /// <summary>
        /// Loads a CSF file from a YAML representation.
        /// </summary>
        /// <param name="stream">Stream with YAML file.</param>
        /// <param name="options">Loading options. If null, default options are used.</param>
        /// <returns>Loaded CSF file.</returns>
        /// <exception cref="ArgumentNullException">If the stream is null.</exception>
        /// <exception cref="InvalidDataException">If the YAML format is incorrect.</exception>
        public static CsfFile LoadFromYamlFile(Stream stream, CsfFileOptions options = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            options = options ?? new CsfFileOptions();
            var csf = new CsfFile(options);

            try
            {
                using (var sr = new StreamReader(stream, Encoding.UTF8, true, 1024))
                {
                    var yaml = sr.ReadToEnd();
                    var model = _deserializer.Deserialize<YamlCsfModel>(yaml);

                    if (model == null)
                        throw new InvalidDataException("YAML deserialization returned null.");

                    csf.Version = model.CSFVersion;
                    
                    if (model.YamlVersion != "1.2")
                    {
                        throw new InvalidDataException($"Unsupported YAML version: {model.YamlVersion}. Only version 1.2 is supported.");
                    }
                    
                    csf.Language = CsfLangHelper.GetCsfLang(model.Language);

                    foreach (var labelPair in model.Labels)
                    {
                        string labelName = labelPair.Key;
                        string labelValue = labelPair.Value?.Value ?? "";
                        string extraStr = labelPair.Value?.Extra;

                        if (!CsfFile.ValidateLabelName(labelName))
                            throw new InvalidDataException($"Invalid label name '{labelName}' in YAML.");

                        byte[] extra = null;
                        if (!string.IsNullOrEmpty(extraStr))
                        {
                            if (options.TreatExtraAsText)
                                extra = Encoding.UTF8.GetBytes(extraStr);
                            else
                                extra = Convert.FromBase64String(extraStr);
                        }

                        csf.AddLabel(labelName, labelValue, extra);
                    }
                }
            }
            catch (YamlException ex)
            {
                throw new InvalidDataException("Invalid YAML format.", ex);
            }

            if (options.OrderByKey)
                csf = csf.OrderByKey();

            return csf;
        }

        /// <summary>
        /// Saves a CSF file to a YAML representation.
        /// </summary>
        /// <param name="csf">CSF file to save.</param>
        /// <param name="stream">Stream for writing.</param>
        /// <exception cref="ArgumentNullException">If the CSF file or stream is null.</exception>
        public static void WriteYamlFile(CsfFile csf, Stream stream)
        {
            if (csf == null) throw new ArgumentNullException(nameof(csf));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var model = new YamlCsfModel
            {
                CSFVersion = csf.Version,
                Language = (int)csf.Language,
                YamlVersion = "1.2",
                Labels = new Dictionary<string, YamlLabel>()
            };

            // Write labels in determined order
            foreach (string labelName in csf.GetLabelsInWriteOrder())
            {
                if (!csf.Labels.TryGetValue(labelName, out string labelValue))
                    continue;

                var yamlLabel = new YamlLabel
                {
                    Value = labelValue ?? ""
                };

                byte[] extra = csf.GetExtra(labelName);
                if (extra != null)
                {
                    if (csf.Options.TreatExtraAsText)
                        yamlLabel.Extra = Encoding.UTF8.GetString(extra);
                    else
                        yamlLabel.Extra = Convert.ToBase64String(extra);
                }

                model.Labels.Add(labelName, yamlLabel);
            }

            using (var sw = new StreamWriter(stream, Encoding.UTF8, 1024))
            {
                var yaml = _serializer.Serialize(model);
                sw.Write(yaml);
            }
        }
    }
}