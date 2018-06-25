using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NLDB
{
    [TestClass]
    public class LanguageTest
    {
        [TestMethod]
        public void ConfidenceTest()
        {
            string text = "Первый полёт самолёта в истории был осуществлён 17 декабря 1903 год";
            Language l = new Language("Тест1", new string[] { "", @"[^\w\d]+", @"[\:\;\.\?\!\n\r]+" });
            l.CreateFromString(text);
            Lexicon lex = l[2];
            var nearest = l.FindMany("первый полёт самолёта", 10, 2);
            Assert.IsTrue(nearest.First().Confidence > 0.9);

        }
    }
}
