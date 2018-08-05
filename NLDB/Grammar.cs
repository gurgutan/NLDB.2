using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class Rule : IEnumerable, IDisposable
    {
        public int id;
        Dictionary<int, int> transitions;   //Словарь<куда, сколько_раз> был переход из текущего слова (с id = this.id)
        public Dictionary<int, int> Transitions
        {
            get { return transitions; }
        }

        Dictionary<int, Rule> rules;
        public Dictionary<int, Rule> Rules
        {
            get { return rules; }
        }

        public Rule(int i)
        {
            id = i;
            transitions = new Dictionary<int, int>();
            rules = new Dictionary<int, Rule>();
        }

        public double Confidence(int i)
        {
            if (!transitions.ContainsKey(i)) return 0;
            return (float)transitions[i] / transitions.Count;
        }

        public void Add(Rule rule)
        {
            if (transitions.ContainsKey(rule.id))
            {
                transitions[rule.id]++;
                return;
            }
            rules.Add(rule.id, rule);
            transitions.Add(rule.id, 1);
        }

        public void Add(int i)
        {
            if (transitions.ContainsKey(i))
            {
                transitions[i]++;
                return;
            }
            rules.Add(i, new Rule(i));
            transitions.Add(i, 1);
        }

        public void Add(int[] ids)
        {
            if (ids.Length == 0) return;
            this.Add(ids.First());
            rules[ids.First()].Add(ids.Skip(1).ToArray());
        }

        public bool Exists(int i)
        {
            return transitions.ContainsKey(i);
        }

        public Rule Get(int i)
        {
            Rule result;
            rules.TryGetValue(i, out result);
            return result;
        }

        public int this[int i]
        {
            get
            {
                int count;
                transitions.TryGetValue(i, out count);
                return count;
            }
            set
            {
                transitions[i] = value;
                if (!rules.ContainsKey(i)) rules.Add(i, new Rule(i));
            }
        }

        public int Count()
        {
            return 1 + rules.Values.Sum(r => r.Count());
        }

        public IEnumerator GetEnumerator()
        {
            yield return this;
            foreach (var r in rules)
            {
                yield return r;
            }
        }

        public void Dispose()
        {
            rules.Clear();
            transitions.Clear();
        }

        public void Clear()
        {
            transitions.Clear();
            rules.Clear();
        }
    }

    //----------------------------------------------------------------------------------------------------
    public class Grammar : IEnumerable, IDisposable
    {
        Rule root = new Rule(0);

        public Rule Root
        {
            get { return root; }
        }

        public Grammar() { }

        public Rule this[int i]
        {
            get { return root.Get(i); }
        }

        public void Add(int[] ids)
        {
            if (ids == null || ids.Length == 0) return;
            root.Add(ids);
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
