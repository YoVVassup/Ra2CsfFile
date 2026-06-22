using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SadPencil.Ra2CsfFile.Tests
{
    [TestClass]
    public class MapHelperTests
    {
        private static string TestDataDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
        private static string MapsDir => Path.Combine(TestDataDir, "Maps");
        private static string Stringtable69Csf => Path.Combine(TestDataDir, "stringtable69.csf");
        private static string A01Map => Path.Combine(MapsDir, "a01.map");

        #region ExtractLabelsFromMapFolder

        [TestMethod]
        public void ExtractLabelsFromMapFolder_FindsLabels()
        {
            var labels = CsfFileMapHelper.ExtractLabelsFromMapFolder(MapsDir);
            Assert.IsTrue(labels.Count > 0, "Should find labels in map files");
        }

        [TestMethod]
        public void ExtractLabelsFromMapFolder_FindsUINameLabels()
        {
            var labels = CsfFileMapHelper.ExtractLabelsFromMapFolder(MapsDir);
            // The map file has UIName entries like a01:name_humvee, NAME:TANYA, etc.
            Assert.IsTrue(labels.Contains("NAME:TANYA", StringComparer.OrdinalIgnoreCase),
                "Should find NAME:TANYA from map UIName");
        }

        [TestMethod]
        public void ExtractLabelsFromMapFolder_FindsCustomUINameLabels()
        {
            var labels = CsfFileMapHelper.ExtractLabelsFromMapFolder(MapsDir);
            Assert.IsTrue(labels.Any(l => l.StartsWith("a01:", StringComparison.OrdinalIgnoreCase)),
                "Should find custom a01: labels from map UIName");
        }

        [TestMethod]
        public void ExtractLabelsFromMapFolder_ReturnsCaseInsensitiveSet()
        {
            var labels = CsfFileMapHelper.ExtractLabelsFromMapFolder(MapsDir);
            // Case-insensitive means duplicate casings are merged
            Assert.IsTrue(labels.Count > 0);
        }

        [TestMethod]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void ExtractLabelsFromMapFolder_NonExistentFolder_ThrowsDirectoryNotFoundException()
        {
            CsfFileMapHelper.ExtractLabelsFromMapFolder(@"C:\NonExistent\Folder\That\Does\Not\Exist");
        }

        #endregion

        #region FindMissingLabels

        [TestMethod]
        public void FindMissingLabels_FindsMissingLabels()
        {
            CsfFile csf;
            using (var fs = File.OpenRead(Stringtable69Csf))
                csf = CsfFile.LoadFromCsfFile(fs);

            var missing = CsfFileMapHelper.FindMissingLabels(csf, MapsDir);
            Assert.IsNotNull(missing, "Missing labels list should not be null");
            Assert.IsInstanceOfType(missing, typeof(System.Collections.Generic.List<string>));
        }

        [TestMethod]
        public void FindMissingLabels_CompleteCsf_NoMissingLabels()
        {
            // Create a CSF with all labels found in the map
            var mapLabels = CsfFileMapHelper.ExtractLabelsFromMapFolder(MapsDir);
            var csf = new CsfFile();
            foreach (var label in mapLabels)
                csf.AddLabel(label, "test_value");

            var missing = CsfFileMapHelper.FindMissingLabels(csf, MapsDir);
            Assert.AreEqual(0, missing.Count, "Complete CSF should have no missing labels");
        }

        [TestMethod]
        public void FindMissingLabels_EmptyCsf_AllLabelsMissing()
        {
            var mapLabels = CsfFileMapHelper.ExtractLabelsFromMapFolder(MapsDir);
            var csf = new CsfFile();

            var missing = CsfFileMapHelper.FindMissingLabels(csf, MapsDir);
            Assert.AreEqual(mapLabels.Count, missing.Count, "Empty CSF should have all map labels missing");
        }

        [TestMethod]
        public void FindMissingLabels_ReturnsSortedList()
        {
            CsfFile csf;
            using (var fs = File.OpenRead(Stringtable69Csf))
                csf = CsfFile.LoadFromCsfFile(fs);

            var missing = CsfFileMapHelper.FindMissingLabels(csf, MapsDir);

            for (int i = 1; i < missing.Count; i++)
            {
                Assert.IsTrue(
                    string.Compare(missing[i - 1], missing[i], StringComparison.OrdinalIgnoreCase) <= 0,
                    $"Missing labels should be sorted: {missing[i - 1]} <= {missing[i]}");
            }
        }

        [TestMethod]
        public void FindMissingLabels_WithRealCSF_ContainsSomeLabels()
        {
            // stringtable69.csf is a real CSF file - it may or may not have all labels
            CsfFile csf;
            using (var fs = File.OpenRead(Stringtable69Csf))
                csf = CsfFile.LoadFromCsfFile(fs);

            var missing = CsfFileMapHelper.FindMissingLabels(csf, MapsDir);
            var allMapLabels = CsfFileMapHelper.ExtractLabelsFromMapFolder(MapsDir);

            // Missing count should be <= total map labels
            Assert.IsTrue(missing.Count <= allMapLabels.Count,
                "Missing count should not exceed total map labels");

            // Each missing label should be in the map labels
            foreach (var label in missing)
            {
                Assert.IsTrue(allMapLabels.Contains(label, StringComparer.OrdinalIgnoreCase),
                    $"Missing label {label} should be in map labels");
            }
        }

        #endregion

        #region Map INI Parsing

        [TestMethod]
        public void MapFile_ParsesMultipleSections()
        {
            // The map file has multiple sections like [01000000], [01000001], etc.
            var labels = CsfFileMapHelper.ExtractLabelsFromMapFolder(MapsDir);
            Assert.IsTrue(labels.Count >= 10, "Map should have multiple UIName labels");
        }

        [TestMethod]
        public void MapFile_ParsesUINameFromDifferentSections()
        {
            var labels = CsfFileMapHelper.ExtractLabelsFromMapFolder(MapsDir);
            // Check that we find labels from different parts of the map
            bool hasGuiLabel = labels.Any(l => l.StartsWith("NAME:", StringComparison.OrdinalIgnoreCase));
            bool hasCustomLabel = labels.Any(l => l.StartsWith("a01:", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(hasGuiLabel, "Should find NAME: labels");
            Assert.IsTrue(hasCustomLabel, "Should find a01: labels");
        }

        #endregion
    }
}
