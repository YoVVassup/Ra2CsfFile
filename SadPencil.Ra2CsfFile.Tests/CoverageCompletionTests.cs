using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SadPencil.Ra2CsfFile.Tests
{
    [TestClass]
    public class ValidateLengthsTests
    {
        [TestMethod]
        public void ValidateLengths_NormalLabels_NoWarnings()
        {
            var csf = new CsfFile();
            csf.AddLabel("NORMAL_LABEL", "Normal value");
            csf.AddLabel("ANOTHER:LABEL", "Another value");

            var warnings = csf.ValidateLengths();
            Assert.AreEqual(0, warnings.Count, "Normal labels should have no warnings");
        }

        [TestMethod]
        public void ValidateLengths_LongLabelName_Warns()
        {
            var csf = new CsfFile();
            string longName = new string('X', 70000); // > 64KB
            csf.AddLabel(longName, "value");

            var warnings = csf.ValidateLengths();
            Assert.IsTrue(warnings.Count > 0, "Long label name should produce warning");
            Assert.IsTrue(warnings.Any(w => w.Contains("exceeds")), "Warning should mention 'exceeds'");
        }

        [TestMethod]
        public void ValidateLengths_LongValue_Warns()
        {
            var csf = new CsfFile();
            string longValue = new string('A', 1100000); // > 1M chars
            csf.AddLabel("LONG_VALUE", longValue);

            var warnings = csf.ValidateLengths();
            Assert.IsTrue(warnings.Count > 0, "Long value should produce warning");
            Assert.IsTrue(warnings.Any(w => w.Contains("chars")), "Warning should mention 'chars'");
        }

        [TestMethod]
        public void ValidateLengths_LongExtra_Warns()
        {
            var csf = new CsfFile();
            byte[] longExtra = new byte[70000]; // > 64KB
            csf.AddLabel("LONG_EXTRA", "value", longExtra);

            var warnings = csf.ValidateLengths();
            Assert.IsTrue(warnings.Count > 0, "Long extra value should produce warning");
            Assert.IsTrue(warnings.Any(w => w.Contains("Extra value")), "Warning should mention 'Extra value'");
        }

        [TestMethod]
        public void ValidateLengths_MultipleIssues_ListsAll()
        {
            var csf = new CsfFile();
            csf.AddLabel(new string('X', 70000), new string('A', 1100000), new byte[70000]);

            var warnings = csf.ValidateLengths();
            Assert.IsTrue(warnings.Count >= 3, "Should have warnings for name, value, and extra");
        }

        [TestMethod]
        public void ValidateLengths_EmptyCsf_NoWarnings()
        {
            var csf = new CsfFile();
            var warnings = csf.ValidateLengths();
            Assert.AreEqual(0, warnings.Count, "Empty CSF should have no warnings");
        }

        [TestMethod]
        public void ValidateLengths_BoundaryValues_NoWarnings()
        {
            var csf = new CsfFile();
            // Just under thresholds
            csf.AddLabel(new string('X', 65535), new string('A', 1048575), new byte[65535]);

            var warnings = csf.ValidateLengths();
            Assert.AreEqual(0, warnings.Count, "Boundary values should have no warnings");
        }
    }

    [TestClass]
    public class Encoding1252Tests
    {
        [TestMethod]
        public void ConvertsEncoding1252ToUnicode_HandlesChars128To159()
        {
            // Characters 128-159 in Windows-1252 map to specific Unicode code points
            var input = new StringBuilder();
            input.Append('\u0080'); // Euro sign € → should map to Windows-1252 0x80
            input.Append('\u0099'); // ™ → should map to Windows-1252 0x99

            string result = Encoding1252Workaround.ConvertsUnicodeToEncoding1252(input.ToString());
            // The result should contain the Windows-1252 byte values as characters
            Assert.IsTrue(result.Length > 0, "Should produce output");
        }

        [TestMethod]
        public void ConvertsUnicodeToEncoding1252_HandlesRegularChars()
        {
            string input = "Hello World 123";
            string result = Encoding1252Workaround.ConvertsUnicodeToEncoding1252(input);
            Assert.AreEqual(input, result, "Regular ASCII chars should pass through unchanged");
        }

        [TestMethod]
        public void ConvertsUnicodeToEncoding1252_NullInput_ReturnsNull()
        {
            string result = Encoding1252Workaround.ConvertsUnicodeToEncoding1252(null);
            Assert.IsNull(result, "Null input should return null");
        }

        [TestMethod]
        public void ConvertsEncoding1252ToUnicode_HandlesRegularChars()
        {
            string input = "Hello World 123";
            string result = Encoding1252Workaround.ConvertsEncoding1252ToUnicode(input);
            Assert.AreEqual(input, result, "Regular ASCII chars should pass through unchanged");
        }

        [TestMethod]
        public void ConvertsEncoding1252ToUnicode_NullInput_ReturnsNull()
        {
            string result = Encoding1252Workaround.ConvertsEncoding1252ToUnicode(null);
            Assert.IsNull(result, "Null input should return null");
        }

        [TestMethod]
        public void Roundtrip_NormalChars_NoChange()
        {
            string original = "Test string with no special chars";
            string encoded = Encoding1252Workaround.ConvertsUnicodeToEncoding1252(original);
            string decoded = Encoding1252Workaround.ConvertsEncoding1252ToUnicode(encoded);
            Assert.AreEqual(original, decoded, "Roundtrip should preserve normal characters");
        }

        [TestMethod]
        public void Encoding1252ToUnicode_MappingTable_CorrectCount()
        {
            Assert.AreEqual(27, Encoding1252Workaround.Encoding1252ToUnicode.Count,
                "Should have 27 mapped characters");
        }

        [TestMethod]
        public void UnicodeToEncoding1252_MappingTable_CorrectCount()
        {
            Assert.AreEqual(27, Encoding1252Workaround.UnicodeToEncoding1252.Count,
                "Should have 27 mapped characters");
        }

        [TestMethod]
        public void Encoding1252ToUnicode_EuroSign_MapsCorrectly()
        {
            // Euro sign (U+20AC) should map to 0x80 in Windows-1252
            Assert.IsTrue(Encoding1252Workaround.Encoding1252ToUnicode.ContainsKey('\u20AC'),
                "Should map Euro sign");
            Assert.AreEqual('\u0080', Encoding1252Workaround.Encoding1252ToUnicode['\u20AC'],
                "Euro sign should map to 0x80");
        }

        [TestMethod]
        public void UnicodeToEncoding1252_TrademarkSign_MapsCorrectly()
        {
            // 0x99 in Windows-1252 should map to ™ (U+2122) in Unicode
            Assert.IsTrue(Encoding1252Workaround.UnicodeToEncoding1252.ContainsKey('\u0099'),
                "Should map 0x99");
            Assert.AreEqual('\u2122', Encoding1252Workaround.UnicodeToEncoding1252['\u0099'],
                "0x99 should map to trademark sign");
        }
    }

    [TestClass]
    public class GetHashCodeTests
    {
        [TestMethod]
        public void GetHashCode_SameObjects_SameHash()
        {
            var csf1 = new CsfFile();
            csf1.AddLabel("TEST", "Value");
            var csf2 = new CsfFile();
            csf2.AddLabel("TEST", "Value");

            Assert.AreEqual(csf1.GetHashCode(), csf2.GetHashCode(),
                "Equal CsfFile objects should have same hash code");
        }

        [TestMethod]
        public void GetHashCode_DifferentObjects_LikelyDifferentHash()
        {
            var csf1 = new CsfFile();
            csf1.AddLabel("TEST1", "Value1");
            var csf2 = new CsfFile();
            csf2.AddLabel("TEST2", "Value2");

            // Not guaranteed but very likely
            Assert.AreNotEqual(csf1.GetHashCode(), csf2.GetHashCode(),
                "Different CsfFile objects should likely have different hash codes");
        }

        [TestMethod]
        public void GetHashCode_EmptyCsf_Deterministic()
        {
            var csf = new CsfFile();
            int hash1 = csf.GetHashCode();
            int hash2 = csf.GetHashCode();
            Assert.AreEqual(hash1, hash2, "Same object should return same hash code");
        }

        [TestMethod]
        public void GetHashCode_CsfFileOptions_SameHash()
        {
            var opt1 = new CsfFileOptions { OrderByKey = true };
            var opt2 = new CsfFileOptions { OrderByKey = true };
            Assert.AreEqual(opt1.GetHashCode(), opt2.GetHashCode(),
                "Equal CsfFileOptions should have same hash code");
        }

        [TestMethod]
        public void GetHashCode_CsfFileOptions_DifferentHash()
        {
            var opt1 = new CsfFileOptions { OrderByKey = true };
            var opt2 = new CsfFileOptions { OrderByKey = false };
            Assert.AreNotEqual(opt1.GetHashCode(), opt2.GetHashCode(),
                "Different CsfFileOptions should have different hash codes");
        }
    }

    [TestClass]
    public class TrimMultiLineTests
    {
        [TestMethod]
        public void TrimMultiLine_SingleLine_NoChange()
        {
            string result = CsfFileIniHelper.TrimMultiLine("Hello World");
            Assert.AreEqual("Hello World", result);
        }

        [TestMethod]
        public void TrimMultiLine_MultiLine_TrimsEachLine()
        {
            string input = "  Line1  \n  Line2  \n  Line3  ";
            string result = CsfFileIniHelper.TrimMultiLine(input);
            Assert.AreEqual("Line1\nLine2\nLine3", result);
        }

        [TestMethod]
        public void TrimMultiLine_NullInput_ReturnsNull()
        {
            string result = CsfFileIniHelper.TrimMultiLine(null);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TrimMultiLine_EmptyInput_ReturnsEmpty()
        {
            string result = CsfFileIniHelper.TrimMultiLine("");
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void TrimMultiLine_LeadingTrailingWhitespace_Trims()
        {
            string input = "  Hello  ";
            string result = CsfFileIniHelper.TrimMultiLine(input);
            Assert.AreEqual("Hello", result);
        }
    }
}
