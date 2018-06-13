﻿using StarMathLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    [Serializable]
    public partial class Lexicon
    {
        private readonly Parser parser;
        private int count = 0;
        Confidence calculator;


        public int Rank;
        public string Splitter = "";

        public Lexicon Child;
        public Lexicon Parent;
        readonly Dictionary<Word, int> w2i = new Dictionary<Word, int>();
        readonly Dictionary<int, Word> i2w = new Dictionary<int, Word>();
        readonly Dictionary<string, int> s2i = new Dictionary<string, int>();
        readonly Dictionary<int, string> i2s = new Dictionary<int, string>();

        public Lexicon(string splitter, Lexicon child = null, Lexicon parent = null)
        {
            if (child == null)
                this.Rank = 0;
            else
                this.Rank = child.Rank + 1;
            this.Child = child;
            this.Parent = parent;
            this.Splitter = splitter;
            this.parser = new Parser(this.Splitter);
            if (child != null) child.Parent = this;
            this.calculator = new Confidence(this);
        }

        public int Count
        {
            get { return this.w2i.Count; }
        }

        public Word this[int id]
        {
            get { return this.i2w[id]; }
        }

        public int this[Word w]
        {
            get { return this.w2i[w]; }
        }

        /// <summary>
        /// Массив Id слов
        /// </summary>
        public IEnumerable<int> Codes
        {
            get { return this.w2i.Values.ToArray(); }
        }

        public IEnumerable<Word> Words
        {
            get { return this.w2i.Keys; }
        }

        /// <summary>
        /// Массив базовых строковых представлений символов(букв) словаря. Имеет смысл только для словаря ранга 0, так как 
        /// </summary>
        public IEnumerable<string> Alphabet
        {
            get { return this.s2i.Keys; }
        }

        public string ToText(int i)
        {
            //разделители слов разного ранга в строку 
            string[] sp = new string[] { "", " ", ".", "\n", "\n\n" };
            if (this.Rank == 0) return this.i2s[i];
            return this.i2w[i].Childs.Aggregate("", (c, n) => c + sp[this.Rank - 1] + this.Child.ToText(n));
        }

        /// <summary>
        /// Возвращает код слова, представленного строкой s
        /// </summary>
        /// <param name="s">тектсовое представление слова</param>
        /// <returns>код, идентифицирующий слово или -1, если слово с текстовым представлением s не найдено в словаре</returns>
        public int ToCode(string s)
        {
            int code;
            if (this.s2i.TryGetValue(s, out code))
                return code;
            return -1;
        }

        public int[] TryAddMany(string text)
        {
            string normText = this.parser.Normilize(text);
            return
                this.parser.
                Split(normText).
                Select(t => t.Trim()).
                Where(s => !string.IsNullOrWhiteSpace(s)).
                Select(s => this.TryAdd(s)).
                ToArray();
        }

        public Term BuildTerm(string s)
        {
            string text = this.parser.Normilize(s);
            if (this.Rank == 0)
                return new Term(text, new List<Term>());
            else
                return new Term(text,
                    this.Child.parser.Split(text).
                    Select(t => t.Trim()).
                    Where(e => !string.IsNullOrWhiteSpace(e)).
                    Select(e => this.Child.BuildTerm(e)).
                    ToList());
        }

        public Term EvaluateTerm(string text)
        {
            Term term = this.BuildTerm(text);
            this.EvaluateTerm(term);
            return term;
        }

        public void EvaluateTerm(Term term)
        {
            if (term.Rank < this.Rank)
            {
                this.Child.EvaluateTerm(term);
                return;
            }
            if (term.Rank > this.Rank)
            {
                this.Parent.EvaluateTerm(term);
                return;
            }
            //Если терм нулевого ранга, то есть символ, то оценка = 1
            //if (term.Rank == 0)
            //{
            //    int a;
            //    if (!this.s2i.TryGetValue(term.Text, out a)) a = -1;
            //    term.Id = a;
            //    term.Confidence = (a >= 0 ? 1 : 0);
            //    return;
            //}
            //вычисляем полную вероятность терма term, по априорным и апостериорным вероятностям символов слова (Childs)
            if (term.Rank > 0)
                foreach (var t in term.Childs)
                    this.EvaluateTerm(t);
            calculator.Evaluate(term);
        }

        //private Tuple<int, double> FindNearest(Term term)
        //{
        //    int id = -1;
        //    double confidence = 0;
        //    //Для нулевого ранга - ищем атомарный символ
        //    if (this.Rank == 0)
        //    {
        //        //Пытаемся найти атомарное слово, соответствующее терму
        //        if (this.s2i.TryGetValue(term.Text, out id)) confidence = 1;
        //        return new Tuple<int, double>(id, confidence);
        //    }
        //    //Для терма ранга > 0
        //    Dictionary<int, double> parents = new Dictionary<int, double>();
        //    //Считаем произведение вектора-слова ранга r-1 на матрицу вхождений слов в словарь ранга r
        //    //Цикл по всем буквам (словам ранга r-1)
        //    for (int i = 0; i < term.Childs.Count; i++)
        //    {
        //        Term subterm = term.Childs[i];
        //        //Если слово term.Childs[i] еще не определено, то пытаемся его определить
        //        if (subterm.Id == -1)
        //        {
        //            Tuple<int, double> pair = this.Child.FindNearest(subterm);
        //            subterm.Id = pair.Item1;
        //            subterm.Confidence = pair.Item2;
        //        }
        //        //если слово определить не получилось, то пропускаем его дальнейшую обработку, т.к. id=-1, confidence=0
        //        if (subterm.Id == -1) continue;
        //        //Получаем букву (подслово) соответствующее терму term.Childs[i]
        //        Word subword = this.Child[subterm.Id];
        //        //По всем словам, в которые входит данная буква, увеличиваем confidence
        //        for (int j = 0; j < subword.parents.Count; j++)
        //        {
        //            WordLink plink = subword.parents[j];
        //            Word word = this.i2w[plink.id];
        //            //Максимальное расстояние между словами. Нужно для нормирования
        //            //double max_dist = Math.Max(parent_word.childs.Length, term.Childs.Count);
        //            if (!parents.ContainsKey(plink.id)) parents.Add(plink.id, 0);
        //            parents[plink.id] += ((i == plink.pos) ? 1 : 0) * subterm.Confidence / subword.parents.Count;// (max_dist - Math.Abs(child_pos - link.pos)) * child_conf;
        //        }
        //    }
        //    //Ищем слово с маскимальным значением
        //    foreach (var p in parents)
        //    {
        //        //Произведение длин векторов для знаменателя косинусного расстояния
        //        double denominator = term.Childs.Count;// this.i2w[p.Key].childs.Length * term.Childs.Count;
        //        double p_confidence = p.Value / denominator;
        //        if (p_confidence > confidence)
        //        {
        //            confidence = p_confidence;
        //            id = p.Key;
        //        }
        //    }
        //    return new Tuple<int, double>(id, confidence);
        //}

        /// <summary>
        /// Пытается добавить слово, представленное строкой s, в словарь. Если такое слово уже есть, то добавления не происходит.
        /// </summary>
        /// <param name="s">текстовое предстваление слова</param>
        /// <returns>возвращает id слова</returns>
        public int TryAdd(string s)
        {
            int id = -1;
            if (this.Rank == 0)
            {
                if ((id = this.AtomId(s)) == -1)
                    id = this.Register(new int[0], s);
            }
            else
            {
                int[] childs = this.Child.TryAddMany(s);
                if ((id = this.GetByChilds(childs)) == -1)
                    id = this.Register(childs);
            }
            return id;
        }

        public int GetByChilds(int[] childs)
        {
            int id;
            if (this.w2i.TryGetValue(new Word(-1, childs), out id))
                return id;
            return -1;
        }

        public int AtomId(string s)
        {
            int id;
            if (this.s2i.TryGetValue(s, out id))
                return id;
            return -1;
        }

        private int Register(int[] subwords, string s = "")
        {
            int id = this.count;
            this.count++;
            //Создаем новое слово
            Word w = new Word(id, subwords);
            //Добавляем строковый символ для слова, если он задан
            if (!string.IsNullOrEmpty(s))
            {
                this.s2i.Add(s, id);
                this.i2s.Add(id, s);
            }
            //Добавляем индексы в словарь
            this.w2i.Add(w, w.Id);
            this.i2w.Add(w.Id, w);
            //Добавляем связи дочерних слов с данным словом
            for (int i = 0; i < subwords.Length; i++)
            {
                this.Child.i2w[subwords[i]].AddParent(id, i);
            }
            return id;
        }

        //Закомментировано. Пока не найдена возможность использовать высокопроизводительные вычисления с разреженными матрицами
        ///// <summary>
        ///// Возвращает представление словаря ранга r, как матрицы размерности m x n, где m - размер дочернего словаря, а n - размер словаря r
        ///// </summary>
        ///// <param name="r">ранга словаря</param>
        ///// <returns></returns>
        //public int[,] AsDenseMatrix()
        //{
        //    if (this.Rank == 0) throw new ArgumentOutOfRangeException("Для словаря нулевого ранга не существует матричного представления");
        //    int[,] matrix = new int[this.Child.Count, this.Count];
        //    int col = 0;
        //    foreach (var w in w2i.Keys)
        //    {
        //        for (int i = 0; i < w.childs.Length; i++)
        //        {
        //            matrix[w.childs[i], col] |= (1 << i);
        //        }
        //        col++;
        //    }
        //    return matrix;
        //}

        ///// <summary>
        ///// Метод формирует матрицу в координатном формате
        ///// </summary>
        ///// <returns>массив из трех массивов: [0] - значения, [1] - индексы строк, [2] - индексы колонок</returns>
        //public int[][] AsCOOMatrix()
        //{
        //    if (this.Rank == 0) throw new ArgumentOutOfRangeException("Для словаря нулевого ранга не существует матричного представления");
        //    List<TripleInt> values = new List<TripleInt>();
        //    int col = 0;
        //    foreach (var w in w2i.Keys)
        //    {
        //        //Составляем набор значений для записи в матрицу
        //        Dictionary<int, int> bitmap = new Dictionary<int, int>(w.childs.Length);
        //        int pos = 0;
        //        foreach (var row in w.childs)
        //        {
        //            if (!bitmap.ContainsKey(row))
        //                bitmap[row] = (1 << pos);
        //            else
        //                bitmap[row] |= (1 << pos);
        //            pos++;
        //        }
        //        //Добавляем значения в списки для COOMatrix
        //        foreach (var v in bitmap)
        //        {
        //            values.Add(new TripleInt(v.Value, v.Key, col));
        //        }
        //        col++;
        //    }
        //    //Сортировка по строкам, потом по столбцам
        //    values.Sort((a, b) => TripleInt.Compare(a, b));
        //    int[][] matrix = new int[3][];
        //    matrix[0] = values.Select(v => v.val).ToArray();
        //    matrix[1] = values.Select(v => v.row).ToArray();
        //    matrix[2] = values.Select(v => v.col).ToArray();
        //    return matrix;
        //}

        //public Dictionary<int[], double> AsSparseMatrix()
        //{
        //    if (this.Rank == 0) throw new ArgumentOutOfRangeException("Для словаря нулевого ранга не существует матричного представления");
        //    Dictionary<int[], double> result = new Dictionary<int[], double>(this.count * Language.WORD_SIZE);
        //    foreach (var pair in w2i)
        //    {
        //        int col = pair.Value;
        //        int pos = 0;
        //        foreach (var c in pair.Key.childs)
        //        {
        //            int row = c * Language.WORD_SIZE + pos;
        //            result.Add(new int[] { row, col }, 1.0);
        //            pos++;
        //        }
        //    }
        //    return result;
        //}

    }
}
