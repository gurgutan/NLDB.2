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
            Assert.Fail();
        }

        [TestMethod()]
        public void LexiconDeserializeTest()
        {
            string filename = "SerializeLexiconTest.dat";
            Lexicon lex = new Lexicon(null, "");
            lex.TryAddMany("слова");
            IFormatter formatter = new BinaryFormatter();
            FileStream wStream = new FileStream(filename, FileMode.Create);
            formatter.Serialize(wStream, lex);
            wStream.Close();

            //Десериализуем
            FileStream rStream = new FileStream(filename, FileMode.Open);
            Lexicon lexDeserialized = (Lexicon)formatter.Deserialize(rStream);
            rStream.Close();

            //Проверим результат десериализации
            Assert.IsTrue(lexDeserialized.Words.Count() == 5);
            Assert.IsTrue(lexDeserialized.Codes.Count() == 5);
            Assert.IsTrue(lexDeserialized.Alphabet.Count() == 5);
            Assert.IsTrue(lexDeserialized.AlphabetCodes.Count() == 5);
        }

        [TestMethod()]
        public void GetObjectDataTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ClearTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToTextTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ToCodeTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void TryAddManyTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void TryAddTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void BuildTermTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void EvaluateTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void EvaluateTest1()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void FindManyTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetByChildsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AtomIdTest()
        {
            Assert.Fail();
        }
    }
}