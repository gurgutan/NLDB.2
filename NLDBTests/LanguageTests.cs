using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB.Tests
{
    [TestClass()]
    public class LanguageTests
    {
        string srctext = "Текст для проверки корректности создания Language. Второе предложение. Последнее третье предложение.";
        string sentence1 = "тест проверки корректности";
        string sentence2 = "проверки корректности"; //предложение полностью включенное в первое предложение текста
        string sentence3 = "пятое предложение";    //предложение наполовину совпадающее со вторым предложением текста
        string srcfilename = "CreateFromTextFileTest.txt";
        string[] splitters = new string[] { "", @"\s+", @"[\.\n]" };


        [TestMethod()]
        public void LanguageTest()
        {
            Language l = new Language("Тест1", splitters);
            //Создан Language с тремя словарями
            Assert.AreEqual(l.Lexicons.Count, 3);
        }

        [TestMethod()]
        public void ClearTest()
        {
            Language l = new Language("Тест1", splitters);
            l.CreateFromString(srctext);
            l.Clear();
            //Ранг остался прежним
            Assert.AreEqual(l.Rank, 2);
            //Словари теперь пустые
            Assert.AreEqual(l[0].Count, 0);
            Assert.AreEqual(l[1].Count, 0);
            Assert.AreEqual(l[2].Count, 0);
        }

        [TestMethod()]
        public void EvaluateTest()
        {
            Language l = new Language("Тест1", splitters);
            l.CreateFromString(srctext);
            //Вычисляем терм (с оценкой) для тестового предложения sentence. Предложения в этом языке имеют ранг 2
            Term term1 = l.Evaluate(sentence2, 2);
            //второе предложение должно давать оценку 1
            Assert.IsTrue(term1.Confidence == 1);
            Term term2 = l.Evaluate(sentence3, 2);
            Assert.IsTrue(term2.Confidence == 0.5);
        }

        [TestMethod()]
        public void EvaluateTermTest()
        {
            Language l = new Language("Тест1", splitters);
            l.CreateFromString(srctext);
            //Строим терм с использованием лексикона ранга 2
            Term term = l[2].BuildTerm(sentence2);
            //второе предложение должно давать оценку 1
            Assert.IsTrue(term.AsText == "проверки корректности");
            Assert.IsTrue(term.Rank == 2);
            l.EvaluateTerm(term);
            Assert.IsTrue(term.Confidence == 1);
        }

        [TestMethod()]
        public void FindManyTest()
        {
            Language l = new Language("Тест поиска ближайших", splitters);
            l.CreateFromString(srctext);
            //Вернуть список ближайших к "тест проверки корректности", состоящий не более чем из 1 элемента ранга 2
            var nearest = l.FindMany(sentence1, 1, 2);
            //Проверяем что что-то найдено
            Assert.IsTrue(nearest.Count > 0);
            //Убеждаемся, что ближайшим предложением является предложение с Id 0
            Assert.AreEqual(nearest.First().Id, 0);
        }

        [TestMethod()]
        public void CreateFromTextFileTest()
        {
            Language l = new Language("Тест1", splitters);
            //Создаем тестовый файл
            using (StreamWriter writer = new StreamWriter(srcfilename))
            {
                writer.Write(srctext);
            }
            //Создаем объект из файла
            l.CreateFromTextFile(srcfilename);
            //Проверяем результат:
            //Количество различных букв (словарь 0-го ранга) равно 23
            Assert.AreEqual(l[0].Count, 23);
            //Количество различных слов (словарь 1-го ранга) равно 10
            Assert.AreEqual(l[1].Count, 10);
            //Количество различных предложений (словарь 2-го ранга) равно 10
            Assert.AreEqual(l[2].Count, 3);
        }

        [TestMethod()]
        public void CreateFromStringTest()
        {
            Language l = new Language("Тест1", splitters);
            //Создаем объект из строки
            l.CreateFromString(srctext);
            //Проверяем результат:
            //Количество различных букв (словарь 0-го ранга) равно 23
            Assert.AreEqual(l[0].Count, 23);
            //Количество различных слов (словарь 1-го ранга) равно 10
            Assert.AreEqual(l[1].Count, 10);
            //Количество различных предложений (словарь 2-го ранга) равно 10
            Assert.AreEqual(l[2].Count, 3);
        }

        [TestMethod()]
        public void SeriailizeTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void DeserializeTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void InitTest()
        {
            Assert.Fail();
        }
    }
}