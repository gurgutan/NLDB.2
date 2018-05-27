using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB.Tests
{
    [TestClass()]
    public class ParserTests
    {
        [TestMethod()]
        public void SplitTest()
        {
            Parser parser = new Parser(@"[^а-яА-ЯёЁ0-9]");
            string[] result = parser.Split("Привет мир!");
            Assert.AreEqual("Привет", result[0]);
            Assert.AreEqual("мир", result[1]);
        }

        [TestMethod()]
        public void NormilizeTest()
        {
            Parser parser = new Parser(@"[^а-яА-ЯёЁ0-9]");
            string result = parser.Normilize("Привет ~~м^^ир/!");
            Assert.AreEqual("Привет ми5р!", result);
        }
    }
}