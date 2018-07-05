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
        Dictionary<int, string> strings = new Dictionary<int, string>();

        public bool Contains(string text)
        {
            return codes.ContainsKey(text);
        }

        public bool Contains(int id)
        {
            return strings.ContainsKey(id);
        }

        public void Add(string s, int i)
        {
            codes[s] = i;
            strings[i] = s;
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
                strings[value] = i;
            }
        }
        public string this[int i]
        {
            get
            {
                string s;
                strings.TryGetValue(i, out s);
                return s;
            }
            set
            {
                strings[i] = value;
                codes[value] = i;
            }
        }

        internal void Clear()
        {
            codes.Clear();
            strings.Clear();
        }
    }
}
