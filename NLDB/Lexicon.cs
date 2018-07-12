using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class Lexicon
    {
        private int max_attempts = 1024; // максимальное количество попыток выделить новый номер
        private int count = 0;
        public int Count { get { return count; } }

        private int capacity;
        public int Capacity { get { return capacity; } }

        Word[] words;

        public Lexicon(int _capacity)
        {
            capacity = Primes.NextPrime(_capacity);
            words = new Word[capacity];
        }

        public Word this[int i]
        {
            get
            {
                return words[i];
            }
            set
            {
                words[i] = value;
            }
        }

        /// <summary>
        /// Возвращает индекс слова w, если он есть в словаре и 0, если его нет в словаре
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        public int this[Word w]
        {
            get
            {
                int i = CalcIndex(w);
                return words[i].id > 0 ? i : 0;
            }
        }

        public bool ContainsIndex(int i)
        {
            return (words[i].id != 0);
        }

        public bool Contains(Word w)
        {
            int i = CalcIndex(w);
            return ContainsIndex(i);
        }

        public int Add(Word w)
        {
            if (count >= capacity - 1)
                throw new ArgumentOutOfRangeException("Словарь полон. Для добавления слова размер словаря должен быть увеличен.");
            if (w.rank == 0)
                return Register(w, RandomFreeIndex());
            w.id = CalcIndex(w);
            if (!ContainsIndex(w.id))
                return Register(w, w.id);
            if (RecalculateMinors(ref w))
                return Register(w, w.id);
            throw new ArgumentOutOfRangeException($"Неуспешная попытка добавления слова {w.ToString()}");
        }

        private bool RecalculateMinors(ref Word w)
        {
            int attempt = 0;
            //получаем всех потомков ранга 0
            var minors = GetMinors(w);
            int i = NextFreeIndex(w.id);
            Register(w,i);
            while (ContainsIndex(i) && attempt++ < max_attempts)
            {
                var parents = minors.SelectMany(c => this[c].parents);

            }
            throw new NotImplementedException();
        }

        //private bool TryRegister(Word w, int prev, int next)
        //{
        //    //Проверим, что next не занят
        //    if (ContainsIndex(next)) return false;
        //    //Перемещаем в словаре слово w с индекса prev, на next
        //    Move(w, prev, next);
        //    //Если слово w НЕ входит в другие слова, то номер next можно использовать - выходим с успехом. При этом словарь остается измененным.
        //    if (words[next].parents == null || words[next].parents.Count == 0) return true;
        //    //Если слово входит в другие слова, то готовим стек для записи всех изменений
        //    Stack<Tuple<int, int, int>> changes = new Stack<Tuple<int, int, int>>();  //Первый элемент пары - "что меняем", второй - "на что меняем"
        //    //Для всех слов, содержащих prev пытаемся пересчитать id и переместить их в новое место (в индекс id)
        //    foreach (var parent in words[next].parents)
        //    {
        //        //Запишем в стек, что будем менять поменяли
        //        changes.Push(new Tuple<int, int, int>(parent, prev, next));
        //        int parentId = UpdateId(parent, prev, next);
        //        //Если замена не получилась по причине коллизии, то откатываем все в зад
        //        if (!TryRegister(words[parent], parent, parentId))
        //        {
        //            //все изменения для вхождений prev в другие слова откатываем из стека
        //            while (changes.Count > 0)
        //            {
        //                var triplet = changes.Pop();
        //                UpdateId(triplet.Item1, triplet.Item2, triplet.Item2);
        //                ChangeIndex(words[pair.Item2], pair.Item1);
        //            }
        //            //возвращаем на место индексы-аргументы для регистрируемого слова
        //            ChangeIndex(w, prev);
        //            return false;
        //        }
        //    }
        //    return true;
        //}

        private int UpdateId(int parent, int prev, int next)
        {
            //обновляем в parent id дочернего элемента (замена всех prev на next всех вхождений w)
            words[parent].childs = words[parent].childs.Select(c => c == prev ? next : c).ToArray();
            //вычисляем новый индекс
            words[parent].id = CalcIndex(words[parent]);
            return words[parent].id;
        }

        private void Move(int prev, int next)
        {
            if (prev != 0)
                words[prev].id = 0;     //освобождаем индекс w.id
            words[next] = words[prev];  //копируем w в место по новому индексу i
            words[next].id = next;      //меняем индекс слова w
        }

        private int Register(Word w, int i)
        {
            count++;
            w.id = i;
            words[w.id] = w;
            return w.id;
        }

        private void Remove(int i)
        {
            if (i == 0) return;
            count--;
            words[i].id = 0;
        }

        //private void Swap(int i, int j)
        //{
        //    Word temp = words[j];
        //    words[j] = words[i];
        //    words[j].id = j;
        //    words[i] = temp;
        //    words[i].id = i;
        //}

        private int CalcIndex(Word w)
        {
            return w.GetHashCode() % capacity;
        }

        private int NextFreeIndex(int start)
        {
            int i = start;
            //всего попыток выбрать свободный индекс будет  не более capacity
            while ((++i) % capacity != start)
                if (!ContainsIndex(i)) return i;
            return 0;
        }

        private int RandomIndex()
        {
            Random rand = new Random((int)DateTime.Now.Ticks);
            return rand.Next(capacity);
        }

        private int RandomFreeIndex()
        {
            int i = RandomIndex();
            return NextFreeIndex(i);
        }

        private IEnumerable<int> GetMinors(Word w)
        {
            if (w.rank == 0) return new int[] { w.id };
            return w.childs.SelectMany(c => GetMinors(words[c]));
        }
    }
}
