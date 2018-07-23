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
    public class ConfidenceTests
    {
        [TestMethod()]
        public void ConfidenceTest()
        {
            Assert.IsTrue(true);
        }

        [TestMethod()]
        public void CompareTest()
        {
            //Подготовка данных для сравнения
            int[] ichilds1 = { 1002, 1003, 1004, 1005 };    // набор из четырёх id
            int[] ichilds3 = { 1002, 1003, 1014, 1015 };    // набор из четырёх id, отличающихся наполовину от ichilds1
            //Подготовим список дочерних id
            List<Term> childs = ichilds1.Select(c => new Term(0, c, 1, "", null)).ToList();
            //Подготовим список дочерних id полностью отличных от childs
            List<Term> childs2 = ichilds1.Select(c => new Term(0, c + 1, 1, "", null)).ToList();
            List<Term> childs3 = ichilds3.Select(c => new Term(0, c, 1, "", null)).ToList();
            Term term_a = new Term(_rank: 1, _id: 2011, _confidence: 1, _text: "тест", _childs: childs);
            Term term_b = new Term(_rank: 1, _id: 2012, _confidence: 1, _text: "тест", _childs: childs);
            Term term_c = new Term(_rank: 1, _id: 2013, _confidence: 1, _text: "тест", _childs: childs2);
            Term term_d = new Term(_rank: 1, _id: 2014, _confidence: 1, _text: "тест", _childs: childs3);
            //Логика выбора метрики для Compare может меняться, так как свойство 
            //private static readonly Func<Term, Term, float>[] Operations = new Func<Term, Term, float>[5]
            //{
            //    Confidence.Equality,
            //    Confidence.Cosine,
            //    Confidence.SoftInclusive,
            //    Confidence.SoftInclusive,
            //    Confidence.SoftInclusive
            //};
            //в классе Confidence можно заполнять другими функциями. Однако для текущего набора функций
            //сравнение термов с идентичным набором дочерних термов должно дать 1.
            var d1 = Confidence.Compare(term_a, term_b);
            Assert.AreEqual(1, d1);
            //Полностью различные термы. Косинусное расстояние должно дать 0.
            var d2 = Confidence.Compare(term_a, term_c);
            Assert.AreEqual(0, d2);
            //Наполовину различные термы. Так как применяется косинусное расстояние, то наполовину схожие термы дадут расстояние 0.5
            var d3 = Confidence.Compare(term_a, term_d);
            Assert.AreEqual(0.5f, d3);
        }

        [TestMethod()]
        public void CompareTest1()
        {
            Assert.AreEqual(1, Confidence.Compare(1001, 1001));
            Assert.AreEqual(0, Confidence.Compare(1002, 1001));
        }
    }
}