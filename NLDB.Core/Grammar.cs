using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using NLDB.Utils;

namespace NLDB
{

    /// <summary>
    /// Класс описывающий сущность Грамматика. Грамматика представляет собой ациклический ориентированный граф
    /// с источником в вершине Origin. Стоков может быть много. Каждый путь в грамматике описывает некоторое слово.
    /// Идентификаторы элементов пути соответствуют символам слова.
    /// </summary>
    public class Grammar
    {
        /// <summary>
        /// Корневоей элемент грамматики. Имеет идентификатор 0
        /// </summary>
        public GrammarNode Origin { get { return origin; } }

        /// <summary>
        /// Число элементов в грамматике
        /// </summary>
        public int NodesCount
        {
            get { return nodes_count; }
        }

        /// <summary>
        /// Число связей между элементами грамматики
        /// </summary>
        public int LinksCount
        {
            get { return links_count; }
        }

        public Grammar(int max_search_depth = 2)
        {
            _max_search_depth = max_search_depth;
        }

        public Grammar(IEnumerable<int[]> words)
        {
            _max_search_depth = -1; // означает построение грамматики без обратного поиска
            int count = 0;
            links_count = 0;
            nodes_count = 1;
            // добавляем все слова из полученного списка в грамматику
            using (ProgressInformer ibar = new ProgressInformer(prompt: $"Построение:", max: words.Count() - 1, measurment: $"слов", barSize: 64, fps: 10))
            {
                foreach (var w in words)
                {
                    if (w.Length > 0)
                    {
                        Add(w);
                        count++;
                        ibar.Set(count, false);
                    }
                }
                ibar.Set(count, true);
            }
        }

        public override string ToString()
        {
            return origin.ToString();
        }

        /// <summary>
        /// Поиск слова, заданного массивом идентификаторов в грамматике. Поиск считается успешным, 
        /// если найден путь в грамматике длины word.Length+1, в котором первый эл-т - Origin, 
        /// а остальные эл-ты имеют идент-ры word[i]
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public List<GrammarNode> FindWord(int[] word)
        {
            GrammarNode _cur_node = origin;
            _path = new List<GrammarNode>
            {
                origin
            };
            for (int i = 0; i < word.Length; i++)
            {
                int _word_id = word[i];
                if (_cur_node.Followers.TryGetValue(_word_id, out GrammarNode _next))
                {
                    _path.Add(_next);
                    _cur_node = _next;
                    continue;
                }
                else
                    return null;
            }
            return new List<GrammarNode>(_path);
        }

        /// <summary>
        /// Поиск элементов грамматики по идентификатору слова
        /// </summary>
        /// <param name="_word_id"></param>
        /// <returns></returns>
        public List<GrammarNode> FindNodesByWordId(int _word_id)
        {
            var nodes = new HashSet<GrammarNode>(); // выполняет роль меток проверенных узлов
            Queue<GrammarNode> queue = new Queue<GrammarNode>();    // очередь обхода графа в ширину
            List<GrammarNode> result = new List<GrammarNode>();
            queue.Enqueue(Origin);
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                if (nodes.Add(n))
                {
                    if (n.word_id == _word_id) result.Add(n);
                    n.Followers.Values.ToList().ForEach(f => queue.Enqueue(f));
                }
            }
            return result;
        }

        /// <summary>
        /// Добавляет слово word в грамматику. 
        /// Быстрый, но затратный по памяти метод. Использует вспомогательный словарь _node, требующий очистки перед началом создания грамматики.
        /// Требует инициализации перед началом построения грамматики.
        /// </summary>
        /// <param name="word"></param>
        public void Add(int[] word)
        {
            GrammarNode _cur_node = origin;
            //Ищем путь из источника Origin, совпадающий (по id в вершинах) со словом word
            for (int i = 0; i < word.Length; i++)
            {
                int _word_id = word[i];  //для удобства
                //Если следующий узел определен, двигаемся в него и продолжаем обработку слова со следующего символа
                if (_cur_node.Followers.TryGetValue(_word_id, out GrammarNode _next))
                {
                    _cur_node = _next;
                }
                else
                {
                    // Ключ в словаре получается конкатенацией битов двух числел: d - глубина текущего узла +1 (с учетом Origin), и _word_id.
                    // Т.о. соблюдается уникальность узла по совокупности двух значений: глубина и идентификатор слова
                    var _key = (i + 1, _word_id);
                    if (!_nodes.TryGetValue(_key, out _next))
                    {
                        _next = new GrammarNode(nodes_count + 1, _word_id);
                        // добавляем узел во вспомогательный словарь
                        _nodes.Add(_key, _next);
                        nodes_count++;
                    }
                    _cur_node.Followers.Add(_word_id, _next);    //Добавим связь _cur_node -> _next
                    _cur_node = _next;  // установим текущим узлом _next
                    links_count++;
                }
            }
        }

        /// <summary>
        /// Добавляет слово word в грамматику, если его еще нет
        /// </summary>
        /// <param name="word"></param>
        public void Add_lowmem(int[] word)
        {
            GrammarNode _cur_node = origin;
            _path = new List<GrammarNode>
            {
                origin
            };
            //Ищем путь из источника Origin, совпадающий (по id в вершинах) со словом word
            for (int i = 0; i < word.Length; i++)
            {
                int _word_id = word[i];  //для удобства
                //Если следующий узел определен, двигаемся в него и продолжаем обработку слова со следующего символа
                if (_cur_node.Followers.TryGetValue(_word_id, out GrammarNode _next))
                {
                    _path.Add(_next);
                    _cur_node = _next;
                    continue;
                }
                else
                {
                    //Если следующего эл-та пути (i-го), совпадающего по id в графе нет, то есть два варианта:
                    //1. если в графе уже есть узел c id=w[i], на расстоянии i, но этот узел в другом пути,
                    //   найдем этот элемент и свяжем его с текущим эл-том пути [(i-1)-м]
                    //2. если 1 выполнить невозможно, создать новый узел с id=w[i] на расстоянии i от Origin 
                    //   и связать (i-1)-й эл-т с созданным i-м.
                    int _depth = 1;
                    //Проверяем 1-й вариант. Для этого нужно попытаться найти путь длины i, ведущий в эл-т id=w[i].
                    //Так как перебор всех путей из Origin длины i имеет сложность n, где n - число путей длины i и сложность растет
                    //как степень длины пути, разумно перебор начинать не от Origin, а от i-1 
                    //  поиск всех путей с префиксом Origin->w[0]->w[1]->...->w[i-1]->?, 
                    //  если не нашли, искать все пути с префиксом Origin->w[0]->w[1]->...->w[i-2]->?
                    //и т.д. Т.е. углублять поиск в обратном направлении от текущей вершины i-1.
                    //При этом _max_search_depth выступает ограничителем глубины обратного поиска.
                    //Цикл по глубине поиска, начинаем с глубины _depth=1, и увеличиваем глубину, пока не найдем соответствие
                    while (_depth < _max_search_depth && _depth < _path.Count - 1)
                    {
                        //поиск начинается с глубины 1. глубина отсчитывается в обратную сторону от i-1
                        //т.е. для цепочки _path[0]->_path[1]->...->_path[i-1]->_path[i] , поиск начинается 
                        //с узла _path[i-1], и продолжается до _path[i-_max_depth]
                        GrammarNode _search_Origin = _path[(_path.Count - 1) - _depth];
                        _next = FindNodeOnDepth(_search_Origin, _depth + 1, _word_id);
                        if (_next == null) // не нашли
                        {
                            _depth++;
                            continue;
                        }
                        else
                        {
                            _cur_node.Followers.Add(_word_id, _next);    //Добавим пермычку между _cur_node и _next
                            _path.Add(_next);   // добавим очередной пройденный узел в путь
                            _cur_node = _next;  // установим текущим узлом _next
                            links_count++;
                            break;
                        }
                    }
                    //поиск по всей допустимой глубине окончен неуспехом, поэтому создаем новый узел
                    //и связываем его с i-1 узлом в пути
                    if (_next == null)
                    {
                        // создаем элемент с id=count+1, хотя подойдет любое уникальное число из диапазона int32
                        _next = new GrammarNode(nodes_count + 1, _word_id);
                        _cur_node.Followers.Add(_word_id, _next);
                        _path.Add(_next);
                        _cur_node = _next;
                        links_count++;
                        nodes_count++;
                    }
                }
            }
            _path.Clear();
        }

        /// <summary>
        /// Поиск узла с идентификатором i строго на глубине depth от корня search_Origin
        /// </summary>
        /// <param name="search_origin"></param>
        /// <param name="depth"></param>
        /// <param name="_word_id"></param>
        /// <returns></returns>
        private GrammarNode FindNodeOnDepth(GrammarNode search_origin, int depth, int _word_id)
        {
            if (depth < 0)
                throw new ArgumentOutOfRangeException("Аргумент depth не должен быть отрицательным");
            else if (depth == 0)
            {
                if (search_origin.word_id == _word_id)
                    return search_origin;
                else
                    return null;
            }
            else if (depth == 1)
            {
                if (search_origin.Followers.TryGetValue(_word_id, out GrammarNode _next))
                    return _next;
                else
                    return null;
            }
            else
                foreach (var _node in search_origin.Followers.Values)
                {
                    GrammarNode _next = FindNodeOnDepth(_node, depth - 1, _word_id);
                    if (_next != null) return _next;
                }
            return null;
        }

        public void LoadFromDB(string dbpath)
        {
            using (var _db = new SQLiteConnection($"Data Source={dbpath}; Version=3;"))
            {
                _db.Open();
                // Зачистим существующую грамматику
                origin = null;
                GC.Collect();
                // Загрузим новую из БД
                (nodes_count, links_count) = GetNodesFromDB(_db);
                _db.Close();
            }
        }

        /// <summary>
        /// Считывает из БД _db элементы грамматики. Реализовано нерекурсивно для экономии на парсинге текста команды.
        /// </summary>
        /// <param name="_db">открытое соединение</param>
        /// <returns></returns>
        private (int, int) GetNodesFromDB(SQLiteConnection _db)
        {
            string text = $"SELECT id, word_id, follower FROM Grammar;";
            // Вспомогательный словарь для хранения всех элементов грамматики
            Dictionary<int, GrammarNode> nodes = new Dictionary<int, GrammarNode>();
            Dictionary<int, List<int>> followers = new Dictionary<int, List<int>>();
            using (SQLiteCommand cmd = new SQLiteCommand(text, _db))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int _id = reader.GetInt32(0);
                    int _word_id = reader.GetInt32(1);
                    int _f_id = reader.GetInt32(2);
                    // создаем считанный элемент и пробуем добавить его
                    if (!nodes.ContainsKey(_id))
                    {
                        var n = new GrammarNode(_id, _word_id);
                        nodes.Add(_id, n);
                        // новый список связей
                        var f = new List<int>();
                        if (_f_id != -1) f.Add(_f_id);
                        followers.Add(_id, f);
                    }
                    else
                    {
                        followers[_id].Add(_f_id);
                    }
                }
                // после того как были созданы элементы, добавим связи
                links_count = 0;
                foreach (var f in followers)
                    foreach (var n in f.Value)
                    {
                        nodes[f.Key].Followers.Add(nodes[n].word_id, nodes[n]);
                        links_count++;
                    }
            }
            nodes_count = nodes.Count;
            origin = nodes[0];  // установим источник
            return (nodes_count, links_count);
        }

        /// <summary>
        /// Сохраняет грамматику в БД 
        /// </summary>
        /// <param name="dbpath"></param>
        /// <returns></returns>
        public int SaveToDB(string dbpath)
        {
            var saved_rows = 0;
            using (var db = new SQLiteConnection($"Data Source={dbpath}; Version=3;"))
            {
                db.Open();

                // Создаем таблицу
                var cmd = db.CreateCommand();
                cmd.CommandText =
                    "DROP TABLE IF EXISTS Grammar; " +
                    "CREATE TABLE Grammar(id INTEGER NOT NULL, word_id INTEGER NOT NULL, follower INTEGER NOT NULL, PRIMARY KEY(id, follower)); " +
                    //"CREATE INDEX IGrammar_followers ON Grammar(follower); " +
                    "CREATE INDEX IGrammar_word_id ON Grammar(word_id); ";
                var transaction = db.BeginTransaction();
                cmd.ExecuteNonQuery();
                transaction.Commit();

                //Записываем данные
                transaction = db.BeginTransaction();
                var nodes = GetAllNodes();
                saved_rows = AddNodesToDB(db, nodes);
                transaction.Commit();
            }
            return saved_rows;
        }

        private IList<GrammarNode> GetAllNodes()
        {
            var nodes = new HashSet<GrammarNode>();
            Queue<GrammarNode> queue = new Queue<GrammarNode>();
            queue.Enqueue(Origin);
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                if (nodes.Add(n))
                    n.Followers.Values.ToList().ForEach(f => queue.Enqueue(f));
            };
            return nodes.ToList();
        }

        /// <summary>
        /// Записывает коллекцию узлов nodes в БД db
        /// </summary>
        /// <param name="db"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private int AddNodesToDB(SQLiteConnection db, IEnumerable<GrammarNode> nodes)
        {
            int saved_rows = 0;
            int count = 0;
            string text = $"INSERT INTO Grammar(id, word_id, follower) VALUES (@i, @w, @f);";
            using (ProgressInformer ibar = new ProgressInformer(
                prompt: $"Сохранение:",
                max: nodes.Count() - 1,
                measurment: $"записей",
                barSize: 64, fps: 10))
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
            {
                cmd.Parameters.Add("@i", System.Data.DbType.Int32);
                cmd.Parameters.Add("@w", System.Data.DbType.Int32);
                cmd.Parameters.Add("@f", System.Data.DbType.Int32);
                foreach (var n in nodes)
                {
                    cmd.Parameters["@i"].Value = n.id;
                    cmd.Parameters["@w"].Value = n.word_id;
                    if (n.Followers.Count > 0)
                        n.Followers.Values.ToList().ForEach(f =>
                        {
                            cmd.Parameters["@f"].Value = f.id;
                            saved_rows += cmd.ExecuteNonQuery();
                        });
                    else
                    {
                        cmd.Parameters["@f"].Value = -1;
                        saved_rows += cmd.ExecuteNonQuery();
                    }
                    count++;
                    ibar.Set(count, false);
                };
                ibar.Set(count, true);  // выведем 100% в прогесс-баре
            }
            return saved_rows;
        }

        //--------------------------------------------------------------------------------------------------------
        // Частные свойства
        private GrammarNode origin = new GrammarNode(0, 0);
        private int nodes_count = 1;  // изначально в грамматике только источник Origin с id = 0
        private int links_count = 0;
        private List<GrammarNode> _path;
        private readonly int _max_search_depth; //глубина поиска при возвратном поиске узла

        private Dictionary<(int, int), GrammarNode> _nodes = new Dictionary<(int, int), GrammarNode>(); // вспомогательный для построения грамматики

    }
}
