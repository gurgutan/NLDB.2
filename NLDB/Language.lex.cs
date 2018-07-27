using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace NLDB
{
    public partial class Language
    {
        private Alphabet alphabet = new Alphabet();
        //private Dictionary<int, Word> i2w = new Dictionary<int, Word>();
        //private Dictionary<Word, int> w2i = new Dictionary<Word, int>();
        Dictionary<Sequence, int> words_exists = new Dictionary<Sequence, int>(1 << 24);


        public Language(string _name, string[] _splitters)
        {
            data = new DataContainer(_name, _splitters);
            parsers = data.Splitters.Select(s => new Parser(s)).ToArray();
            this.Name = _name;
            //data = new DataContainer(splitters, alphabet, i2w, w2i, links);
        }

        public string Name { get; private set; }

        public int Rank { get { return data.Splitters.Length - 1; } }


        public string[] Splitters
        {
            get { return this.data.Splitters; }
        }

        public int Count { get { return data.Count(); } }

        //public Dictionary<int, Word> Words { get { return i2w; } }

        /// <summary>
        /// Возвращает слово из словаря по идентификатору.
        /// </summary>
        /// <param name="i"></param>
        /// <returns>Слово из словаря или null, если нет слова с id=i</returns>
        //public Word Get(int i)
        //{
        //    Word w;
        //    i2w.TryGetValue(i, out w);
        //    return w;
        //    //return data.Get(i);
        //}

        //public int Add(Word w, string letter = "")
        //{
        //    w.id = NextId();
        //    i2w[w.id] = w;
        //    w2i[w] = w.id;
        //    //Добавление родительского слова в каждое из дочерних слов w.childs
        //    Array.ForEach(w.childs, c => Get(c).AddParent(w.id));
        //    //Добавление буквы в алфавит
        //    if (!string.IsNullOrEmpty(letter) && !alphabet.Contains(letter))
        //        alphabet.Add(letter, w.id);
        //    return w.id;
        //} 

        //private IEnumerable<int> Parse(string text, int rank)
        //{
        //    text = this.parsers[rank].Normilize(text);
        //    var strings = this.parsers[rank].Split(text).Where(s => !string.IsNullOrEmpty(s));
        //    //Для букв (слов ранга 0) возвращаем id букв, добавляя новые в алфавит, если надо
        //    if (rank == 0)
        //    {
        //        return strings.Select(s =>
        //        {
        //            if (alphabet.Contains(s)) return alphabet[s];
        //            return Add(new Word(0, 0, new int[0], new int[0]), s);
        //        });
        //    }
        //    else
        //    {
        //        //Для слов ранга > 0 добавляем слова, которых еще нет
        //        var ids = strings.Select(s =>
        //        {
        //            var childs = Parse(s, rank - 1).ToArray();      //получаем id дочерних слов ранга rank-1
        //            int id = Find(childs, rank);                    //ищем соответствие в словаре, если нет, то id=0
        //            if (id == 0)
        //                id = Add(new Word(0, rank, childs, new int[0]));   //если не нашли, - добавляем
        //            return id;
        //        });
        //        return ids.ToList();
        //    }
        //}

        private IEnumerable<int> Parse(string text, int rank)
        {
            text = this.parsers[rank].Normilize(text);
            var strings = this.parsers[rank].Split(text).Where(s => !string.IsNullOrEmpty(s));
            //Для слов ранга > 0 добавляем слова, которых еще нет
            return strings.Select(s =>
            {
                int id;
                int[] childs = new int[0];
                Word w = null;
                if (rank > 0)
                {
                    childs = Parse(s, rank - 1).ToArray();      //получаем id дочерних слов ранга rank-1
                    Sequence childs_seq = new Sequence(childs);
                    if (!words_exists.TryGetValue(childs_seq, out id))
                    {
                        id = data.Add(new Word(0, rank, "", childs, new int[0]));
                        words_exists[childs_seq] = id;
                    }
                    //w = data.Get(childs);
                }
                else
                {
                    if (alphabet.Contains(s))
                        id = alphabet[s];
                    else
                    {
                        id = data.Add(new Word(0, rank, s, childs, new int[0]));
                        alphabet.Add(s, id);
                    }
                    //w = data.Get(s);
                }
                return id;
            });
        }

        /// <summary>
        /// Возвращает id слова, если существует слово с набором id дочерних слов childs, или 0, если такое слово не найдено
        /// </summary>
        /// <param name="childs">набор id дочерних слов</param>
        /// <param name="rank">ранг искомого слова</param>
        /// <returns></returns>
        //public int Find(int[] childs, int rank)
        //{
        //    //return data.Get(childs).id;
        //    Word word = new Word(0, rank, childs, new int[0]);
        //    int id;
        //    w2i.TryGetValue(word, out id);
        //    return id;
        //}

        public Term ToTerm(string text, int rank)
        {
            text = parsers[rank].Normilize(text);
            return new Term(rank, 0, 0, text, rank == 0 ?
                null :
                parsers[rank - 1].Split(text).
                Where(s => !string.IsNullOrWhiteSpace(s)).
                Select(s => ToTerm(s, rank - 1)));
        }

        public Term ToTerm(Word w)
        {
            return new Term(
                w.rank,
                w.id,
                _confidence: 1,
                _text: w.symbol,
                _childs: w.rank == 0 ? null : w.childs.Select(c => ToTerm(data.Get(c))));
        }

        public Term ToTerm(Word w, float _confidence)
        {
            return new Term(
                w.rank,
                w.id,
                _confidence,
                w.symbol,
                w.rank == 0 ? null : w.childs.Select(c => ToTerm(data.Get(c))));
        }

        public Term ToTerm(int i)
        {
            var word = data.Get(i);
            return word == null ? null : ToTerm(word);
        }

        /// <summary>
        /// Вычисляет ближайший терм ранга rank к тексту text
        /// </summary>
        /// <param name="text">искомая строка</param>
        /// <param name="rank">ранг терма</param>
        /// <returns></returns>
        public Term Similar(string text, int rank)
        {
            text = parsers[rank].Normilize(text);
            Term term = ToTerm(text, rank);
            return Evaluate(term);
        }

        public List<Term> Similars(string text, int count = 0, int rank = 2)
        {
            text = parsers[rank].Normilize(text);
            Term term = ToTerm(text, rank);
            //Для терма нулевого ранга возвращаем результат по наличию соответствующей буквы в алфавите
            if (term.rank == 0) return new List<Term> { Evaluate(term) };
            //Выделение претендентов на роль ближайшего
            //var candidates = term.childs.                // дочерние термы
            //    Select(c => Evaluate(c)).               // вычисляем все значения confidence
            //    Where(c => c.id != 0).                  // отбрасываем термы с id=0, т.к. они не идентифицированы
            //    SelectMany(c => data.Get(c.id).parents.Select(i => data.Get(i))). // получаем список родительских слов
            //    Distinct().                             // без дублей
            //    Select(p => ToTerm(p)).                 // переводим слова в термы
            //    ToList();
            var childs = term.
                childs.
                Select(c => Evaluate(c)).
                Where(c => c.id != 0).
                Select(c => c.id).ToArray();
            var candidates = data.
                GetParents(childs).
                Select(p => ToTerm(p)).ToList();
            if (count == 0) count = candidates.Count;
            //Расчет оценок Confidence для каждого из соседей
            candidates.AsParallel().
                ForAll(p => p.confidence = Confidence.Compare(term, p));
            //Сортировка по убыванию оценки
            candidates.Sort(new Comparison<Term>((t1, t2) => Math.Sign(t2.confidence - t1.confidence)));
            return candidates.Take(count).ToList();
        }

        /// <summary>
        /// Вычисляет значение confidence и id для терма term. Меняет переданный по ссылке term
        /// </summary>
        /// <param name="term">изменяемый терм</param>
        /// <returns>возвращает ссылку на term (возврат значения для удобства использования в LINQ)</returns>
        public Term Evaluate(Term term)
        {
            if (term.rank == 0)
            {
                //При нулевом ранге терма, confidence считаем исходя из наличия соответствующей буквы в алфавите
                term.id = data.Get(term.text).id;
                term.confidence = (term.id == 0 ? 0 : 1);
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

                var childs = term.childs.Select(c => Evaluate(c)).Where(c => c.id != 0).Select(c => c.id).ToArray();
                var candidates = data.GetParents(childs).Select(p => ToTerm(p));
                //Поиск ближайшего родителя, т.е.родителя с максимумом сonfidence
                candidates.AsParallel().ForAll(p =>
                {
                    float confidence = Confidence.Compare(term, p);
                    if (term.confidence < confidence)
                    {
                        term.id = p.id;
                        term.confidence = confidence;
                    }
                });

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
