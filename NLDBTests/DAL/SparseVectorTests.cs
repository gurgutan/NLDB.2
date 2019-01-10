using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NLDB.DAL.Tests
{
    [TestClass()]
    public class SparseVectorTests
    {
        [TestMethod()]
        public void SparseVectorTest()
        {
            //Создание через упорядоченную коллекцию значений
            var v1 = new SparseVector(new double[] { 1.0, 2.0, 3.0 });
            Assert.AreEqual(v1[0], 1.0);
            Assert.AreEqual(v1[1], 2.0);
            Assert.AreEqual(v1[2], 3.0);

            //Инициализация через коллекцию кортежей
            var tuples = new List<Tuple<int, double>> { Tuple.Create(0, 1.0), Tuple.Create(1, 2.0), Tuple.Create(2, 3.0) };
            var v2 = new SparseVector(tuples);
            Assert.AreEqual(v1[0], 1.0);
            Assert.AreEqual(v1[1], 2.0);
            Assert.AreEqual(v1[2], 3.0);
        }


        [TestMethod()]
        public void DotTest()
        {
            var v1 = new SparseVector(new double[] { 1.0, 1.0, 1.0 });
            var v2 = new SparseVector(new double[] { 1.0, 1.0, 1.0 });
            Assert.AreEqual(v1.Dot(v2), 3);
        }

        [TestMethod()]
        public void CosDistanceTest()
        {
            var v1 = new SparseVector(new double[] { 1.0 });
            var v2 = new SparseVector(new double[] { 1.0 });
            var v3 = new SparseVector(new double[] { 1.0, 0.0, 0.0 });
            var v4 = new SparseVector(new double[] { 0.0, 1.0, 0.0 });
            var v5 = new SparseVector(new double[] { -1.0, 0.0, 0.0 });
            //При вычислении возможны отклонения на 0.00000001. Будьте осторожны
            var cd1 = v1.CosDistance(v2);
            var cd2 = v3.CosDistance(v4);
            var cd3 = v3.CosDistance(v5);
            Assert.IsTrue(Math.Abs(cd1 - 1) < 0.000001);
            Assert.IsTrue(Math.Abs(cd2) < 0.000001);
            Assert.IsTrue(Math.Abs(cd3 + 1) < 0.000001);
        }

        [TestMethod()]
        public void EnumerateIndexedTest()
        {
            var v = new SparseVector(new double[] { 1.0 });
            var vi = v.EnumerateIndexed();
            var e = vi.GetEnumerator();
            Assert.AreEqual(e.MoveNext(), true);
            Assert.AreEqual(e.Current.Item1, 0);    //индекс
            Assert.AreEqual(e.Current.Item2, 1);    //значение
        }

        [TestMethod()]
        public void GetEnumeratorTest()
        {
            var v = new SparseVector(new double[] { 1.0, 1.0, 1.0 });
            var enumerator = v.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public void TestNorm()
        {
            var v = new SparseVector(new double[] { 1.0, 1.0, 1.0 });
            Assert.AreEqual(v.NormL1, 3);
            Assert.AreEqual(v.NormL2, Math.Sqrt(3));
            Assert.AreEqual(v.SquareNormL2, 3);
        }

    }
}