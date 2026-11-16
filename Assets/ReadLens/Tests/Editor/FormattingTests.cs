using NUnit.Framework;
using ReadLens.Core;

namespace ReadLens.Tests
{
    public class FormattingTests
    {
        [TestCase("table", "ta|ble")]
        [TestCase("reading", "rea|ding")]
        [TestCase("children", "chil|dren")]
        [TestCase("happy", "hap|py")]
        [TestCase("make", "make")]
        [TestCase("jumped", "jumped")]
        [TestCase("wanted", "wan|ted")]
        [TestCase("elephant", "e|le|phant")]
        [TestCase("boxes", "box|es")]
        [TestCase("Cat", "Cat")]
        public void SplitsEnglish(string word, string expected) =>
            Assert.AreEqual(expected, string.Join("|", Syllabifier.Split(word, "en")));

        [TestCase("maison", "mai|son")]
        [TestCase("chocolat", "cho|co|lat")]
        [TestCase("table", "ta|ble")]
        [TestCase("quatre", "qua|tre")]
        [TestCase("école", "é|co|le")]
        public void SplitsFrench(string word, string expected) =>
            Assert.AreEqual(expected, string.Join("|", Syllabifier.Split(word, "fr")));

        [Test]
        public void PlainProfileLeavesTextUnchanged()
        {
            var p = new ReadingProfile { colorMode = ColorMode.None, highlightConfusables = false };
            Assert.AreEqual("Hello, world! It's 9.", TextFormatter.ToRichText("Hello, world! It's 9.", p));
        }

        [Test]
        public void SyllablesAlternateColours()
        {
            var p = new ReadingProfile { palette = new[] { "#A", "#B" }, highlightConfusables = false };
            Assert.AreEqual("<color=#A>ta</color><color=#B>ble</color> <color=#A>cat</color>", TextFormatter.ToRichText("table cat", p));
        }

        [Test]
        public void HighlightsConfusableLetters()
        {
            var p = new ReadingProfile { colorMode = ColorMode.None, confusableColor = "#G" };
            Assert.AreEqual("<b><color=#G>d</color></b>og", TextFormatter.ToRichText("dog", p));
        }

        [Test]
        public void ReflowJoinsLinesAndHyphenatedWords()
        {
            var blocks = new[]
            {
                new[] { "The fox was read-", "ing a book", "  " },
                new string[0],
                new[] { "Second para-", "Graph keeps its dash." },
            };
            Assert.AreEqual("The fox was reading a book\n\nSecond para- Graph keeps its dash.", TextReflow.Join(blocks));
        }

        [Test]
        public void PresetCloneIsIndependent()
        {
            var a = ReadingProfile.Presets()[0];
            var b = a.Clone();
            b.palette[0] = "#000000";
            Assert.AreNotEqual(a.palette[0], b.palette[0]);
        }

        [Test]
        public void OcrTextCannotInjectTags()
        {
            var p = new ReadingProfile { colorMode = ColorMode.None, highlightConfusables = false };
            Assert.AreEqual("a <noparse><</noparse>size=500>", TextFormatter.ToRichText("a <size=500>", p));
        }
    }
}
