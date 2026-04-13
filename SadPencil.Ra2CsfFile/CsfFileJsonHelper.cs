using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Helper class for working with JSON files representing CSF string tables.
    /// Supports extra data (WRTS) as a Base64-encoded string or plain text.
    /// Supports label ordering via CsfFileOptions.OrderByKey.
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
            public Dictionary<string, JsonLabel> Labels { get; set; }
        }

        private class JsonLabel
        {
            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("extra", NullValueHandling = NullValueHandling.Ignore)]
            public string Extra { get; set; }
        }

        /// <summary>
        /// Loads a CSF file from a JSON representation.
        /// </summary>
        /// <param name="stream">Stream with JSON file.</param>
        /// <param name="options">Loading options. If null, default options are used.</param>
        /// <returns>Loaded CSF file.</returns>
        /// <exception cref="ArgumentNullException">If the stream is null.</exception>
        /// <exception cref="InvalidDataException">If the JSON format is incorrect.</exception>
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

                    if (model == null)
                        throw new InvalidDataException("JSON deserialization returned null.");

                    csf.Version = model.Version;
                    csf.Language = CsfLangHelper.GetCsfLang(model.Language);

                    foreach (var labelPair in model.Labels)
                    {
                        string labelName = labelPair.Key;
                        string labelValue = labelPair.Value?.Value ?? "";
                        string extraStr = labelPair.Value?.Extra;

                        if (!CsfFile.ValidateLabelName(labelName))
                            throw new InvalidDataException($"Invalid label name '{labelName}' in JSON.");

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
            catch (JsonException ex)
            {
                throw new InvalidDataException("Invalid JSON format.", ex);
            }

            if (options.OrderByKey)
                csf = csf.OrderByKey();

            return csf;
        }

        /// <summary>
        /// Saves a CSF file to a JSON representation.
        /// </summary>
        /// <param name="csf">CSF file to save.</param>
        /// <param name="stream">Stream for writing.</param>
        /// <exception cref="ArgumentNullException">If the CSF file or stream is null.</exception>
        public static void WriteJsonFile(CsfFile csf, Stream stream)
        {
            if (csf == null) throw new ArgumentNullException(nameof(csf));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var model = new JsonCsfModel
            {
                Version = csf.Version,
                Language = (int)csf.Language,
                Labels = new Dictionary<string, JsonLabel>()
            };

            // Write labels in determined order
            foreach (string labelName in csf.GetLabelsInWriteOrder())
            {
                if (!csf.Labels.TryGetValue(labelName, out string labelValue))
                    continue;

                var jsonLabel = new JsonLabel
                {
                    Value = labelValue ?? ""
                };

                byte[] extra = csf.GetExtra(labelName);
                if (extra != null)
                {
                    if (csf.Options.TreatExtraAsText)
                        jsonLabel.Extra = Encoding.UTF8.GetString(extra);
                    else
                        jsonLabel.Extra = Convert.ToBase64String(extra);
                }

                model.Labels.Add(labelName, jsonLabel);
            }

            using (var sw = new StreamWriter(stream, Encoding.UTF8, 1024))
            {
                var json = JsonConvert.SerializeObject(model, Formatting.Indented);
                sw.Write(json);
            }
        }
    }
}