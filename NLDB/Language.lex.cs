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
        //private Alphabet alphabet = new Alphabet();
        //private Dictionary<int, Word> i2w = new Dictionary<int, Word>();
        //private Dictionary<Word, int> w2i = new Dictionary<Word, int>();


        public Language(string _name, string[] _splitters)
        {
            this.Name = _name;
            splitters = _splitters;
            parsers = splitters.Select(s => new Parser(s)).ToArray();
            data = new DataContainer(_name, splitters);
            //data.Open(Name);
        }

        public string Name { get; private set; }

        public int Rank { get { return data.Splitters.Length - 1; } }


        public string[] Splitters
        {
            get { return this.data.Splitters; }
        }

        public int Count { get { return data.Count(); } }

        private IEnumerable<int> Parse(string text, int rank)
        {
            text = this.parsers[rank].Normilize(text);
            var strings = this.parsers[rank].Split(text).Where(s => !string.IsNullOrEmpty(s));
            //Для слов ранга > 0 добавляем слова, которых еще нет
            return strings.Select(s =>
            {
                int id;
                int[] childs = new int[0];
                if (rank > 0)
                {
                    childs = Parse(s, rank - 1).ToArray();      //получаем id дочерних слов ранга rank-1
                    id = data.GetId(childs);
                    if (id == 0)
                        id = data.Add(new Word(0, rank, "", childs, new int[0]));
                }
                else
                {
                    id = data.GetId(s);
                    if (id == 0)
                        id = data.Add(new Word(0, rank, s, childs, new int[0]));
                }
                return id;
            });
        }

        public Term ToTerm(string text, int rank)
        {
            text = parsers[rank].Normilize(text);
            return new Term(rank, 0, 0, text, rank == 0 ?
                null :
                parsers[rank - 1].Split(text).
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
            return Evaluate(term);
        }

        public List<Term> Similars(string text, int rank = 2)
        {
            text = parsers[rank].Normilize(text);
            Stopwatch sw = new Stopwatch(); //!!!
            sw.Start(); //!!!
            Term term = ToTerm(text, rank);
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->ToTerm: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            Evaluate(term);
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->Evaluate: {sw.Elapsed.TotalSeconds}");
            //Для терма нулевого ранга возвращаем результат по наличию соответствующей буквы в алфавите
            if (term.rank == 0) return new List<Term> { term };
            //Выделение претендентов на роль ближайшего
            //var candidates = term.childs.                // дочерние термы
            //    Select(c => Evaluate(c)).               // вычисляем все значения confidence
            //    Where(c => c.id != 0).                  // отбрасываем термы с id=0, т.к. они не идентифицированы
            //    SelectMany(c => data.Get(c.id).parents.Select(i => data.Get(i))). // получаем список родительских слов
            //    Distinct().                             // без дублей
            //    Select(p => ToTerm(p)).                 // переводим слова в термы
            //    ToList();
            sw.Restart(); //!!!
            var childs = term.Childs.
                //Select(c => Evaluate(c)).
                Where(c => c.id != 0).
                Select(c => c.id).
                Distinct().
                ToArray();
            sw.Stop();  //!!!
            Debug.WriteLine($"Similars->childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
            sw.Restart(); //!!!
            var context = data.
                GetParents(childs).
                Select(c => c.Item2).
                Distinct(new WordComparer()).
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
        public Term Evaluate(Term term)
        {
            //if (term.Evaluated) return term;
            if (term.rank == 0)
            {
                //При нулевом ранге терма, confidence считаем исходя из наличия соответствующей буквы в алфавите
                term.id = data.GetId(term.text);
                term.confidence = (term.id == 0 ? 0 : 1);
                term.Evaluated = true;
                return term;
            }
            else
            {
                //Если ранг терма больше нуля, то confidence считаем по набору дочерних элементов
                //Выделение претендентов на роль ближайшего
                //var candidates = term.childs.            // дочерние термы
                //    Select(c => Evaluate(c)).
                //    Where(c => c.id != 0).              // отбрасываем термы с id=0, т.к. они не идентифицированы
                //    Select(c => data.Get(c.id));        // переводим термы в слова
                //SelectMany(w => w.parents.Select(i => data.Get(i))). // получаем список родительских слов
                //Distinct().                         // без дублей
                //Select(p => ToTerm(p)).             // переводим слова в термы
                //ToList();
                Stopwatch sw = new Stopwatch(); //!!!
                sw.Start(); //!!!
                var childs = term.Childs.
                    Distinct(new TermComparer()).
                    Select(c => Evaluate(c)).
                    Where(c => c.id != 0).
                    Select(c => c.id).
                    ToArray();
                sw.Stop();  //!!!
                Debug.WriteLine($"Evaluate->{term.ToString()}.childs [{childs.Length}]: {sw.Elapsed.TotalSeconds}");
                sw.Restart(); //!!!
                var context = data.
                    GetParents(childs).
                    Select(p => p.Item2).
                    //Distinct(new WordComparer()).
                    Select(p => ToTerm(p)).ToList();
                sw.Stop();  //!!!
                Debug.WriteLine($"Evaluate->{term.ToString()}.context [{context.Count}]: {sw.Elapsed.TotalSeconds}");
                //Поиск ближайшего родителя, т.е.родителя с максимумом сonfidence
                context.AsParallel().ForAll(p =>
                    {
                        float confidence = Confidence.Compare(term, p);
                        if (term.confidence < confidence)
                        {
                            term.id = p.id;
                            term.confidence = confidence;
                        }
                    });
                term.Evaluated = true;
                return term;
            }
        }

        //private int NextId()
        //{
        //    id_counter++;
        //    return id_counter;
        //}

    }
}
