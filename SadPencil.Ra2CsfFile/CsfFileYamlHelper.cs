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
            public Dictionary<string, string> Labels { get; set; }
        }

        // Caste emitter for multi -line lines
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

                    csf.Version = model.CSFVersion;
					// Checking the version of YAML
                    if (model.YamlVersion != "1.2")
                    {
                        throw new InvalidDataException($"Unsupported YAML version: {model.YamlVersion}. Only version 1.2 is supported.");
                    }
                    csf.Language = CsfLangHelper.GetCsfLang(model.Language);

                    foreach (var label in model.Labels)
                    {
                        csf.AddLabel(label.Key, label.Value);
                    }
                }
            }
            catch (YamlDotNet.Core.YamlException ex)
            {
                throw new InvalidDataException("Invalid YAML format.", ex);
            }

            return csf;
        }

        /// <summary>
        /// Saves a CSF file to a YAML representation.
        /// </summary>
        public static void WriteYamlFile(CsfFile csf, Stream stream)
        {
            if (csf == null) throw new ArgumentNullException(nameof(csf));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var model = new YamlCsfModel
            {
                CSFVersion = csf.Version,
                Language = (int)csf.Language,
				YamlVersion = "1.2",
                Labels = new Dictionary<string, string>(csf.Labels)
            };

            using (var sw = new StreamWriter(stream, Encoding.UTF8, 1024))
            {
                var yaml = _serializer.Serialize(model);
                sw.Write(yaml);
            }
        }
    }
}