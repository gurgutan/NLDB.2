using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NLDB
{
    //TODO: переделать Rule на движок БД
    /// <summary>
    /// Класс, представляющий сущность Правило Грамматики. Содержит идентификатор Слова и список Правил, допустимых как следующие за данным Словом.
    /// </summary>
    public class Rule : IEnumerable, IDisposable, IEquatable<Rule>
    {
        /// <summary>
        /// Идентификатор Слова и Правила одновременно
        /// </summary>
        public int id;
        /// <summary>
        /// Число упоминаний данного правила по итогам анализа текста
        /// </summary>
        public int number;
        //public readonly int pos;

        private readonly List<Rule> rules;
        public List<Rule> Rules => rules;

        public Rule(int _id, int _n = 1/*, int _pos = 0*/)
        {
            id = _id;
            number = _n;
            //this.pos = _pos;
            rules = new List<Rule>();
        }

        /// <summary>
        /// Вычисляет уверенность в текущем правиле, как отношение числа использований данного правила к общему числу упоминаний 
        /// любого правила, в данном контексте.
        /// </summary>
        /// <param name="i">идентификатор Правила (Слова)</param>
        /// <returns></returns>
        public float Confidence(int i)
        {
            if (!Exists(i)) return 0;
            return (float)this[i].number / rules.Count;
        }

        /// <summary>
        /// Метод добавляет следующее за текущим Правило rule, если оно еще не добавлено или увеличивает счетчик упоминаний Правила rule
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public Rule AddCount(Rule rule)
        {
            Rule actual = Get(rule.id);
            if (actual != null)
            {
                actual.number++;
            }
            else
            {
                rules.Add(rule);
                actual = rule;
            }
            return actual;
        }

        public Rule AddCount(int _id)
        {
            Rule actual = Get(_id);
            if (actual != null)
                actual.number++;
            else
            {
                actual = new Rule(_id, 1/*, this.pos + 1*/);
                rules.Add(actual);
            }
            return actual;
        }

        /// <summary>
        /// Добавить цепочку Правил ids, следующих за данным (this)
        /// </summary>
        /// <param name="ids"></param>
        public void AddSeq(int[] ids)
        {
            if (ids.Length == 0) return;
            int first = ids.First();
            AddCount(first).AddSeq(ids.Skip(1).ToArray());
        }

        public bool Exists(int i)
        {
            return Get(i) != null;
        }

        public Rule Get(int i)
        {
            Rule rule = new Rule(i);
            //TODO: критическая для производительности операция! надо что-нибудь придумать. Отказ от словаря для экономии памяти
            int index = rules.IndexOf(rule);
            if (index > -1)
                return rules[index];
            else
                return null;
        }

        public Rule this[int i] => Get(i);

        public int Count()
        {
            return 1 + rules.Sum(r => r.Count());
        }

        public IEnumerator GetEnumerator()
        {
            yield return this;
            foreach (Rule r in rules)
            {
                yield return r;
            }
        }

        public void Dispose()
        {
            rules.Clear();
        }

        public void Clear()
        {
            rules.Clear();
        }

        public bool Equals(Rule other)
        {
            if (other == null) return false;
            return (other as Rule).id == id;
        }
    }

    //----------------------------------------------------------------------------------------------------
    public class GrammarTree : IEnumerable, IDisposable
    {
        private readonly Rule root = new Rule(0);

        public Rule Root => root;

        public GrammarTree() { }

        public Rule this[int i] => root.Get(i);

        public void Add(int[] ids)
        {
            if (ids == null || ids.Length == 0) return;
            root.AddSeq(ids);
        }

        public double Confidence(int[] ids)
        {
            Rule current = root;
            double confidence = 1;
            int count = 0;
            for (int i = 0; i < ids.Length; i++)
            {
                count++;
                confidence += current.Confidence(ids[i]);
                current = current.Get(ids[i]);
                if (current == null) break;
            }
            return confidence / count;
        }

        public int Count()
        {
            return root.Count();
        }

        public void Clear()
        {
            root.Clear();
        }

        public void Dispose()
        {
            root.Dispose();
        }

        public IEnumerator GetEnumerator()
        {
            return root.GetEnumerator();
        }
    }
}
