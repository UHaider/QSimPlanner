﻿using QSP.LibraryExtension;
using NUnit.Framework;
using System;
using System.Linq;
using static QSP.LibraryExtension.Strings;

namespace UnitTest.LibraryExtension
{
    [TestFixture]
    public class StringsTest
    {
        [Test]
        public void CastTest()
        {
            Assert.AreEqual("testabc", new string("testabc".CastStr().ToArray()));
        }

        [Test]
        public void ReplaceAnyTest()
        {
            Assert.IsTrue("".ReplaceAny("01", "abc") == "");
            Assert.IsTrue("0123".ReplaceAny("013", "ab") == "abab2ab");
            Assert.IsTrue("0123".ReplaceAny("3130", "ab") == "abab2ab");
        }

        [Test]
        public void RemoveHtmlTagsTest()
        {
            Assert.IsTrue("<shouldRemoveThis>".RemoveHtmlTags() == "");
            Assert.IsTrue("123<456>789".RemoveHtmlTags() == "123789");
        }

        [Test]
        public void ShiftToRightValidCount()
        {
            string s = @"123
456
789";

            string expected = @"   123
   456
   789";

            Assert.IsTrue(s.ShiftToRight(3) == expected);
        }

        [Test]
        public void ShiftToRightInvalidCount()
        {
            string s = @"123";

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            s.ShiftToRight(-5));
        }

        [Test]
        public void TrimEmptyLinesTest()
        {
            var s1 = "123";
            Assert.IsTrue(s1 == s1.TrimEmptyLines());

            var s2 = "  \n\t\n  \n456\n \n789\n\t";
            Assert.IsTrue("456\n \n789" == s2.TrimEmptyLines());
        }

        [Test]
        public void EscapeCommandLineArgTest()
        {
            Assert.AreEqual("\"123 456\"", EscapeCommandLineArg("123 456"));
            Assert.AreEqual("\"a\"\"\"\"b\"", EscapeCommandLineArg("a\"\"b"));
        }

        [Test]
        public void IsDoubleTest()
        {
            Assert.True("15".IsDouble());
            Assert.True("-1.5".IsDouble());
            Assert.True(double.PositiveInfinity.ToString().IsDouble());
            Assert.True(double.NaN.ToString().IsDouble());
            Assert.False("15x".IsDouble());
        }

        [Test]
        public void IsFiniteDoubleTest()
        {
            Assert.True("15".IsFiniteDouble());
            Assert.True("-1.5".IsFiniteDouble());
            Assert.False(double.PositiveInfinity.ToString().IsFiniteDouble());
            Assert.False(double.NaN.ToString().IsFiniteDouble());
            Assert.False("15x".IsFiniteDouble());
        }
    }
}
