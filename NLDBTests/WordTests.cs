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
    public class WordTests
    {
        [TestMethod()]
        public void WordTest()
        {
            Word a = new Word(1001, 0, null, null);
            Word b = new Word(1002, 1, null, null);
            Assert.AreEqual(1001, a.id);
            Assert.AreEqual(1, a.rank);

        }

        [TestMethod()]
        public void AddParentTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AsSparseVectorTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void EqualsTest()
        {
            Word a1 = new Word(1001, 0, null, null);
            Word b1 = new Word(1001, 1, null, null);
            Assert.IsTrue(a1.Equals(b1));

            //Сравнение должно быть по childs
            Word a2 = new Word(0, 1, new int[2] { 1001, 1002 }, null);
            Word b2 = new Word(0, 1, new int[2] { 1001, 1002 }, null);
            Assert.IsTrue(a2.Equals(b2));

            //Сравнение должно быть по id
            Word a3 = new Word(1002, 1, new int[2] { 1001, 1002 }, null);
            Word b3 = new Word(1001, 1, new int[2] { 1001, 1002 }, null);
            Assert.IsFalse(a3.Equals(b3));

            //Сравнение должно быть по childs
            Word a4 = new Word(0, 1, new int[2] { 1001, 1002 }, null);
            Word b4 = new Word(1001, 1, new int[2] { 1001, 1002 }, null);
            Assert.IsTrue(a4.Equals(b4));

            //Сравнение должно быть по id
            Word a5 = new Word(1001, 1, new int[1] { 1001 }, null);
            Word b5 = new Word(1001, 1, new int[2] { 1001, 1002 }, null);
            Assert.IsTrue(a5.Equals(b5));

        }

        [TestMethod()]
        public void GetHashCodeTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void SerializeTest()
        {
            Assert.Fail();
        }
    }
}