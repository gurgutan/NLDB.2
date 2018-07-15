using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public partial class Language
    {
        private const int max_similars = 16;        //максимальное количество похожих слов при поиске
        private const int links_max_count = 1 << 23;// Количество цепочек для инициализации словаря
        private const int link_max_size = 3;        // Максимальный размер цепочек
        Dictionary<Sequence, Link[]> links = new Dictionary<Sequence, Link[]>(links_max_count);

        public void BuildSequences()
        {
            //Значения словаря - словари Dictionary<int, int>, ключ - id, значение - количество
            Dictionary<Sequence, Dictionary<int, int>> temp_links = new Dictionary<Sequence, Dictionary<int, int>>(links_max_count);
            Console.WriteLine("Построение цепочек...");
            var words = w2i.Keys;
            for (int size = 1; size < link_max_size; size++)
            {
                Console.WriteLine($"...длины {size + 1}");
                foreach (var word in words)
                {
                    if (word.rank == 0) continue;
                    Queue<int> que = new Queue<int>();
                    for (int i = 0; i < word.childs.Length - 1; i++)
                    {
                        que.Enqueue(word.childs[i]);
                        if (que.Count >= size)
                        {
                            Sequence link = new Sequence(que.ToArray());
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
                Console.CursorLeft = 0;
            }
            Console.WriteLine($"Обработка цепочек...");
            foreach (var l in temp_links)
            {
                var variants = l.Value.ToList();
                int sum = variants.Sum(i => i.Value);
                var followers = variants.Select(kvp => new Link(kvp.Key, (double)kvp.Value / (double)sum)).ToList();
                followers.Sort(new Comparison<Link>((t1, t2) => Math.Sign(t2.confidence - t2.confidence)));
                links[l.Key] = followers.ToArray();
            }
            Console.WriteLine($"Построено {links.Count} цепочек");
        }

        private Link[] Followers(int[] seq)
        {
            Sequence link = new Sequence(seq);
            Link[] followers;
            links.TryGetValue(link, out followers);
            return followers;
        }

        private Link Follower(int[] seq, IEnumerable<Link> constraints)
        {
            Sequence link = new Sequence(seq);
            Link[] followers;
            links.TryGetValue(link, out followers);
            Link result = default(Link);
            if (followers == null) return result;
            foreach (var c in constraints)
            {
                foreach (var f in followers)
                {
                    if (result.confidence < c.confidence * f.confidence)
                    {
                        result.id = f.id;
                        result.confidence = c.confidence * f.confidence;
                    }
                }
            }
            return result;
        }

        public Term Predict(string text, int rank = 2)
        {
            var similars = this.Similars(text, rank, max_similars);
            if (similars.Count() == 0) return null;
            //Первым в коллекции будет терм с максимальным confidence
            var best_similar = similars.First();
            //Получаем ссылки на всех предков similars с confidence пронаследованным от similars
            var parents = similars.SelectMany(s => Get(s.id).parents.Select(p => new Link(p, s.confidence))).ToList();  //ToList() для отладки
            //Список взвешенных идентификаторов слов-дочерних к parents
            var constaints = parents.SelectMany(p => Get(p.id).childs.Select(c => new Link(c, p.confidence))).ToList(); //ToList() для отладки
            //Идентификатор следующего слова за best_similar.id, оптимального в смысле произведения веса этого слова на вес слова из constraints
            var follower = Follower(new int[] { best_similar.id }, constaints);
            if (follower.id == 0) return null;
            var term = ToTerm(Get(follower.id));
            term.confidence = follower.confidence;
            return term;
        }

        public List<Term> PredictRecurrent(string text, int max_count = 4)
        {
            int rank = 2;
            var similars = this.Similars(text, rank, max_similars);
            if (similars.Count() == 0) return null;
            //Первым в коллекции будет терм с максимальным confidence
            var best_similar = similars.First();
            //Получаем ссылки на всех предков similars с confidence пронаследованным от similars
            var parents = similars.SelectMany(s => Get(s.id).parents.Select(p => new Link(p, s.confidence))).ToList();  //ToList() для отладки
            //Список взвешенных идентификаторов слов-дочерних к parents
            var childs = parents.SelectMany(p => Get(p.id).childs.Select(c => new Link(c, p.confidence))).ToList(); //ToList() для отладки
            var grandchilds = childs.SelectMany(c => Get(c.id).childs.Select(gc => new Link(gc, c.confidence))).ToList();
            grandchilds.Sort(new Comparison<Link>((t1, t2) => Math.Sign(t2.confidence - t2.confidence)));
            //Идентификатор следующего слова за best_similar.id, оптимального в смысле произведения веса этого слова на вес слова из constraints
            List<Term> result = new List<Term>();
            Queue<Term> seq = new Queue<Term>();
            var next = new Link(grandchilds.First().id, grandchilds.First().confidence);
            for (int count = 1; count < max_count && next.id > 0; count++)
            {
                Term term = ToTerm(Get(next.id));
                term.confidence = next.confidence;
                seq.Enqueue(term);
                next = Follower(seq.Select(e => e.id).ToArray(), grandchilds);
                if (next.id == 0) break;
                result.Add(term);
                if (seq.Count > link_max_size) seq.Dequeue();
            }
            return result;
        }


    }
}
