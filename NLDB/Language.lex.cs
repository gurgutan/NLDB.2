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
        private Dictionary<Word, Word> words = new Dictionary<Word, Word>();

        public Language(string _name, string[] _splitters)
        {
            this.Name = _name;
            this.Splitters = _splitters;
        }

        public string Name { get; private set; }

        public int Rank { get { return splitters.Length - 1; } }


        public string[] Splitters
        {
            get { return this.splitters; }
            set { this.splitters = value; parsers = splitters.Select(s => new Parser(s)).ToArray(); }
        }

        public int Count { get { return words.Count; } }

        public Dictionary<Word, Word> Words { get { return words; } }

        /// <summary>
        /// Возвращает слово из словаря по идентификатору.
        /// </summary>
        /// <param name="i"></param>
        /// <returns>Слово из словаря или null, если нет слова с id=i</returns>
        public Word Get(int i)
        {
            Word w = new Word(i, 0, null, null);
            Word result;
            words.TryGetValue(w, out result);
            return result;
        }

        public Word Get(ref Word w)
        {
            Word result;
            words.TryGetValue(w, out result);
            w = result;
            return result;
        }

        public int Add(Word w, string letter = "")
        {
            if (w.rank == 0)
            {
                if (alphabet.TryGetValue(letter, out w.id))
                {
                    return w.id;
                }
                else
                {
                    w.id = NextId();
                    if (string.IsNullOrEmpty(letter))
                        throw new ArgumentException("Аргумент letter не может быть пустым для слова ранга 0!");
                    alphabet.Add(letter, w.id);
                    words.Add(w, w);
                    return w.id;
                }
            }
            else
            {
                Word exists;
                if (words.TryGetValue(w, out exists))
                {
                    return exists.id;
                }
                else
                {
                    w.id = NextId();
                    words.Add(w, w);
                    //Добавление родительского слова в каждое из дочерних слов w.childs
                    if (w.childs != null)
                        Array.ForEach(w.childs, c => Get(c).AddParent(w.id));
                    return w.id;
                }
            }
        }

        private int[] Parse(string text, int rank)
        {
            text = this.parsers[rank].Normilize(text);
            var strings = this.parsers[rank].Split(text).Where(s => !string.IsNullOrEmpty(s));
            //Для букв (слов ранга 0)
            
            if (rank == 0)
                return strings.Select(s => Add(new Word(0, 0, null, null), s)).ToArray();   //возвращаем id букв, добавляя новые в алфавит
            else
                return strings.Select(s => Add(new Word(0, rank, Parse(s, rank - 1), null))).ToArray(); //Для слов ранга > 0 добавляем слова, которых еще нет
        }

        /// <summary>
        /// Возвращает id слова, если существует слово с набором id дочерних слов childs, или 0, если такое слово не найдено
        /// </summary>
        /// <param name="childs">набор id дочерних слов</param>
        /// <param name="rank">ранг искомого слова</param>
        /// <returns></returns>
        //public int Find(int[] childs, int rank)
        //{
        //    Word word = new Word(0, rank, childs, null);
        //    Word result;
        //    words.TryGetValue(word, out result);
        //    if (result == null) return 0;
        //    return result.id;
        //}

        public Term ToTerm(string text, int rank)
        {
            text = parsers[rank].Normilize(text);
            if (rank == 0)
                return new Term(rank, alphabet[text], 1, text, null);
            else
                return new Term(rank, 0, 0, text,
                    parsers[rank - 1].Split(text).
                    Where(s => !string.IsNullOrWhiteSpace(s)).
                    Select(s => ToTerm(s, rank - 1)));
        }

        public Term ToTerm(Word w)
        {
            if (w == null) return null;
            if (w.rank == 0)
                return new Term(w.rank, w.id, _confidence: 1, _text: alphabet[w.id], _childs: null);
            else
                return new Term(w.rank, w.id, _confidence: 1, _text: "", _childs: w.childs.Select(c => ToTerm(Get(c))));
        }

        public Term ToTerm(Word w, float _confidence)
        {
            if (w.rank == 0)
                return new Term(w.rank, w.id, _confidence, alphabet[w.id], null);
            else
                return new Term(w.rank, w.id, _confidence, "", w.childs.Select(c => ToTerm(Get(c))));
        }

        public Term ToTerm(int i)
        {
            return ToTerm(Get(i));
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
            var candidates = term.childs.               // дочерние термы
                Select(c => Evaluate(c)).               // вычисляем все значения confidence
                Where(c => c.id != 0).                  // отбрасываем термы с id=0, т.к. они не идентифицированы
                SelectMany(c => Get(c.id).parents?.Select(i => Get(i))). // получаем список родительских слов
                Distinct().                             // без дублей
                Select(p => ToTerm(p)).                 // переводим слова в термы
                ToList();
            if (count == 0) count = candidates.Count;
            //Расчет оценок Confidence для каждого из соседей
            candidates.AsParallel().ForAll(p => p.confidence = Confidence.Compare(term, p));
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
                term.id = alphabet[term.text];
                term.confidence = (term.id == 0 ? 0 : 1);
                return term;
            }
            else
            {
                //Если ранг терма больше нуля, то confidence считаем по набору дочерних элементов
                //Выделение претендентов на роль ближайшего
                var candidates = term.childs.            // дочерние термы
                    Select(c => Evaluate(c)).
                    Where(c => c.id != 0).              // отбрасываем термы с id=0, т.к. они не идентифицированы
                    Select(c => Get(c.id)).             // переводим термы в слова
                    SelectMany(w => w.parents?.Select(i => Get(i))). // получаем список родительских слов
                    Distinct().                         // без дублей
                    Select(p => ToTerm(p)).             // переводим слова в термы
                    ToList();
                //Поиск ближайшего родителя, т.е. родителя с максимумом сonfidence
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

        private int NextId()
        {
            id_counter++;
            return id_counter;
        }

    }
}
