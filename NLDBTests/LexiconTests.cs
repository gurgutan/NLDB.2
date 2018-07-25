using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace NLDB.Tests
{
    [TestClass()]
    public class LexiconTests
    {
        [TestMethod()]
        public void LexiconTest()
        {
            Lexicon lex = new Lexicon(1000);
            Word w_r0_1 = new Word(1, 0, "", new int[0], null);
            Word w_r0_2 = new Word(2, 0, "", new int[0], null);
            Word w_r0_3 = new Word(3, 0, "", new int[0], null);
            Assert.AreEqual(64513, lex.Capacity);
        }
    }
}