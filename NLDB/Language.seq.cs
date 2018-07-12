using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public partial class Language
    {
        private const int links_max_count = 1 << 23;// Количество цепочек для инициализации словаря
        private const int link_max_size = 8;        // Максимальный размер цепочек
        Dictionary<Link, int> links = new Dictionary<Link, int>(links_max_count);

        public void BuildSequences()
        {
            //Значения словаря - словари Dictionary<int, int>, ключ - id, значение - количество
            Dictionary<Link, Dictionary<int, int>> temp_links = new Dictionary<Link, Dictionary<int, int>>(links_max_count);
            Console.WriteLine("Построение цепочек...");
            var words = w2i.Keys;
            foreach (var word in words)
            {
                if (word.rank == 0) continue;
                for (int size = 2; size < link_max_size; size++)
                {
                    Queue<int> que = new Queue<int>();
                    for (int i = 0; i < word.childs.Length - 1; i++)
                    {
                        que.Enqueue(word.childs[i]);
                        if (que.Count > size)
                        {
                            Link link = new Link(que.ToArray());
                            Dictionary<int, int> d;
                            temp_links.TryGetValue(link, out d);
                            if (d == null)
                            {
                                d = new Dictionary<int, int>();
                                temp_links[link] = d;
                            }
                            if (!d.ContainsKey(word.childs[i + 1]))
                                d[word.childs[i + 1]] = 0;
                            d[word.childs[i + 1]]++;
                            que.Dequeue();
                        }
                    }
                }
            }
            Console.WriteLine($"Обработка цепочек...");
            foreach (var l in temp_links)
            {
                var ids = l.Value.ToList();
                ids.Sort(new Comparison<KeyValuePair<int, int>>((t1, t2) => Math.Sign(t2.Value - t1.Value)));
                links[l.Key] = ids.First().Key;
            }
            Console.WriteLine($"Построено {links.Count} цепочек");
        }

        public int ProposeNext(int[] c)
        {
            Link link = new Link(c);
            int next;
            links.TryGetValue(link, out next);
            return next;
        }
    }
}
