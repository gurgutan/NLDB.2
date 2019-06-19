using System;
using System.Collections.Generic;
using System.Linq;

namespace NLDB
{
    public class Node
    {
        public int id;

        private readonly Dictionary<int, Node> followers = new Dictionary<int, Node>();
        public Dictionary<int, Node> Followers
        {
            get
            {
                return followers;
            }
        }

        public Node(int _id)
        {
            id = _id;
        }

        public override string ToString()
        {
            if (followers.Count == 0)
                return id.ToString();
            else if (followers.Count == 1)
                return id.ToString() + "->" + followers.Values.First().ToString();
            else
            {
                var f = followers.Values.ToList();
                string ending = "";
                if (f.Count>4)
                {
                    f = f.Take(4).ToList();
                    ending = "...";
                }
                string followers_str = f.Aggregate("", (c, n) => c + (c != "" ? "|" : "") + n.ToString());
                return id.ToString() + "->" + "(" + followers_str + ending + ")";
            }
        }

    }

    public class Grammar
    {
        private Node root = new Node(0);
        public Node Root
        {
            get
            {
                return root;
            }
        }
        private List<Node> _path;
        private readonly int _max_search_depth; //глубина поиска при возвратном поиске узла

        public Grammar(int max_search_depth = 2)
        {
            _max_search_depth = max_search_depth;
        }

        public override string ToString()
        {
            return root.ToString();
        }

        public List<Node> Find(int[] word)
        {
            Node _cur_node = root;
            _path = new List<Node>
            {
                root
            };
            for (int i = 0; i < word.Length; i++)
            {
                int _id = word[i];
                if (root.Followers.TryGetValue(_id, out Node _next))
                {
                    _path.Add(_next);
                    _cur_node = _next;
                    continue;
                }
                else
                    return null;
            }
            return _path;
        }

        /// <summary>
        /// Добавляет слово word в грамматику, если его еще нет
        /// </summary>
        /// <param name="word"></param>
        public void Add(int[] word)
        {
            Node _cur_node = root;
            _path = new List<Node>
            {
                root
            };
            for (int i = 0; i < word.Length; i++)
            {
                int _id = word[i];
                //Если следующий узел определен, двигаемся в него и продолжаем обработку слова
                if (_cur_node.Followers.TryGetValue(_id, out Node _next))
                {
                    _path.Add(_next);
                    _cur_node = _next;
                    continue;
                }
                else
                {
                    int _depth = 0;
                    //Цикл по глубине поиска
                    while (_depth < _max_search_depth && _depth < _path.Count - 1)
                    {
                        //поиск начинается с глубины 1
                        //глубина отсчитывается в обратную сторону от текущего положения в цепочке
                        //т.е. для цепочки _path[0]->_path[1]->...->_path[i-1]->_path[i] , поиск начинается 
                        //с узла _path[i-1], и продолжается до _path[i-_max_depth]
                        _depth++;
                        Node _search_root = _path[(_path.Count - 1) - _depth];
                        _next = FindNode(_search_root, _depth + 1, _id);
                        if (_next == null) // не нашли
                        {
                            continue;
                        }
                        else
                        {
                            _cur_node.Followers.Add(_id, _next);    //Добавим пермычку между _cur_node и _next
                            _path.Add(_next);   // добавим очередной пройденный узел в путь
                            _cur_node = _next;  // установим текущим узлом _next
                            break;
                        }
                    }
                    //поиск по всей допустимой глубине окончен, но результата так и нет, тогда создаем новый узел
                    if (_next == null)
                    {
                        _next = new Node(_id);
                        _cur_node.Followers.Add(_id, _next);
                        _path.Add(_next);
                        _cur_node = _next;
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
        /// <param name="i"></param>
        /// <returns></returns>
        private Node FindNode(Node search_root, int depth, int i)
        {
            if (depth < 0)
                throw new ArgumentOutOfRangeException("Аргумент depth не должен быть отрицательным");
            else if (depth == 0)
            {
                if (search_root.id == i)
                    return search_root;
                else
                    return null;
            }
            else if (depth == 1)
            {
                if (search_root.Followers.TryGetValue(i, out Node _next))
                    return _next;
                else
                    return null;
            }
            else
                foreach (var _node in search_root.Followers.Values)
                {
                    Node _next = FindNode(_node, depth - 1, i);
                    if (_next != null) return _next;
                }
            return null;
        }
    }
}
