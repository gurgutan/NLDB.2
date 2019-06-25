using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace NLDB
{
    //TODO: Включить Grammar в состав класса DataBase. Реализовать сохранение/загрузку грамматики

    /// <summary>
    /// Элемент (узел) грамматики. Содержит идентификатор слова и ссылки на следующие узлы
    /// </summary>
    public class Node
    {
        public int id;         // уникальный идентификатор элемента 
        public int word_id;     // идент-р слова, соответствующего данному элементу грамматики

        private readonly Dictionary<int, Node> followers = new Dictionary<int, Node>();
        public Dictionary<int, Node> Followers
        {
            get
            {
                return followers;
            }
        }

        public Node(int _id, int _word_id = -1)
        {
            id = _id;
            if (_word_id == -1) word_id = id;
            else word_id = _word_id;
        }

        public override string ToString()
        {
            if (followers.Count == 0)
                return word_id.ToString();
            else if (followers.Count == 1)
                return word_id.ToString() + "->" + followers.Values.First().ToString();
            else
            {
                var f = followers.Values.ToList();
                string ending = "";
                if (f.Count > 4)
                {
                    f = f.Take(4).ToList();
                    ending = "...";
                }
                string followers_str = f.Aggregate("", (c, n) => c + (c != "" ? "|" : "") + n.ToString());
                return word_id.ToString() + "->" + "(" + followers_str + ending + ")";
            }
        }

    }

    /// <summary>
    /// Класс описывающий сущность Грамматика. Грамматика представляет собой ациклический ориентированный граф
    /// с источником в вершине Root. Стоков может быть много. Каждый путь в грамматике описывает некоторое слово.
    /// Идентификаторы элементов пути соответствуют символам слова.
    /// </summary>
    public class Grammar
    {
        private Node root = new Node(0, 0); // корневой узел имеет идент-р 0 и соответствует слову с id=0
        /// <summary>
        /// Корневоей элемент грамматики. Имеет идентификатор 0
        /// </summary>
        public Node Root
        {
            get
            {
                return root;
            }
        }

        private List<Node> _path;
        private readonly int _max_search_depth; //глубина поиска при возвратном поиске узла
        private int count = 1;  // изначально в грамматике только источник root с id = 0
        /// <summary>
        /// Число элементов в грамматике
        /// </summary>
        public int Count
        {
            get { return count; }
        }

        private int links_count = 0;
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

        public override string ToString()
        {
            return root.ToString();
        }

        /// <summary>
        /// Поиск слова, заданного массивом идентификаторов в грамматике. Поиск считается успешным, 
        /// если найден путь в грамматике длины word.Length+1, в котором первый эл-т - Root, 
        /// а остальные эл-ты имеют идент-ры w[i]
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public List<Node> FindWord(int[] word)
        {
            Node _cur_node = root;
            _path = new List<Node>
            {
                root
            };
            for (int i = 0; i < word.Length; i++)
            {
                int _word_id = word[i];
                if (_cur_node.Followers.TryGetValue(_word_id, out Node _next))
                {
                    _path.Add(_next);
                    _cur_node = _next;
                    continue;
                }
                else
                    return null;
            }
            return new List<Node>(_path);
        }

        /// <summary>
        /// Найти элемент грамматики с идент-м id на глубине depth. Т.о. путь до эл-та должен быть длины depth+1,
        /// включая начальный эл-т Root
        /// </summary>
        /// <param name="_word_id"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public Node FindNode(int _word_id, int depth = 64)
        {
            return FindNode(root, depth, _word_id);
        }

        /// <summary>
        /// Частный метод, поиска элемента грамматики на глубине depth.использующий ссылку на ко
        /// </summary>
        /// <param name="search_root"></param>
        /// <param name="depth"></param>
        /// <param name="_word_id"></param>
        /// <returns></returns>
        private Node FindNode(Node search_root, int depth, int _word_id)
        {
            if (depth < 0)
                throw new ArgumentOutOfRangeException("Аргумент depth не должен быть отрицательным");
            else if (depth == 0)
            {
                if (search_root.word_id == _word_id)
                    return search_root;
                else
                    return null;
            }
            else
            {
                if (search_root.Followers.TryGetValue(_word_id, out Node _next))
                    return _next;
                else
                    foreach (var _node in search_root.Followers.Values)
                    {
                        _next = FindNode(_node, depth - 1, _word_id);
                        if (_next != null) return _next;
                    }
            }
            return null;
        }


        /// <summary>
        /// Добавляет слово word в грамматику, если его еще нет
        /// </summary>
        /// <param name="word"></param>
        public void Add(int[] word)
        {
            Node _cur_node = root;
            //
            _path = new List<Node>
            {
                root
            };
            //Ищем путь из источника Root, совпадающий (по id в вершинах) со словом word
            for (int i = 0; i < word.Length; i++)
            {
                int _word_id = word[i];  //для удобства
                //Если следующий узел определен, двигаемся в него и продолжаем обработку слова со следующего символа
                if (_cur_node.Followers.TryGetValue(_word_id, out Node _next))
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
                    //2. если 1 выполнить невозможно, создать новый узел с id=w[i] на расстоянии i от Root 
                    //   и связать (i-1)-й эл-т с созданным i-м.
                    int _depth = 1;
                    //Проверяем 1-й вариант. Для этого нужно попытаться найти путь длины i, ведущий в эл-т id=w[i].
                    //Так как перебор всех путей из Root длины i имеет сложность n, где n - число путей длины i и сложность растет
                    //как степень длины пути, разумно перебор начинать не от Root, а от i-1 
                    //  поиск всех путей с префиксом Root->w[0]->w[1]->...->w[i-1]->?, 
                    //  если не нашли, искать все пути с префиксом Root->w[0]->w[1]->...->w[i-2]->?
                    //и т.д. Т.е. углублять поиск в обратном направлении от текущей вершины i-1.
                    //При этом _max_search_depth выступает ограничителем глубины обратного поиска.
                    //Цикл по глубине поиска, начинаем с глубины _depth=1, и увеличиваем глубину, пока не найдем соответствие
                    while (_depth < _max_search_depth && _depth < _path.Count - 1)
                    {
                        //поиск начинается с глубины 1. глубина отсчитывается в обратную сторону от i-1
                        //т.е. для цепочки _path[0]->_path[1]->...->_path[i-1]->_path[i] , поиск начинается 
                        //с узла _path[i-1], и продолжается до _path[i-_max_depth]
                        Node _search_root = _path[(_path.Count - 1) - _depth];
                        _next = FindNodeOnDepth(_search_root, _depth + 1, _word_id);
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
                        _next = new Node(count + 1, _word_id);
                        _cur_node.Followers.Add(_word_id, _next);
                        _path.Add(_next);
                        _cur_node = _next;
                        links_count++;
                        count++;
                    }
                }
            }
            _path.Clear();
        }

        /// <summary>
        /// Поиск узла с идентификатором i строго на глубине depth от корня search_root
        /// </summary>
        /// <param name="search_root"></param>
        /// <param name="depth"></param>
        /// <param name="_word_id"></param>
        /// <returns></returns>
        private Node FindNodeOnDepth(Node search_root, int depth, int _word_id)
        {
            if (depth < 0)
                throw new ArgumentOutOfRangeException("Аргумент depth не должен быть отрицательным");
            else if (depth == 0)
            {
                if (search_root.word_id == _word_id)
                    return search_root;
                else
                    return null;
            }
            else if (depth == 1)
            {
                if (search_root.Followers.TryGetValue(_word_id, out Node _next))
                    return _next;
                else
                    return null;
            }
            else
                foreach (var _node in search_root.Followers.Values)
                {
                    Node _next = FindNodeOnDepth(_node, depth - 1, _word_id);
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
                root.Followers.Clear();
                GC.Collect();
                // Загрузим новую из БД
                (count, links_count) = GetNodeFromDB(_db, root);
                _db.Close();
            }
        }

        /// <summary>
        /// Считывает из БД _db элементы грамматики. Реализовано нерекурсивно для экономии на парсинге текста команды.
        /// </summary>
        /// <param name="_db">открытое соединение</param>
        /// <param name="root">корневой элемент, с которого начнется считывание</param>
        /// <returns></returns>
        private (int, int) GetNodeFromDB(SQLiteConnection _db, Node root)
        {
            string text = $"SELECT follower, word_id FROM Grammar WHERE id=@i";
            // Вспомогательный словарь для хранения всех элементов грамматики
            Dictionary<int, Node> _all_nodes = new Dictionary<int, Node>();
            int c = 1;  //число элементов
            int l = 0;  //число связей
            Queue<Node> queue = new Queue<Node>();
            queue.Enqueue(root);
            using (SQLiteCommand cmd = new SQLiteCommand(text, _db))
            {
                cmd.Parameters.Add("@i", System.Data.DbType.Int32);
                // Обход всего графа грамматики в БД организуем с помощью очереди queue
                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    cmd.Parameters["@i"].Value = node.id;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows) continue;
                        while (reader.Read())
                        {
                            int _f = reader.GetInt32(0);
                            int _f_id = reader.GetInt32(1);
                            // если элемент был загружен ранее
                            if (_all_nodes.TryGetValue(_f, out Node follower))
                                // добавляем связь с сущемтвующим элементов
                                root.Followers.Add(_f, follower);
                            else
                            {
                                // создаем и добавляем новый элемент и связь
                                follower = new Node(_f, _f_id);
                                _all_nodes[_f] = follower;
                                root.Followers.Add(_f_id, follower);
                                c++;    // элементы
                            }
                            // Добавляем в очередь очередной узел
                            queue.Enqueue(follower);
                            l++;    // связи
                        }
                    }
                }
            }
            return (c, l);
        }

        /// <summary>
        /// Сохраняет грамматику в БД 
        /// </summary>
        /// <param name="dbpath"></param>
        /// <returns></returns>
        public int SaveToDB(string dbpath)
        {
            var saved_links = 0;
            using (var db = new SQLiteConnection($"Data Source={dbpath}; Version=3;"))
            {
                db.Open();

                // Создаем таблицу
                var cmd = db.CreateCommand();
                cmd.CommandText =
                    "DROP TABLE IF EXISTS Grammar; " +
                    "CREATE TABLE Grammar(id INTEGER PRIMARY KEY, follower INTEGER NOT NULL, word_id INTEGER NOT NULL); " +
                    "CREATE INDEX IGrammar_followers ON Grammar(follower); " +
                    "CREATE INDEX IGrammar_word_id ON Grammar(word_id); ";
                cmd.ExecuteNonQuery();

                //Записываем данные
                var transaction = db.BeginTransaction();
                var nodes = GetAllNodesFrom(Root);
                saved_links = AddNodesToDB(db, nodes);
                transaction.Commit();
            }
            return saved_links;
        }

        /// <summary>
        /// Возвращает перечисление всех элементов грамматики, начиная с n и далее все, последующие
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private IEnumerable<Node> GetAllNodesFrom(Node n)
        {
            return new Node[1] { n }.
                Union(n.Followers.Values.
                Union(n.Followers.Values.
                SelectMany(f => GetAllNodesFrom(f))));
        }

        /// <summary>
        /// Записывает коллекцию узлов nodes в БД db
        /// </summary>
        /// <param name="db"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private int AddNodesToDB(SQLiteConnection db, IEnumerable<Node> nodes)
        {
            int saved_links = 0;
            string text = $"INSERT OR REPLACE INTO Grammar(id, follower, word_id) VALUES (@i, @f, @w);";
            using (SQLiteCommand cmd = new SQLiteCommand(text, db))
            {
                cmd.Parameters.Add("@i", System.Data.DbType.Int32);
                cmd.Parameters.Add("@f", System.Data.DbType.Int32);
                cmd.Parameters.Add("@w", System.Data.DbType.Int32);
                foreach (var n in nodes)
                {
                    cmd.Parameters["@i"].Value = n.id;
                    n.Followers.Values.ToList().ForEach(f =>
                    {
                        cmd.Parameters["@f"].Value = f.id;
                        cmd.Parameters["@w"].Value = f.word_id;
                        saved_links += cmd.ExecuteNonQuery();
                    });
                };
            }
            return saved_links;
        }

        //===================================================================================================
    }
}
