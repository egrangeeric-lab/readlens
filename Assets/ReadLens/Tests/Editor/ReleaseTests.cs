using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ReadLens.App;
using ReadLens.Core;

namespace ReadLens.Tests
{
    public class ReleaseTests
    {
        [Test]
        public void VersionCodeIsMinutesSince2026AndGrows()
        {
            Assert.AreEqual(0, BuildVersion.Code(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            Assert.AreEqual(1441, BuildVersion.Code(new DateTime(2026, 1, 2, 0, 1, 0, DateTimeKind.Utc)));
            Assert.Greater(BuildVersion.Code(DateTime.UtcNow.AddMinutes(1)), BuildVersion.Code(DateTime.UtcNow));
        }

        [Test]
        public void DocumentRoundTripsAndSurvivesCorruption()
        {
            var path = Path.GetTempFileName();
            try
            {
                new SavedDocument { pages = new List<string> { "one", "two" }, currentPage = 1 }.Save(path);
                var doc = SavedDocument.Load(path);
                CollectionAssert.AreEqual(new[] { "one", "two" }, doc.pages);
                Assert.AreEqual(1, doc.currentPage);

                File.WriteAllText(path, "{ not json");
                Assert.AreEqual(0, SavedDocument.Load(path).pages.Count);

                File.Delete(path);
                Assert.AreEqual(0, SavedDocument.Load(path).pages.Count);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
