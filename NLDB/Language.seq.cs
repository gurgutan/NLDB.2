using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public partial class Language
    {
        private const int similars_max_count = 4;           //максимальное количество похожих слов при поиске
        private const float similars_min_confidence = 0.7F;
        private const float followers_min_confidence = 0.02F;
        private const int followers_max_count = 16;
        private const int link_max_size = 6;                // Максимальный размер цепочек
        private const int links_max_count = 1 << 23;        // Количество цепочек для инициализации словаря
        Dictionary<Sequence, Link[]> links = new Dictionary<Sequence, Link[]>(links_max_count);

        public void BuildSequences()
        {
            //Значения словаря - словари Dictionary<int, int>, ключ - id, значение - количество
            Dictionary<Sequence, Dictionary<int, int>> temp_links = new Dictionary<Sequence, Dictionary<int, int>>(links_max_count);
            Console.WriteLine("\nПостроение цепочек:");
            var words = this.words.Keys;
            for (int size = 1; size < link_max_size; size++)
            {
                Console.WriteLine($"...длины {size + 1}");
                foreach (var word in words)
                {
                    if (word.rank == 0) continue;
                    //"Гусеница" для формирования цепочки
                    Queue<int> que = new Queue<int>();
                    for (int i = 0; i < word.childs.Length - 1; i++)
                    {
                        que.Enqueue(word.childs[i]);
                        if (que.Count >= size)
                        {
                            Sequence link = new Sequence(que.ToArray());
                            Dictionary<int, int> d;
                            if (!temp_links.TryGetValue(link, out d))
                            {
                                d = new Dictionary<int, int>();
                                temp_links[link] = d;
                            }
                            if (!d.ContainsKey(word.childs[i + 1])) d[word.childs[i + 1]] = 0;
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
                var followers = variants.Select(kvp => new Link(kvp.Key, (float)kvp.Value / (float)sum)).ToList();
                followers.Sort(new Comparison<Link>((t1, t2) => Math.Sign(t2.confidence - t1.confidence)));
                links[l.Key] = followers.Take(followers_max_count).ToArray();
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

        private Link Follower(int[] seq, IEnumerable<Link> constraints, float min_confidence = 1)
        {
            Link result = default(Link);
            Queue<int> que = new Queue<int>(seq);
            //Сокращаем цепочку слов до длины link_max_size 
            while (que.Count > link_max_size) que.Dequeue();
            while (result.confidence <= min_confidence && que.Count > 0 && result.id == 0)
            {
                Sequence link = new Sequence(que.ToArray());
                Link[] followers;
                if (!links.TryGetValue(link, out followers)) que.Dequeue();
                else
                    foreach (var c in constraints)
                        foreach (var f in followers)
                            if (result.confidence < c.confidence * f.confidence)
                            {
                                result = new Link(f.id, c.confidence * f.confidence);
                                if (result.confidence == 1) return result;
                            }
            }
            return result;
        }

        public Term Predict(string text, int rank = 2)
        {
            var similars = this.Similars(text, similars_max_count, rank).Where(t => t.confidence >= similars_min_confidence).Take(similars_max_count);
            if (similars.Count() == 0) return null;
            //Первым в коллекции будет терм с максимальным confidence
            var best_similar = similars.First();
            //Получаем ссылки на всех предков similars с confidence пронаследованным от similars
            var parents = similars.
                SelectMany(s => Get(s.id).parents?.Select(p => new Link(p, s.confidence))).
                Distinct().
                ToList();  //ToList() для отладки
            //Список взвешенных идентификаторов слов-дочерних к parents
            var constaints = parents.
                SelectMany(p => Get(p.id).childs.Select(c => new Link(c, p.confidence))).
                Distinct().
                ToList(); //ToList() для отладки
            //Идентификатор следующего слова за best_similar.id, оптимального в смысле произведения веса этого слова на вес слова из constraints
            var follower = Follower(new int[] { best_similar.id }, constaints);
            if (follower.id == 0) return null;
            return ToTerm(Get(follower.id), follower.confidence);
        }

        public List<Term> PredictRecurrent(string text, int max_count = 4, int rank = 2)
        {
            //результат (цепочка термов)
            List<Term> result = new List<Term>();
            var similars = this.Similars(text, similars_max_count, rank).Where(t => t.confidence > similars_min_confidence).Take(similars_max_count);
            if (similars.Count() == 0) return null;
            //Первым в коллекции будет терм с максимальным confidence
            var best_similar = similars.First();
            //Получаем ссылки на всех предков similars с confidence пронаследованным от similars
            var parents = similars.SelectMany(s => Get(s.id).parents?.Select(p => new Link(p, s.confidence))).ToList();  //ToList() для отладки
            //"Мешок слов". Список взвешенных идентификаторов слов для пользования при составлении текста
            var constaints = parents.
                SelectMany(p => Get(p.id).childs.Select(c => new Link(c, p.confidence))).Distinct().
                SelectMany(c => Get(c.id).childs.Select(gc => new Link(gc, c.confidence))).Distinct().
                ToList();
            //constaints.Sort(new Comparison<Link>((t1, t2) => Math.Sign(t2.confidence - t1.confidence)));
            //Очередь термов, используемая для предсказания следующего терма
            Queue<int> seq = new Queue<int>(best_similar.childs.Select(s => s.id));
            //Queue<int> seq = new Queue<int>(new int[] { best_similar.childs.First().id });
            //var follower = Follower(new int[] { best_similar.id }, constaints);
            //Queue<int> seq = new Queue<int>(new int[] { Get(follower.id).childs.First() });

            //Начальное следующее слово
            Link next = Follower(seq.ToArray(), constaints);
            int size = seq.Count;
            //Если начальное следующее слово не найдено, то сокращаем цепочку
            while (next.id == 0)
            {
                size--;
                seq = new Queue<int>(best_similar.childs.Take(size).Select(s => s.id));
                next = Follower(seq.ToArray(), constaints);
            }

            do
            {
                //Если результата нет, то выходим с тем что есть
                if (next.id == 0) break;
                Term term = ToTerm(Get(next.id), next.confidence);
                seq.Enqueue(term.id);
                result.Add(term);
                next = Follower(seq.ToArray(), constaints, followers_min_confidence);
                //if (seq.Count > link_max_size) seq.Dequeue();
            }
            while (next.confidence >= followers_min_confidence && result.Count < max_count);
            return result;
        }



    }
}
