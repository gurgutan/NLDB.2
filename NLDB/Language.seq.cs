using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public partial class Language
    {
        private const int similars_max_count = 4;           //максимальное количество похожих слов при поиске
        private const float similars_min_confidence = 0.8F;
        private const float followers_min_confidence = 0.02F;
        private const int followers_max_count = 16;
        private const int link_max_size = 6;                // Максимальный размер цепочек
        private const int links_max_count = 1 << 23;        // Количество цепочек для инициализации словаря

        //public void BuildSequences()
        //{
        //    //Значения словаря - словари Dictionary<int, int>, ключ - id, значение - количество
        //    Dictionary<Sequence, Dictionary<int, int>> temp_links = new Dictionary<Sequence, Dictionary<int, int>>(links_max_count);
        //    Console.WriteLine("\nПостроение цепочек:");
        //    for (int size = 1; size < link_max_size; size++)
        //    {
        //        Console.WriteLine($"...длины {size + 1}");
        //        foreach (var word in data)
        //        {
        //            if (word.rank == 0) continue;
        //            //"Гусеница" для формирования цепочки
        //            Queue<int> que = new Queue<int>();
        //            for (int i = 0; i < word.childs.Length - 1; i++)
        //            {
        //                que.Enqueue(word.childs[i]);
        //                if (que.Count >= size)
        //                {
        //                    Sequence link = new Sequence(que.ToArray());
        //                    Dictionary<int, int> d;
        //                    if (!temp_links.TryGetValue(link, out d))
        //                    {
        //                        d = new Dictionary<int, int>();
        //                        temp_links[link] = d;
        //                    }
        //                    if (!d.ContainsKey(word.childs[i + 1])) d[word.childs[i + 1]] = 0;
        //                    d[word.childs[i + 1]]++;
        //                    que.Dequeue();
        //                }
        //            }
        //        }
        //        Console.CursorLeft = 0;
        //    }
        //    Console.WriteLine($"Обработка цепочек...");
        //    foreach (var l in temp_links)
        //    {
        //        var variants = l.Value.ToList();
        //        int sum = variants.Sum(i => i.Value);
        //        var followers = variants.Select(kvp => new Link(kvp.Key, (float)kvp.Value / (float)sum)).ToList();
        //        followers.Sort(new Comparison<Link>((t1, t2) => Math.Sign(t2.confidence - t1.confidence)));
        //        links[l.Key] = followers.Take(followers_max_count).ToArray();
        //    }
        //    Console.WriteLine($"Построено {links.Count} цепочек");
        //}

        public int BuildSequences()
        {
            int seq_counter = 0;
            Console.WriteLine("\nПостроение цепочек:");
            for (int size = 1; size < link_max_size; size++)
            {
                Dictionary<Sequence, int> exists = new Dictionary<Sequence, int>(1 << 24);
                Console.WriteLine($"...длины {size + 1}");
                data.BeginTransaction();    //в транзакции каждый массив цепочек
                foreach (var word in data)
                {
                    if (word.rank == 0) continue;
                    //"Гусеница" для формирования цепочки
                    Queue<int> que = new Queue<int>();
                    for (int i = 0; i < word.childs.Length - 1; i++)
                    {
                        que.Enqueue(word.childs[i]);
                        if (que.Count < size) continue;
                        int[] que_array = que.ToArray();
                        //Создадим и заполним полную цепочку - с (i+1)-м словом
                        int[] full_que_array = new int[size + 1];
                        que_array.CopyTo(full_que_array, 0);
                        full_que_array[size] = word.childs[i + 1];
                        //Проверим, есть ли полная цепочка в словаре
                        Sequence full_seq = new Sequence(full_que_array);
                        int number;
                        if (exists.TryGetValue(full_seq, out number))
                        {
                            number++;
                            exists[full_seq] = number;
                            data.ReplaceLink(que_array, word.childs[i + 1], number);
                        }
                        else
                        {
                            seq_counter++;
                            exists[new Sequence(full_que_array)] = 1;
                            Debug.WriteLineIf(seq_counter % (1 << 14) == 0, seq_counter);
                            data.InsertLink(que_array, word.childs[i + 1], 1);
                        }
                        //var link = data.GetLink(seq, word.childs[i + 1]);
                        //if (link.id == 0)
                        //{
                        //    seq_counter++;
                        //    Debug.WriteLineIf(seq_counter % (1 << 14) == 0, seq_counter);
                        //    data.InsertLink(seq, word.childs[i + 1], 1);
                        //}
                        //else
                        //    data.ReplaceLink(seq, word.childs[i + 1], link.number + 1);
                        que.Dequeue();
                    }
                }
                data.EndTransaction();
                Console.CursorLeft = 0;
            }
            return seq_counter;
        }

        private IEnumerable<Link> Followers(int[] seq)
        {
            return data.GetLinks(seq);
        }

        private Link Follower(int[] seq, IEnumerable<Link> constraints, float min_confidence = 1)
        {
            Link result = default(Link);
            Queue<int> que = new Queue<int>(seq);
            //Сокращаем цепочку слов до длины link_max_size 
            while (que.Count > link_max_size) que.Dequeue();
            while (result.confidence <= min_confidence && que.Count > 0 && result.id == 0)
            {
                var followers = data.GetLinks(que.ToArray());
                if (followers == null)
                    que.Dequeue();
                else
                    foreach (var c in constraints)
                        foreach (var f in followers)
                            if (result.confidence < c.confidence * f.confidence)
                            {
                                result = new Link(f.id, f.number, c.confidence * f.confidence);
                                if (result.confidence == 1) return result;
                            }
            }
            return result;
        }

        public Term Predict(string text, int rank = 2)
        {
            var similars = this.Similars(text, similars_max_count, rank).
                Where(t => t.confidence > similars_min_confidence).
                Take(similars_max_count);
            if (similars.Count() == 0) return null;
            //Первым в коллекции будет терм с максимальным confidence
            var best_similar = similars.First();
            //Получаем ссылки на всех предков similars с confidence пронаследованным от similars
            var parents = similars.
                SelectMany(s => data.Get(s.id).parents.Select(p => new Link(p, 0, s.confidence))).
                Distinct().
                ToList();  //ToList() для отладки
            //Список взвешенных идентификаторов слов-дочерних к parents
            var constaints = parents.
                SelectMany(p => data.Get(p.id).childs.Select(c => new Link(c, 0, p.confidence))).
                Distinct().
                ToList(); //ToList() для отладки
            //Идентификатор следующего слова за best_similar.id, оптимального в смысле произведения веса этого слова на вес слова из constraints
            var follower = Follower(new int[] { best_similar.id }, constaints);
            if (follower.id == 0) return null;
            return ToTerm(data.Get(follower.id), follower.confidence);
        }

        public List<Term> PredictRecurrent(string text, int max_count = 4, int rank = 2)
        {
            //результат (цепочка термов)
            List<Term> result = new List<Term>();
            var similars = this.Similars(text, similars_max_count, rank).Where(t => t.confidence > similars_min_confidence).Take(similars_max_count);
            if (similars.Count() == 0) return result;
            //Первым в коллекции будет терм с максимальным confidence
            var best_similar = similars.First();
            //Получаем ссылки на всех предков similars с confidence пронаследованным от similars
            var parents = similars.SelectMany(s => data.Get(s.id).parents.Select(p => new Link(p, 0, s.confidence))).ToList();  //ToList() для отладки
            //"Мешок слов". Список взвешенных идентификаторов слов для пользования при составлении текста
            var constaints = parents.
                SelectMany(p => data.Get(p.id).childs.Select(c => new Link(c, 0, p.confidence))).Distinct().
                SelectMany(c => data.Get(c.id).childs.Select(gc => new Link(gc, 0, c.confidence))).Distinct().
                ToList();
            //Очередь термов, используемая для предсказания следующего терма
            Queue<int> seq = new Queue<int>(best_similar.childs.Select(s => s.id));
            //Стартовое слово: первый среди потомков
            Link next = Follower(seq.ToArray(), constaints);
            int size = seq.Count;
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
                Term term = ToTerm(data.Get(next.id), next.confidence);
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
