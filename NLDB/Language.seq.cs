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
        private const int similars_max_count = 8;           //максимальное количество похожих слов при поиске
        private const float attenuation = 100F;         //количество слов в ответе, которое обнуляет confidence ответа
        private const float similars_min_confidence = 0.5F;
        private const float followers_min_confidence = 0.02F;
        private const int followers_max_count = 16;

        //private const int link_max_size = 8;                // Максимальный размер цепочек
        //private const int links_max_count = 1 << 23;        // Количество цепочек для инициализации словаря

        //private Dictionary<Sequence, int> sequences = new Dictionary<Sequence, int>(links_max_count);

        //private Link Follower(int[] iarray, IEnumerable<Link> constraints, float min_confidence = 1)
        //{
        //    Queue<int> que = new Queue<int>(iarray);
        //    //уменьшаем до нужного размера
        //    while (que.Count >= link_max_size) que.Dequeue();
        //    Stack<int> stack = new Stack<int>(que);
        //    //Сокращаем цепочку слов до длины link_max_size 
        //    Link result = default(Link);
        //    while (stack.Count > 0 && (result.id == 0 || result.confidence < min_confidence))
        //    {
        //        int count_all = 0;
        //        foreach (var c in constraints)
        //        {
        //            stack.Push(c.id);
        //            Sequence sequence = new Sequence(stack.Reverse().ToArray());
        //            int number;
        //            if (sequences.TryGetValue(sequence, out number))
        //            {
        //                count_all += number;
        //                if (result.number < number)
        //                {
        //                    result.id = c.id;
        //                    result.number = number;
        //                }
        //            }
        //            stack.Pop();
        //        }
        //        result.confidence = (float)result.number / count_all;
        //        que.Dequeue();
        //        stack = new Stack<int>(que);
        //    }
        //    return result;
        //}

        //предлагает следующее слово по первым нескольким
        //public Term Predict(string text, int rank = 2)
        //{
        //    var similars = this.Similars(text, rank).
        //        Where(t => t.confidence > similars_min_confidence).
        //        Take(similars_max_count);
        //    if (similars.Count() == 0) return null;
        //    //Первым в коллекции будет терм с максимальным confidence
        //    var best_similar = similars.First();

        //    //Список взвешенных идентификаторов слов-дочерних к parents
        //    var context = //parents.
        //        similars.SelectMany(s =>
        //            data.GetParents(s.id).
        //            SelectMany(p => p.childs.Select(c => new Link(c, 0, s.confidence)))).Distinct();
        //    //Идентификатор следующего слова за best_similar.id, оптимального в смысле произведения веса этого слова на вес слова из constraints
        //    var follower = Follower(new int[] { best_similar.id }, context);
        //    if (follower.id == 0) return null;
        //    return ToTerm(follower.id, follower.confidence);
        //}

        ////формирует цепочку слов по первым нескольким
        //public List<Term> PredictRecurrent(string text, int max_count = 1, int rank = 2)
        //{
        //    //результат (цепочка термов)
        //    List<Term> result = new List<Term>();
        //    var similars = this.Similars(text, rank).
        //        Where(t => t.confidence > similars_min_confidence).
        //        Take(similars_max_count);
        //    if (similars.Count() == 0) return result;
        //    //Первым в коллекции будет терм с максимальным confidence
        //    var best_similar = similars.First();
        //    //Получаем ссылки на всех предков similars с confidence пронаследованным от similars
        //    var parents = similars.
        //        SelectMany(s => data.GetParents(s.id).Select(p => new Link(p.id, 0, s.confidence))).
        //        ToList();  //ToList() для отладки
        //    //"Мешок слов". Список взвешенных идентификаторов слов для пользования при составлении текста
        //    var context = parents.
        //        SelectMany(p => data.Get(p.id).childs.Select(c => new Link(c, 0, p.confidence))).Distinct().
        //        SelectMany(c => data.Get(c.id).childs.Select(gc => new Link(gc, 0, c.confidence))).Distinct().
        //        ToList();
        //    //TODO: Нужен алгоритм поиска цепочки с максимумом функции качества (? определить функцию)
        //    //Очередь термов, используемая для предсказания следующего терма
        //    Queue<int> seq = new Queue<int>(best_similar.Childs.Select(s => s.id));
        //    //Стартовое слово: первый среди потомков
        //    Link next = Follower(seq.ToArray(), context);
        //    int skip = 0;
        //    while (next.id == 0 && skip < best_similar.Childs.Count)
        //    {
        //        skip++;
        //        seq = new Queue<int>(best_similar.Childs.Skip(skip).Select(s => s.id));
        //        next = Follower(seq.ToArray(), context);
        //    }
        //    do
        //    {
        //        //Если результата нет, то выходим с тем что есть
        //        if (next.id == 0) break;
        //        Term term = ToTerm(next.id, next.confidence);
        //        seq.Enqueue(term.id);
        //        result.Add(term);
        //        next = Follower(seq.ToArray(), context, followers_min_confidence);
        //    }
        //    while (next.confidence >= followers_min_confidence && result.Count < max_count);
        //    return result;
        //}

        public List<Term> Similars(string text, int rank = 2)
        {
            text = parsers[rank].Normilize(text);
            Stopwatch sw = new Stopwatch(); //!!!
            sw.Start(); //!!!
            Term term = ToTerm(text, rank);
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->ToTerm: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            Identify(term);
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->Identify: {sw.Elapsed.TotalSeconds}");
            //Для терма нулевого ранга возвращаем результат по наличию соответствующей буквы в алфавите
            if (term.rank == 0) return new List<Term> { term };
            //Определение контекста по дочерним словам
            sw.Restart(); //!!!
            var childs = term.Childs.
                Where(c => c.id != 0).
                Select(c => c.id).
                Distinct().
                ToArray();
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            var context = data.
                GetParentsId(childs).
                Distinct().
                Select(p => ToTerm(p)).
                ToList();
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
            //Расчет оценок Confidence для каждого из соседей
            sw.Restart(); //!!!
            context.AsParallel().ForAll(p => p.confidence = Confidence.Compare(term, p));
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->Compare [{context.Count}]: {sw.Elapsed.TotalSeconds}");
            //Сортировка по убыванию оценки
            context.Sort(new Comparison<Term>((t1, t2) => Math.Sign(t2.confidence - t1.confidence)));
            return context.ToList();
        }

        /// <summary>
        /// Вычисляет значение confidence и id для терма term. Меняет переданный по ссылке term
        /// </summary>
        /// <param name="term">изменяемый терм</param>
        /// <returns>возвращает ссылку на term (возврат значения для удобства использования в LINQ)</returns>
        public Term Identify(Term term)
        {
            if (term.Identified) return term;
            if (term.rank == 0)
            {
                //При нулевом ранге терма, confidence считаем исходя из наличия соответствующей буквы в алфавите
                term.id = data.GetId(term.text);
                term.confidence = (term.id == 0 ? 0 : 1);
                term.Identified = true;
                return term;
            }
            else
            {
                //Если ранг терма больше нуля, то confidence считаем по набору дочерних элементов
                Stopwatch sw = new Stopwatch(); //!!!
                sw.Start(); //!!!
                var childs = term.Childs.
                    Distinct(new TermComparer()).
                    Select(c => Identify(c)).
                    Where(c => c.id != 0).
                    Select(c => c.id).
                    ToArray();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                var parents = data.GetParentsId(childs).ToList();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.parents [{parents.Count}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                var context = parents.Select(p => ToTerm(p)).ToList();
                sw.Stop();  //!!!
                Debug.WriteLine($"Identify->{term.ToString()}.context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
                //Поиск ближайшего родителя, т.е.родителя с максимумом сonfidence
                var max = context.AsParallel().Aggregate(
                    new Link(),
                    (subtotal, thread_term) =>
                    {
                        float confidence = Confidence.Compare(term, thread_term);
                        if (subtotal.confidence < confidence) return new Link(thread_term.id, 0, confidence);
                        return subtotal;
                    },
                    (total, subtotal) => total.confidence < subtotal.confidence ? subtotal : total,
                    (final) => final);
                term.id = max.id;
                term.confidence = max.confidence;
                term.Identified = true;
                return term;
            }
        }


        public List<Term> Next(string text, int rank = 2)
        {
            List<Term> result = new List<Term>();
            Stopwatch sw = new Stopwatch(); //!!!
            sw.Start(); //!!!
            var terms = Parse(text, rank).
                Select(t => ToTerm(t)).
                ToList();
            sw.Stop();  //!!!
            Debug.WriteLine($"Парсинг: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            var similars = Similars(text, rank).Where(t => t.confidence >= similars_min_confidence).Take(similars_max_count);
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение similars [{similars.Count()}]: {sw.Elapsed.TotalSeconds}");
            similars.ToList().ForEach(s => Debug.WriteLine($" [{s.confidence.ToString("F4")}] {s}"));    //!!!
            if (similars.Count() == 0) return result;
            //запоминаем веса 
            Dictionary<int, float> weights = similars.ToDictionary(s => s.id, s => s.confidence);
            sw.Restart(); //!!!
            //Получаем контекст
            var context = data.
                GetParentsWithChilds(weights.Keys.ToArray()).
                SelectMany(p => data.GetGrandchildsId(p.Item2.id).
                Select(gc => new Link(gc, 0, weights[p.Item1]))).
                Distinct(new LinkComparer()).
                ToDictionary(link => link.id, link => link);
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
            //Запоминаем словарь допустимых слов, для использования в поиске
            sw.Restart(); //!!!
            context.Add(grammar.Root.id, new Link(0, 0, 0));
            var path = FindSequence(grammar.Root, context);
            sw.Stop();  //!!!
            Debug.WriteLine($"Определение path [{path.Item1.ToString("F4")};{path.Item2.Count}]: {sw.Elapsed.TotalSeconds}");
            return path.Item2.Skip(1).Select(link => ToTerm(link.id)).ToList();
        }

        public Tuple<float, Stack<Link>> FindSequence(Rule rule, Dictionary<int, Link> context)
        {
            if (!context.ContainsKey(rule.id)) return null;
            float head_weight = context[rule.id].confidence;
            float tail_weight = 0;
            Stack<Link> path = new Stack<Link>();
            Link found = new Link();
            foreach (var t in rule.Rules)
            {
                //if (!bag.ContainsKey(t.Key)) continue;
                var cur_path = FindSequence(rule.Rules[t.Key], context);
                if (cur_path == null) continue;
                if (tail_weight < cur_path.Item1)
                {
                    //критерий определяется как максимум сумм произведений вероятности слова в цепочке на confidence слова
                    found = new Link(t.Key, 0, context[t.Key].confidence * rule.Confidence(t.Key));
                    tail_weight = cur_path.Item1;
                    path = cur_path.Item2;
                }
            }
            path.Push(new Link(rule.id, 0, head_weight));
            return new Tuple<float, Stack<Link>>((head_weight + tail_weight) / (path.Count / attenuation), path);
        }

    }
}
