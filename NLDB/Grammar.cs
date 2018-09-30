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
        public List<Rule> Rules => this.rules;

        public Rule(int _id, int _n = 1/*, int _pos = 0*/)
        {
            this.id = _id;
            this.number = _n;
            //this.pos = _pos;
            this.rules = new List<Rule>();            
        }

        /// <summary>
        /// Вычисляет уверенность в текущем правиле, как отношение числа использований данного правила к общему числу упоминаний 
        /// любого правила, в данном контексте.
        /// </summary>
        /// <param name="i">идентификатор Правила (Слова)</param>
        /// <returns></returns>
        public float Confidence(int i)
        {
            if (!this.Exists(i)) return 0;
            return (float)this[i].number / this.rules.Count;
        }

        /// <summary>
        /// Метод добавляет следующее за текущим Правило rule, если оно еще не добавлено или увеличивает счетчик упоминаний Правила rule
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public Rule AddCount(Rule rule)
        {
            Rule actual = this.Get(rule.id);
            if (actual != null)
            {
                actual.number++;
            }
            else
            {
                this.rules.Add(rule);
                actual = rule;
            }
            return actual;
        }

        public Rule AddCount(int _id)
        {
            Rule actual = this.Get(_id);
            if (actual != null)
                actual.number++;
            else
            {
                actual = new Rule(_id, 1/*, this.pos + 1*/);
                this.rules.Add(actual);
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
            this.AddCount(first).AddSeq(ids.Skip(1).ToArray());
        }

        public bool Exists(int i)
        {
            return this.Get(i) != null;
        }

        public Rule Get(int i)
        {
            Rule rule = new Rule(i);
            //TODO: критическая для производительности операция! надо что-нибудь придумать. Отказ от словаря для для экономии памяти
            int index = this.rules.IndexOf(rule);
            if (index > -1)
                return this.rules[index];
            else
                return null;
        }

        public Rule this[int i] => this.Get(i);

        public int Count()
        {
            return 1 + this.rules.Sum(r => r.Count());
        }

        public IEnumerator GetEnumerator()
        {
            yield return this;
            foreach (Rule r in this.rules)
            {
                yield return r;
            }
        }

        public void Dispose()
        {
            this.rules.Clear();
        }

        public void Clear()
        {
            this.rules.Clear();
        }

        public bool Equals(Rule other)
        {
            if (other == null) return false;
            return (other as Rule).id == this.id;
        }
    }

    //----------------------------------------------------------------------------------------------------
    public class Grammar : IEnumerable, IDisposable
    {
        private readonly Rule root = new Rule(0);

        public Rule Root => this.root;

        public Grammar() { }

        public Rule this[int i] => this.root.Get(i);

        public void Add(int[] ids)
        {
            if (ids == null || ids.Length == 0) return;
            this.root.AddSeq(ids);
        }

        public double Confidence(int[] ids)
        {
            Rule current = this.root;
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
            return this.root.Count();
        }

        public void Clear()
        {
            this.root.Clear();
        }

        public void Dispose()
        {
            this.root.Dispose();
        }

        public IEnumerator GetEnumerator()
        {
            return this.root.GetEnumerator();
        }
    }
}
