import sqlite3 as sqlite
import numpy as np



class Tokenizer(object):
    """Класс с методами для преобразования дерева строк в дерево токенов"""

    def __init__(self):
        self.id = 65535
        self.db = sqlite.connect(':memory:')
        self.db.execute(
            'create table words(id integer primary key, childs BLOB not null, rank integer not null)')
        self.db.execute('create unique index childs on words(childs)')
        self.db.commit()

    def __del__(self):
        self.db.close()

    def get_token(self, childs, rank):
        a = np.array(childs, dtype=np.int32)
        cur = self.db.cursor()
        cur.execute(
            'select id from words where childs=?', (a.tobytes(),))
        result = cur.fetchone()
        if result == None:
            self.id += 1
            self.db.execute('insert into words(id, childs, rank) values(?,?,?)',
                            (self.id, a.tobytes(), rank))
            self.db.commit()
            token=self.id
        else:
            token=result[0]
        return token
    
    def get_word(self, token):
        cur=self.db.cursor()
        cur.execute('select id, childs, rank from words where id=?',(token,))
        result = cur.fetchone()
        if result==None:
            return None
        else:
            id=result[0]
            rank=result[2]
            np_array=np.frombuffer(result[1],dtype=np.int32)
            if rank==1:
                return (id, np_array.tolist(), 1)
            else:
                childs=[self.get_word(c) for c in np_array.tolist()]
            return (id, childs, rank)

    def tokenize(self, text_tree, rank):
        if rank == 0:
            return [ord(c) for c in text_tree]
        tokens=[]
        for t in text_tree:
            childs=self.tokenize(t, rank-1)
            if len(childs) == 0:
                continue
            id=self.get_token(childs, rank)
            tokens.append(id)
        return tokens
    
    def get_tokens_tree(self, text_tree, rank):
        tokens=self.tokenize(text_tree, rank)
        result=[self.get_word(t) for t in tokens]
        return result
    
    
