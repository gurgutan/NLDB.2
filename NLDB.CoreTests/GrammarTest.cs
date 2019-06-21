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
            //Добавим одну цепочку 1 2 3
            g.Add(new int[] { 1, 2, 3 });
            Assert.IsNotNull(g.Root.Followers[1].Followers[2].Followers[3]);
            //Добавим цепочку 1 2 4
            g.Add(new int[] { 1, 2, 4 });
            Assert.IsNotNull(g.Root.Followers[1].Followers[2].Followers[4]);
            //Добавим цепочки, сединенные в последнем элементе
            g.Add(new int[] { 1, 2, 3, 5 });
            g.Add(new int[] { 1, 2, 4, 5 });
            //Проверим последний элемент 5
            var node5_1 = g.Root.Followers[1].Followers[2].Followers[3].Followers[5];
            var node5_2 = g.Root.Followers[1].Followers[2].Followers[4].Followers[5];
            Assert.AreEqual(node5_1, node5_2);
        }

        [TestMethod]
        public void GrammarFindNodeTest()
        {
            Grammar g = new Grammar(2);
            Assert.IsNotNull(g.Root);
            g.Add(new int[] { 1, 2, 3 });
            Assert.IsNotNull(g.Root.Followers[1].Followers[2].Followers[3]);
            g.Add(new int[] { 1, 2, 4 });
            Assert.IsNotNull(g.Root.Followers[1].Followers[2].Followers[4]);
            var node = g.FindNode(4);
            Assert.AreEqual(g.Root.Followers[1].Followers[2].Followers[4], node);
        }
        [TestMethod]
        public void GrammarFindWordTest()
        {
            Grammar g = new Grammar(2);
            Assert.IsNotNull(g.Root);
            g.Add(new int[] { 1, 2, 3 });
            Assert.IsNotNull(g.Root.Followers[1].Followers[2].Followers[3]);
            g.Add(new int[] { 1, 2, 4 });
            Assert.IsNotNull(g.Root.Followers[1].Followers[2].Followers[4]);
            var path = g.FindWord(new int[] { 1, 2, 4});
            Assert.AreEqual(g.Root, path[0]);
            Assert.AreEqual(g.Root.Followers[1], path[1]);
            Assert.AreEqual(g.Root.Followers[1].Followers[2], path[2]);
            Assert.AreEqual(g.Root.Followers[1].Followers[2].Followers[4], path[3]);
        }

    }
}
