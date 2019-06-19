using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLDB;

namespace NLDBCoreTests
{
    [TestClass]
    public class GrammarTest
    {
        [TestMethod]
        public void GrammarConstructorTest()
        {
            Grammar g = new Grammar(2);
            Assert.IsNotNull(g.Root);
        }

        [TestMethod]
        public void GrammarAddTest()
        {
            Grammar g = new Grammar(2);
            Assert.IsNotNull(g.Root);
            //������� ���� ������� 1 2 3
            g.Add(new int[] { 1, 2, 3 });
            Assert.IsNotNull(g.Root.Followers[1].Followers[2].Followers[3]);
            //������� ������ ������� 1 2 4
            g.Add(new int[] { 1, 2, 4 });
            Assert.IsNotNull(g.Root.Followers[1].Followers[2].Followers[4]);
            //������� ������� 1 2 3 5 � 1 2 4 5, ������������ � 5
            g.Add(new int[] { 1, 2, 3, 5 });
            g.Add(new int[] { 1, 2, 4, 5 });
            //��������� ������� ������������ � 5
            var node5_1 = g.Root.Followers[1].Followers[2].Followers[3].Followers[5];
            var node5_2 = g.Root.Followers[1].Followers[2].Followers[4].Followers[5];
            Assert.AreEqual(node5_1, node5_2);

        }
    }
}
