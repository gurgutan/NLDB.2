import sqlite3 as sqlite
import numpy as np
import os.path
import rlcompleter
from tqdm import tqdm


class Tokenizer(object):
    """Класс с методами для преобразования дерева строк в дерево токенов"""

    def __init__(self, dbpath=''):
        self.word_id = 65535    # начальный номер токена
        self.word_min_len = 3   # минимальная длина слова
        if dbpath == '':
            self.db = sqlite.connect(':memory:')
        else:
            if os.path.exists(dbpath):
                self.db = sqlite.connect(dbpath)
                c = self.db.cursor()
                c.execute('select max(id) from words')
                maxid = c.fetchone()
                if maxid is not None:
                    self.word_id = maxid[0]
            else:
                self.db = sqlite.connect(dbpath)
                self.db.execute(
                    '''
                    create table words(
                        id integer primary key,
                        childs BLOB not null,
                        rank integer not null)
                    ''')
                self.db.execute('create unique index childs on words(childs)')
                self.db.commit()

    def __del__(self):
        self.db.close()

    def tokenize(self, text_tree, rank):
        self._max_rank = rank
        tokens = self._tokenize(text_tree, rank)
        self.db.commit()
        return [self._get_word(t) for t in tokens if not t is None]

    def _get_token(self, childs, rank):
        a = np.array(childs, dtype=np.int32)
        cur = self.db.cursor()
        cur.execute(
            'select id from words where childs=?', (a.tobytes(),))
        result = cur.fetchone()
        if result is None:
            self.word_id += 1
            self.db.execute(
                '''
                insert into words(id, childs, rank) values(?,?,?)
                ''',
                (self.word_id, a.tobytes(), rank))
            token = self.word_id
        else:
            token = result[0]
        return token

    def _get_word(self, token):
        cur = self.db.cursor()
        cur.execute('select id, childs, rank from words where id=?', (token,))
        result = cur.fetchone()
        if result is None:
            return None
        else:
            id = result[0]
            rank = result[2]
            np_array = np.frombuffer(result[1], dtype=np.int32)
            if rank == 1:
                return (id, np_array.tolist(), 1)
            else:
                childs = [self._get_word(c) for c in np_array.tolist()]
            return (id, childs, rank)

    def _tokenize(self, text_tree, rank):
        if rank == 0:
            if(len(text_tree) < self.word_min_len):
                return None
            else:
                return [ord(c) for c in text_tree]
        tokens = []
        if rank == self._max_rank:
            progress = tqdm(total=len(text_tree), ncols=120, mininterval=0.5)
        for t in text_tree:
            childs = self._tokenize(t, rank-1)
            if childs is None:
                if rank == self._max_rank:
                    progress.update(1)
                continue
            if len(childs) == 0:
                if rank == self._max_rank:
                    progress.update(1)
                continue
            id = self._get_token(childs, rank)
            tokens.append(id)
            if rank == self._max_rank:
                progress.update(1)
        
        if rank == self._max_rank:
            progress.close()
        return tokens
