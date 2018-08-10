using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace NLDB
{
    public partial class Language
    {
        private IEnumerable<int> Parse(string text, int rank)
        {
            text = this.parsers[rank].Normilize(text);
            var strings = this.parsers[rank].Split(text).Where(s => !string.IsNullOrEmpty(s));
            //Для слов ранга > 0 добавляем слова, которых еще нет
            return strings.Select(s =>
            {
                int id;
                int[] childs = null;
                if (rank > 0)
                {
                    childs = Parse(s, rank - 1).ToArray();      //получаем id дочерних слов ранга rank-1
                    if (childs.Length == 0) return 0;
                    id = data.GetId(childs);
                    if (id == 0) id = data.Add(new Word(0, rank, "", childs, new int[0]));
                }
                else
                {
                    id = data.GetId(s);
                    if (id == 0) id = data.Add(new Word(0, rank, s, null, new int[0]));
                }
                return id;
            }).Where(i => i != 0);
        }

        public Term ToTerm(string text, int rank)
        {
            text = parsers[rank].Normilize(text);
            return new Term(rank, 0, 0, text,
                rank == 0 ? null : 
                parsers[rank - 1].
                Split(text).
                Where(s => !string.IsNullOrWhiteSpace(s)).
                Select(s => ToTerm(s, rank - 1)));
        }

        public Term ToTerm(Word w, float _confidence = 1)
        {
            return data.ToTerm(w, _confidence);
        }

        public Term ToTerm(int i, float _confidence = 1)
        {
            return data.ToTerm(i, _confidence);
        }

        public Term Similar(string text, int rank)
        {
            text = parsers[rank].Normilize(text);
            Term term = ToTerm(text, rank);
            return Identify(term);
        }
    }
}
