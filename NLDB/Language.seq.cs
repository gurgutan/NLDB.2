﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public partial class Language
    {
        private const int similars_max_count = 8;           //максимальное количество похожих слов при поиске
        private const float attenuation_coef = 40F;         //количество слов в ответе, которое обнуляет единицу confidence
        private const float similars_min_confidence = 0.7F;
        private const float followers_min_confidence = 0.02F;
        private const int followers_max_count = 16;
        private const int link_max_size = 8;                // Максимальный размер цепочек
        private const int links_max_count = 1 << 23;        // Количество цепочек для инициализации словаря

        private Dictionary<Sequence, int> sequences = new Dictionary<Sequence, int>(links_max_count);

        private Grammar grammar = new Grammar();

        //public int BuildSequences()
        //{
        //    sequences.Clear();
        //    int seq_counter = 0;
        //    Console.WriteLine("\nПостроение цепочек:");
        //    for (int size = 2; size <= link_max_size; size++)
        //    {
        //        Console.WriteLine($"...длины {size} ");
        //        foreach (var word in data)
        //        {
        //            if (word.rank == 0) continue;
        //            //"Гусеница" для формирования цепочки
        //            Queue<int> caterpillar = new Queue<int>();
        //            for (int i = 0; i < word.childs.Length - 1; i++)
        //            {
        //                caterpillar.Enqueue(word.childs[i]);
        //                if (caterpillar.Count < size) continue;
        //                //Проверим, есть ли цепочка в словаре
        //                Sequence seq = new Sequence(caterpillar.ToArray());
        //                int number;
        //                if (sequences.TryGetValue(seq, out number))
        //                {
        //                    sequences[seq] = number + 1;
        //                }
        //                else
        //                {
        //                    seq_counter++;
        //                    sequences[seq] = 1;
        //                    Debug.WriteLineIf(seq_counter % (1 << 17) == 0, seq_counter);
        //                }
        //                caterpillar.Dequeue();
        //            }
        //        }
        //    }
        //    return seq_counter;
        //}

        public int BuildGrammar()
        {
            grammar.Clear();
            Debug.WriteLine("Построение грамматики");
            var words = data.Where(w => w.rank > 0);
            int count = 0;
            foreach (var w in words)
            {
                grammar.Add(w.childs);
                count++;
                Debug.WriteLineIf(count % (1 << 16) == 0, count);
            }
            return grammar.Count();
        }


        private Link Follower(int[] iarray, IEnumerable<Link> constraints, float min_confidence = 1)
        {
            Queue<int> que = new Queue<int>(iarray);
            //уменьшаем до нужного размера
            while (que.Count >= link_max_size) que.Dequeue();
            Stack<int> stack = new Stack<int>(que);
            //Сокращаем цепочку слов до длины link_max_size 
            Link result = default(Link);
            while (stack.Count > 0 && (result.id == 0 || result.confidence < min_confidence))
            {
                int count_all = 0;
                foreach (var c in constraints)
                {
                    stack.Push(c.id);
                    Sequence sequence = new Sequence(stack.Reverse().ToArray());
                    int number;
                    if (sequences.TryGetValue(sequence, out number))
                    {
                        count_all += number;
                        if (result.number < number)
                        {
                            result.id = c.id;
                            result.number = number;
                        }
                    }
                    stack.Pop();
                }
                result.confidence = (float)result.number / count_all;
                que.Dequeue();
                stack = new Stack<int>(que);
            }
            return result;
        }

        public Term Predict(string text, int rank = 2)
        {
            var similars = this.Similars(text, rank).
                Where(t => t.confidence > similars_min_confidence).
                Take(similars_max_count);
            if (similars.Count() == 0) return null;
            //Первым в коллекции будет терм с максимальным confidence
            var best_similar = similars.First();

            //Список взвешенных идентификаторов слов-дочерних к parents
            var constraints = //parents.
                similars.SelectMany(s =>
                    data.GetParents(s.id).
                    SelectMany(p => p.childs.Select(c => new Link(c, 0, s.confidence)))).Distinct();
            //Идентификатор следующего слова за best_similar.id, оптимального в смысле произведения веса этого слова на вес слова из constraints
            var follower = Follower(new int[] { best_similar.id }, constraints);
            if (follower.id == 0) return null;
            return ToTerm(follower.id, follower.confidence);
        }

        public List<Term> PredictRecurrent(string text, int max_count = 1, int rank = 2)
        {
            //результат (цепочка термов)
            List<Term> result = new List<Term>();
            var similars = this.Similars(text, rank).
                Where(t => t.confidence > similars_min_confidence).
                Take(similars_max_count);
            if (similars.Count() == 0) return result;
            //Первым в коллекции будет терм с максимальным confidence
            var best_similar = similars.First();
            //Получаем ссылки на всех предков similars с confidence пронаследованным от similars
            var parents = similars.
                SelectMany(s => data.GetParents(s.id).Select(p => new Link(p.id, 0, s.confidence))).
                ToList();  //ToList() для отладки
            //"Мешок слов". Список взвешенных идентификаторов слов для пользования при составлении текста
            var constraints = parents.
                SelectMany(p => data.Get(p.id).childs.Select(c => new Link(c, 0, p.confidence))).Distinct().
                SelectMany(c => data.Get(c.id).childs.Select(gc => new Link(gc, 0, c.confidence))).Distinct().
                ToList();

            //Queue<int> seq = new Queue<int>();
            //foreach (var link in constraints)
            //{
            //    Link next = Follower(new int[] { link.id }, constraints);
            //    if (next.id == 0 || next.confidence < followers_min_confidence) continue;
            //    seq.Enqueue(next.id);
            //    result.Add(ToTerm(next.id, next.confidence));
            //    do
            //    {
            //        Term term = ToTerm(next.id, next.confidence);
            //        seq.Enqueue(term.id);
            //        result.Add(term);
            //        next = Follower(seq.ToArray(), constraints, followers_min_confidence);
            //    }
            //    while (next.confidence >= followers_min_confidence && result.Count < max_count);


            //}
            //TODO: Нужен алгоритм поиска цепочки с максимумом функции качества (? определить функцию)
            //Очередь термов, используемая для предсказания следующего терма
            Queue<int> seq = new Queue<int>(best_similar.Childs.Select(s => s.id));
            //Стартовое слово: первый среди потомков
            Link next = Follower(seq.ToArray(), constraints);
            int skip = 0;
            while (next.id == 0 && skip < best_similar.Childs.Count)
            {
                skip++;
                seq = new Queue<int>(best_similar.Childs.Skip(skip).Select(s => s.id));
                next = Follower(seq.ToArray(), constraints);
            }
            do
            {
                //Если результата нет, то выходим с тем что есть
                if (next.id == 0) break;
                Term term = ToTerm(next.id, next.confidence);
                seq.Enqueue(term.id);
                result.Add(term);
                next = Follower(seq.ToArray(), constraints, followers_min_confidence);
            }
            while (next.confidence >= followers_min_confidence && result.Count < max_count);
            return result;
        }


        public List<Term> Next(string text, int rank = 2)
        {
            //результат (цепочка термов)
            List<Term> result = new List<Term>();

            Stopwatch sw = new Stopwatch(); //!!!
            sw.Start(); //!!!
            var terms = Parse(text, rank).Select(t => ToTerm(t)).ToList();
            sw.Stop();  //!!!
            Debug.WriteLine($"Парсинг: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            var similars = Similars(text, rank).Where(t => t.confidence >= similars_min_confidence).Take(similars_max_count);
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение similars [{similars.Count()}]: {sw.Elapsed.TotalSeconds}");
            Debug.WriteLine(similars.Aggregate("", (c, n) => c + (c == "" ? "" : c + "\n" + n.ToString())));
            if (similars.Count() == 0) return result;
            //var constraints = similars.SelectMany(s => s.childs.Select(c => new Link(c.id, 0, s.confidence))).
            //    Distinct(new LinkComparer()).
            //    ToDictionary(link => link.id, link => link);
            //Получаем ссылки на всех предков similars с confidence пронаследованным от similars
            sw.Restart(); //!!!
            //запоминаем веса 
            Dictionary<int, float> weights = similars.ToDictionary(s => s.id, s => s.confidence);
            var constraints = data.
                GetParents(weights.Keys.ToArray()).
                    SelectMany(p =>
                        data.Get(p.Item2.id).childs.SelectMany(c =>
                            data.Get(c).childs.Select(gc =>
                                new Link(gc, 0, weights[p.Item1])))).
                Distinct(new LinkComparer()).
                ToDictionary(link => link.id, link => link);
            //var constraints = similars.
            //    SelectMany(s =>
            //        data.GetParents(s.id).SelectMany(p =>
            //            data.Get(p.id).childs.SelectMany(c =>
            //                data.Get(c).childs.Select(gc =>
            //                    new Link(gc, 0, s.confidence))))).
            //    Distinct(new LinkComparer()).
            //    ToDictionary(link => link.id, link => link);
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение constraints [{constraints.Count}]: {sw.Elapsed.TotalSeconds}");
            //Запоминаем словарь допустимых слов, для использования в поиске
            sw.Restart(); //!!!
            constraints.Add(grammar.Root.id, new Link(0, 0, 0));
            var path = FindSequence(grammar.Root, constraints);
            sw.Stop();  //!!!
            //Debug.WriteLine($"Определение sequence [{weight.ToString("F4")};{path.Count}]: {sw.Elapsed.TotalSeconds}");
            return path.Item2.Skip(1).Select(link => ToTerm(link.id)).ToList();
        }


        public Tuple<float, Stack<Link>> FindSequence(Rule rule, Dictionary<int, Link> bag)
        {
            if (!bag.ContainsKey(rule.id)) return null;
            float head_weight = bag[rule.id].confidence;
            float tail_weight = 0;

            Stack<Link> path = new Stack<Link>();
            Link found = new Link();
            foreach (var t in rule.Transitions)
            {
                //if (!bag.ContainsKey(t.Key)) continue;
                var cur_path = FindSequence(rule.Rules[t.Key], bag);
                if (cur_path == null) continue;
                if (tail_weight < cur_path.Item1)
                {
                    found = new Link(t.Key, 0, bag[t.Key].confidence);
                    tail_weight = cur_path.Item1;
                    path = cur_path.Item2;
                }
            }
            path.Push(new Link(rule.id, 0, head_weight));
            return new Tuple<float, Stack<Link>>(head_weight + tail_weight - path.Count / attenuation_coef, path);
        }

    }
}
