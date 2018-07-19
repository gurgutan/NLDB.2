using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class Alphabet
    {
        Dictionary<string, int> codes = new Dictionary<string, int>();
        Dictionary<int, string> letters = new Dictionary<int, string>();

        public int Count { get { return codes.Count; } }

        public Dictionary<int, string> Letters { get { return letters; } }

        public bool Contains(string text)
        {
            return codes.ContainsKey(text);
        }

        public bool Contains(int id)
        {
            return letters.ContainsKey(id);
        }

        public void Add(string s, int i)
        {
            codes[s] = i;
            letters[i] = s;
        }

        public int this[string i]
        {
            get
            {
                int id;
                codes.TryGetValue(i, out id);
                return id;
            }
            set
            {
                codes[i] = value;
                letters[value] = i;
            }
        }
        public string this[int i]
        {
            get
            {
                string s;
                letters.TryGetValue(i, out s);
                return s;
            }
            set
            {
                letters[i] = value;
                codes[value] = i;
            }
        }

        public bool TryGetValue(string s, out int i)
        {
            return codes.TryGetValue(s, out i);
        }

        public bool TryGetValue(int i, out string s)
        {
            return letters.TryGetValue(i, out s);
        }

        internal void Clear()
        {
            codes.Clear();
            letters.Clear();
        }
    }
}
