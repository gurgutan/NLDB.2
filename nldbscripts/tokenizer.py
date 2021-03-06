# #############################################################################
# В рамках проекта NLDB. Слеповичев И.И. 26.07.2019.
# -----------------------------------------------------------------------------
# Векторизация слов
# #############################################################################

import sqlite3 as sqlite
import numpy as np
import os.path
import rlcompleter
from tqdm import tqdm
import alphabet


class Vectorizer(object):
    '''
    Класс с методами для преобразования дерева строк в дерево идентификаторов.
    Ключевое отличие от обычных векторизаторов в том что на входе и на выходе 
    основного метода - дерево, а не вектор.
    '''

    def __init__(self, dbpath='', max_rank=1, word_min_len=2):
        self._max_rank = max_rank
        self._letters = alphabet.Alphabet()
        self._current_id = self._letters.size+1    # текущий id слова
        # минимальная длина слова, всё что меньше - пропускается
        self.word_min_len = word_min_len
        if dbpath == '':
            self._create_db(':memory:')
        else:
            if os.path.exists(dbpath):
                self.db = sqlite.connect(dbpath)
                c = self.db.cursor()
                c.execute('select max(id) from words')
                maxid = c.fetchone()
                if maxid is not None:
                    self.current_id = maxid[0]
            else:
                self._create_db(dbpath)

    def vectorize(self, text_tree):
        '''
        Метод получает дерево букв и возвращает соответствующее дерево идентификаторов.
        Результат также записывается в БД dbpath.
        '''
        with tqdm(total=len(text_tree), ncols=120, mininterval=0.5) as self._progress:
            ids = self.transform(text_tree, self._max_rank)

        self.db.commit()
        return ids  # [self.get_word(t) for t in ids if not t is None]

    def transform(self, text_tree, rank):
        if rank == 0:
            return None if len(text_tree) < self.word_min_len else [self._letters.get_int(c) for c in text_tree]
        ids = []
        for t in text_tree:
            if(rank == self._max_rank):
                self._progress.update(1)
            # векторное представление подслова t
            childs = self.transform(t, rank-1)
            if (not childs is None) and (len(childs) > 0):
                ids.append(self.get_id(childs, rank))
        return ids

    def get_id(self, childs, rank):
        a = np.array(childs, dtype=np.int32).tobytes()
        cur = self.db.cursor()
        cur.execute('select id from words where childs=?', (a,))
        result = cur.fetchone()
        # Если слово, состоящее из подслов childs не найдено - добавим его в БД
        if result is None:
            self._current_id += 1
            self.db.execute(
                'insert into words(id, childs, rank) values(?,?,?)',
                (self._current_id, a, rank))
            return self._current_id
        else:
            return result[0]

    def get_word(self, id):
        cur = self.db.cursor()
        cur.execute('select childs, rank from words where id=?', (id,))
        # В result запишем кортеж (childs, rank)
        result = cur.fetchone()
        if result is None:
            return None
        else:
            # В БД поле childs хранит данные о дочерних словах в формате массива numpy
            a = np.frombuffer(result[0], dtype=np.int32)
            return (a.tolist(), 1) if result[1] == 1 else ([self.get_word(c) for c in a.tolist()], result[1])

    def _create_db(self, dbpath):
        create_table = 'create table words(id integer primary key,childs BLOB not null,rank integer not null)'
        create_index = 'create unique index childs on words(childs)'
        self.db = sqlite.connect(dbpath)
        self.db.execute(create_table)
        self.db.execute(create_index)
        self.db.commit()

    def __del__(self):
        self.db.close()


# проверка векторизации
MODE_TEST = False

if(MODE_TEST):
    tree = [["пример", "текста", "для", "проверки"],
            ["другой", "пример", "текста"]]
    v = Vectorizer(max_rank=2)
    print(v.vectorize(tree))
