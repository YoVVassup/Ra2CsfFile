using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Helper class for working with JSON files representing CSF string tables.
    /// </summary>
    public static class CsfFileJsonHelper
    {
        private class JsonCsfModel
        {
            [JsonProperty("version")]
            public int Version { get; set; }

            [JsonProperty("language")]
            public int Language { get; set; }

            [JsonProperty("labels")]
            public Dictionary<string, string> Labels { get; set; }
        }

        /// <summary>
        /// Loads a CSF file from a JSON representation.
        /// </summary>
        public static CsfFile LoadFromJsonFile(Stream stream, CsfFileOptions options = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            options = options ?? new CsfFileOptions();
            var csf = new CsfFile(options);

            try
            {
                using (var sr = new StreamReader(stream, Encoding.UTF8, true, 1024))
                {
                    var json = sr.ReadToEnd();
                    var model = JsonConvert.DeserializeObject<JsonCsfModel>(json);

                    csf.Version = model.Version;
                    csf.Language = CsfLangHelper.GetCsfLang(model.Language);

                    foreach (var label in model.Labels)
                    {
                        csf.AddLabel(label.Key, label.Value);
                    }
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException("Invalid JSON format.", ex);
            }

            return csf;
        }

        /// <summary>
        /// Saves a CSF file to a JSON representation.
        /// </summary>
        public static void WriteJsonFile(CsfFile csf, Stream stream)
        {
            if (csf == null) throw new ArgumentNullException(nameof(csf));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var model = new JsonCsfModel
            {
                Version = csf.Version,
                Language = (int)csf.Language,
                Labels = new Dictionary<string, string>(csf.Labels)
            };

            using (var sw = new StreamWriter(stream, Encoding.UTF8, 1024))
            {
                var json = JsonConvert.SerializeObject(model, Formatting.Indented);
                sw.Write(json);
            }
        }
    }
}