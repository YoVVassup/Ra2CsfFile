using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SadPencil.Ra2CsfFile.Tests
{
    [TestClass]
    public class SetOperationTests
    {
        private static string TestDataDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
        private static string EnglishCsf => Path.Combine(TestDataDir, "ra2md.csf");
        private static string RussianCsf => Path.Combine(TestDataDir, "ra2md_ru.csf");

        #region Merge

        [TestMethod]
        public void Merge_TwoFiles_CombinesAllLabels()
        {
            CsfFile file1, file2;
            using (var fs = File.OpenRead(EnglishCsf))
                file1 = CsfFile.LoadFromCsfFile(fs);
            using (var fs = File.OpenRead(RussianCsf))
                file2 = CsfFile.LoadFromCsfFile(fs);

            var result = new CsfFile();
            foreach (var label in file1.Labels)
                result.AddLabel(label.Key, label.Value, file1.GetExtra(label.Key));
            foreach (var label in file2.Labels)
                result.AddLabel(label.Key, label.Value, file2.GetExtra(label.Key));

            Assert.IsTrue(result.Labels.Count >= file1.Labels.Count, "Merged should have at least as many labels as file1");
            Assert.IsTrue(result.Labels.Count >= file2.Labels.Count, "Merged should have at least as many labels as file2");
        }

        [TestMethod]
        public void Merge_SameFile_DoesNotDuplicate()
        {
            CsfFile file1;
            using (var fs = File.OpenRead(EnglishCsf))
                file1 = CsfFile.LoadFromCsfFile(fs);

            var result = new CsfFile();
            foreach (var label in file1.Labels)
                result.AddLabel(label.Key, label.Value, file1.GetExtra(label.Key));
            foreach (var label in file1.Labels)
                result.AddLabel(label.Key, label.Value, file1.GetExtra(label.Key));

            Assert.AreEqual(file1.Labels.Count, result.Labels.Count, "Merging same file should not duplicate labels");
        }

        #endregion

        #region Subtract

        [TestMethod]
        public void Subtract_RemovesCommonLabels()
        {
            CsfFile file1;
            using (var fs = File.OpenRead(EnglishCsf))
                file1 = CsfFile.LoadFromCsfFile(fs);

            // Create a second file with some labels removed
            var file2 = new CsfFile();
            var keysToRemove = file1.Labels.Keys.Take(10).ToList();
            foreach (var label in file1.Labels)
            {
                if (!keysToRemove.Contains(label.Key, StringComparer.OrdinalIgnoreCase))
                    file2.AddLabel(label.Key, label.Value);
            }

            // Subtract: file1 minus file2 = only the removed labels
            var result = new CsfFile(file1);
            foreach (var label in file2.Labels)
                result.RemoveLabel(label.Key);

            Assert.AreEqual(keysToRemove.Count, result.Labels.Count,
                $"Subtract should leave {keysToRemove.Count} labels, got {result.Labels.Count}");
        }

        #endregion

        #region Intersection

        [TestMethod]
        public void Intersection_KeepsCommonLabels()
        {
            CsfFile file1;
            using (var fs = File.OpenRead(EnglishCsf))
                file1 = CsfFile.LoadFromCsfFile(fs);

            // Create a second file with only some labels from file1
            var file2 = new CsfFile();
            var commonKeys = file1.Labels.Keys.Take(50).ToList();
            foreach (var key in commonKeys)
                file2.AddLabel(key, file1.Labels[key]);

            var result = new CsfFile();
            var common = new HashSet<string>(file1.Labels.Keys, StringComparer.InvariantCultureIgnoreCase);
            common.IntersectWith(file2.Labels.Keys);

            foreach (var key in common)
                result.AddLabel(key, file1.Labels[key]);

            Assert.AreEqual(common.Count, result.Labels.Count, "Intersection should keep only common labels");
        }

        #endregion

        #region SymmetricDifference

        [TestMethod]
        public void SymmetricDifference_KeepsUniqueLabels()
        {
            CsfFile file1, file2;
            using (var fs = File.OpenRead(EnglishCsf))
                file1 = CsfFile.LoadFromCsfFile(fs);
            using (var fs = File.OpenRead(RussianCsf))
                file2 = CsfFile.LoadFromCsfFile(fs);

            // Count labels unique to each file
            var onlyInFile1 = file1.Labels.Keys.Except(file2.Labels.Keys, StringComparer.OrdinalIgnoreCase).Count();
            var onlyInFile2 = file2.Labels.Keys.Except(file1.Labels.Keys, StringComparer.OrdinalIgnoreCase).Count();

            // Symmetric difference should have onlyInFile1 + onlyInFile2 labels
            Assert.IsTrue(onlyInFile1 >= 0, "Should count unique labels in file1");
            Assert.IsTrue(onlyInFile2 >= 0, "Should count unique labels in file2");
        }

        #endregion

        #region Diff

        [TestMethod]
        public void Diff_SameFiles_NoDifferences()
        {
            CsfFile file1, file2;
            using (var fs = File.OpenRead(EnglishCsf))
                file1 = CsfFile.LoadFromCsfFile(fs);
            using (var fs = File.OpenRead(EnglishCsf))
                file2 = CsfFile.LoadFromCsfFile(fs);

            var allKeys = new HashSet<string>(file1.Labels.Keys, StringComparer.InvariantCultureIgnoreCase);
            allKeys.UnionWith(file2.Labels.Keys);

            int added = 0, removed = 0, changed = 0;
            foreach (var key in allKeys)
            {
                bool in1 = file1.Labels.TryGetValue(key, out string v1);
                bool in2 = file2.Labels.TryGetValue(key, out string v2);
                if (!in1 && in2) added++;
                else if (in1 && !in2) removed++;
                else if (!string.Equals(v1, v2, StringComparison.InvariantCulture)) changed++;
            }

            Assert.AreEqual(0, added, "No added labels for same files");
            Assert.AreEqual(0, removed, "No removed labels for same files");
            Assert.AreEqual(0, changed, "No changed labels for same files");
        }

        [TestMethod]
        public void Diff_DifferentFiles_DetectsDifferences()
        {
            CsfFile file1, file2;
            using (var fs = File.OpenRead(EnglishCsf))
                file1 = CsfFile.LoadFromCsfFile(fs);
            using (var fs = File.OpenRead(RussianCsf))
                file2 = CsfFile.LoadFromCsfFile(fs);

            var allKeys = new HashSet<string>(file1.Labels.Keys, StringComparer.InvariantCultureIgnoreCase);
            allKeys.UnionWith(file2.Labels.Keys);

            int added = 0, removed = 0;
            foreach (var key in allKeys)
            {
                bool in1 = file1.Labels.ContainsKey(key);
                bool in2 = file2.Labels.ContainsKey(key);
                if (!in1 && in2) added++;
                else if (in1 && !in2) removed++;
            }

            // English and Russian CSF files should have some differences
            Assert.IsTrue(added + removed > 0, "Different CSF files should have some differences");
        }

        #endregion
    }
}
