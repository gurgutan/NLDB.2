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
            int[] childs = new int[] { 1, 2, 3, 4 };            
            Word word = new Word(1, childs);
            word.AddParent(5, 1);
            word.AddParent(6, 1);
            word.AddParent(7, 1);
            Assert.AreEqual(word.Childs.Count(), 4);
            Assert.AreEqual(word.Parents.Count(), 3);
        }

        [TestMethod()]
        public void AddParentTest()
        {
            Word a = new Word(1, new int[] { 2 });
            a.AddParent(3, 1);
            a.AddParent(4, 2);
            Assert.AreEqual(a.Parents.Count, 2);
            Assert.AreEqual(a.Parents[0].Id, 3);
            Assert.AreEqual(a.Parents[0].Pos, 1);
            Assert.AreEqual(a.Parents[1].Id, 4);
            Assert.AreEqual(a.Parents[1].Pos, 2);
        }

        //Тест закоментирован, т.к. пока нет нужды в представлении слова в качестве разреженного вектора
        //[TestMethod()]
        //public void AsSparseVectorTest()
        //{
        //    Assert.Fail();
        //}

        [TestMethod()]
        public void EqualsTest()
        {
            Word a = new Word(1, new int[] { 11, 12, 13 });
            Word b = new Word(2, new int[] { 11, 12, 13 });
            Assert.IsTrue(a.Equals(b));
        }

        [TestMethod()]
        public void GetHashCodeTest()
        {
            Word a = new Word(1, new int[] { 11, 12, 13 });
            Word b = new Word(2, new int[] { 11, 12, 13 });
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());

            //Один потомок отличается на единицу
            Word c = new Word(1, new int[] { 10, 12, 13 });
            Assert.AreNotEqual(a.GetHashCode(), c.GetHashCode());
        }

        [TestMethod()]
        public void SerializeTest()
        {
            string fileName = "TestWordData.dat";
            IFormatter formatter = new BinaryFormatter();
            //Сериализуем
            Word a = new Word(
                1, 
                new int[] { 11, 12, 13 }, 
                new WordLink[] { new WordLink(5,1), new WordLink(6,1) });
            FileStream wStream = new FileStream(fileName, FileMode.Create);
            formatter.Serialize(wStream, a);
            wStream.Close();

            //Десериализуем
            FileStream rStream = new FileStream(fileName, FileMode.Open);
            Word b = (Word)formatter.Deserialize(rStream);
            rStream.Close();

            //Проверим результат десериализации
            Assert.IsTrue(a.Equals(b));
        }
    }
}