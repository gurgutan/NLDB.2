import sqlite3 as sqlite
import numpy as np



class Tokenizer(object):
    """Класс с методами для преобразования дерева строк в дерево токенов"""

    def __init__(self):
        self.id = 65535
        self.db = sqlite.connect(':memory:')
        self.db.execute(
            'create table words(id INTEGER PRIMARY KEY, childs BLOB not null)')
        self.db.execute('create unique index childs on words(childs)')
        self.db.commit()

    def __del__(self):
        self.db.close()

    def get_token(self, childs):
        a = np.array(childs, dtype=np.int32)
        cur = self.db.cursor()
        cur.execute(
            'select id from words where childs=?', (a.tobytes(),))
        result = cur.fetchone()
        if result == None:
            self.id += 1
            self.db.execute('insert into words(id, childs) values(?,?)',
                            (self.id, a.tobytes()))
            self.db.commit()
            token=self.id
        else:
            token=result[0]
        return token

    def tokenize(self, text_tree, rank):
        if rank == 0:
            return [ord(c) for c in text_tree]
        tokens=[]
        for t in text_tree:
            childs=self.tokenize(t, rank-1)
            if len(childs) == 0:
                continue
            id=self.get_token(childs)
            if id == None:
                id=self.get_token(childs)
            tokens.append(id)
        return tokens
