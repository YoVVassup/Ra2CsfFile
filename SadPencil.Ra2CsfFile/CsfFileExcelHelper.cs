using System;
using System.IO;
using System.Text;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;

namespace SadPencil.Ra2CsfFile
{
    /// <summary>
    /// Helper class for working with Excel files (XLSX/XLS) representing CSF string tables.
    /// Supports reading and writing with two sheets:
    /// - "CSF_Data": contains three columns: Label Name, Value, Extra.
    /// - "Metadata": contains key-value pairs for CSF metadata (Version, Language).
    /// Extra column can be treated as UTF-8 text or Base64 according to CsfFileOptions.TreatExtraAsText.
    /// Supports label ordering via CsfFileOptions.OrderByKey.
    /// </summary>
    public static class CsfFileExcelHelper
    {
        private const string DataSheetName = "CSF_Data";
        private const string MetadataSheetName = "Metadata";

        /// <summary>
        /// Loads a CSF file from an Excel file (XLSX or XLS).
        /// Reads metadata from "Metadata" sheet if present, otherwise uses defaults.
        /// Reads label data from "CSF_Data" sheet (first row as header, data starting from row 2).
        /// Columns: A = Label Name, B = Value, C = Extra (optional).
        /// </summary>
        /// <param name="stream">Stream containing the Excel file.</param>
        /// <param name="options">Loading options. If null, default options are used.</param>
        /// <returns>Loaded CSF file.</returns>
        /// <exception cref="ArgumentNullException">If stream is null.</exception>
        /// <exception cref="InvalidDataException">If the Excel format is invalid or label names are invalid.</exception>
        public static CsfFile LoadFromExcelFile(Stream stream, CsfFileOptions options = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            options = options ?? new CsfFileOptions();
            var csf = new CsfFile(options);

            IWorkbook workbook;
            try
            {
                workbook = WorkbookFactory.Create(stream);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Failed to read Excel file. Ensure it is a valid XLSX or XLS file.", ex);
            }

            // Load metadata from Metadata sheet if present
            ISheet metadataSheet = workbook.GetSheet(MetadataSheetName);
            if (metadataSheet != null)
            {
                LoadMetadataFromSheet(csf, metadataSheet);
            }

            // Load data from CSF_Data sheet
            ISheet dataSheet = workbook.GetSheet(DataSheetName);
            if (dataSheet == null)
                throw new InvalidDataException($"Excel file must contain a sheet named '{DataSheetName}'.");

            // Assume row 0 is header, data starts at row 1
            for (int rowIdx = 1; rowIdx <= dataSheet.LastRowNum; rowIdx++)
            {
                IRow row = dataSheet.GetRow(rowIdx);
                if (row == null) continue;

                ICell labelCell = row.GetCell(0);
                if (labelCell == null) continue;

                string label = GetCellStringValue(labelCell);
                if (string.IsNullOrEmpty(label)) continue;

                if (!CsfFile.ValidateLabelName(label))
                    throw new InvalidDataException($"Invalid label name '{label}' in Excel row {rowIdx + 1}.");

                string value = GetCellStringValue(row.GetCell(1)) ?? "";
                string extraStr = GetCellStringValue(row.GetCell(2));

                byte[] extra = null;
                if (!string.IsNullOrEmpty(extraStr))
                {
                    if (options.TreatExtraAsText)
                        extra = Encoding.UTF8.GetBytes(extraStr);
                    else
                        extra = Convert.FromBase64String(extraStr);
                }

                csf.AddLabel(label, value, extra);
            }

            if (options.OrderByKey)
                csf = csf.OrderByKey();

            return csf;
        }

        /// <summary>
        /// Saves a CSF file to an Excel file.
        /// Creates two sheets: "Metadata" (version, language) and "CSF_Data" (labels).
        /// </summary>
        /// <param name="csf">CSF file to save.</param>
        /// <param name="stream">Output stream.</param>
        /// <param name="xlsx">If true, saves as XLSX (Excel 2007+); if false, saves as XLS (Excel 97-2003).</param>
        /// <exception cref="ArgumentNullException">If csf or stream is null.</exception>
        /// <exception cref="InvalidDataException">If label names contain invalid characters.</exception>
        public static void WriteExcelFile(CsfFile csf, Stream stream, bool xlsx = true)
        {
            if (csf == null) throw new ArgumentNullException(nameof(csf));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            IWorkbook workbook;
            if (xlsx)
                workbook = new XSSFWorkbook();
            else
                workbook = new HSSFWorkbook();

            // Create Metadata sheet
            ISheet metadataSheet = workbook.CreateSheet(MetadataSheetName);
            WriteMetadataToSheet(csf, metadataSheet);

            // Create Data sheet
            ISheet dataSheet = workbook.CreateSheet(DataSheetName);

            // Header row
            IRow headerRow = dataSheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("Label Name");
            headerRow.CreateCell(1).SetCellValue("Value");
            headerRow.CreateCell(2).SetCellValue("Extra");

            // Write labels in determined order
            int rowIdx = 1;
            foreach (string labelName in csf.GetLabelsInWriteOrder())
            {
                if (!csf.Labels.TryGetValue(labelName, out string labelValue))
                    continue;

                if (!CsfFile.ValidateLabelName(labelName))
                    throw new InvalidDataException($"Invalid characters in label name \"{labelName}\".");

                IRow row = dataSheet.CreateRow(rowIdx++);
                row.CreateCell(0).SetCellValue(labelName);
                row.CreateCell(1).SetCellValue(labelValue ?? "");

                byte[] extra = csf.GetExtra(labelName);
                if (extra != null)
                {
                    string extraStr;
                    if (csf.Options.TreatExtraAsText)
                        extraStr = Encoding.UTF8.GetString(extra);
                    else
                        extraStr = Convert.ToBase64String(extra);
                    row.CreateCell(2).SetCellValue(extraStr);
                }
            }

            // Auto-size columns
            for (int i = 0; i <= 2; i++)
                dataSheet.AutoSizeColumn(i);

            workbook.Write(stream);
        }

        #region Metadata Helpers

        private static void LoadMetadataFromSheet(CsfFile csf, ISheet sheet)
        {
            for (int rowIdx = 0; rowIdx <= sheet.LastRowNum; rowIdx++)
            {
                IRow row = sheet.GetRow(rowIdx);
                if (row == null) continue;

                string key = GetCellStringValue(row.GetCell(0));
                string value = GetCellStringValue(row.GetCell(1));

                if (string.IsNullOrEmpty(key)) continue;

                switch (key.ToLower())
                {
                    case "version":
                        if (int.TryParse(value, out int ver))
                            csf.Version = ver;
                        break;
                    case "language":
                        if (int.TryParse(value, out int lang))
                            csf.Language = CsfLangHelper.GetCsfLang(lang);
                        break;
                }
            }
        }

        private static void WriteMetadataToSheet(CsfFile csf, ISheet sheet)
        {
            int rowIdx = 0;
            IRow versionRow = sheet.CreateRow(rowIdx++);
            versionRow.CreateCell(0).SetCellValue("Version");
            versionRow.CreateCell(1).SetCellValue(csf.Version);

            IRow languageRow = sheet.CreateRow(rowIdx++);
            languageRow.CreateCell(0).SetCellValue("Language");
            languageRow.CreateCell(1).SetCellValue((int)csf.Language);

            sheet.AutoSizeColumn(0);
            sheet.AutoSizeColumn(1);
        }

        #endregion

        #region Cell Value Helper

        private static string GetCellStringValue(ICell cell)
        {
            if (cell == null) return null;

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    return cell.NumericCellValue.ToString();
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Formula:
                    try
                    {
                        return cell.StringCellValue;
                    }
                    catch
                    {
                        return cell.NumericCellValue.ToString();
                    }
                default:
                    return null;
            }
        }

        #endregion
    }
}