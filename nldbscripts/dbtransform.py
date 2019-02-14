import numpy as np
import sqlite3 as sqlite


def download_words(db_src_path, db_dst_path):
    db_src = sqlite.connect(db_src_path)
    db_dst = sqlite.connect(db_dst_path)
    db_dst.execute(
        'CREATE TABLE IF NOT EXISTS Words (Id INTEGER PRIMARY KEY, Rank INTEGER NOT NULL, Symbol TEXT, Childs BLOB)')
    db_dst.execute(
        'CREATE TABLE IF NOT EXISTS Parents (WordId INTEGER NOT NULL, ParentId INTEGER NOT NULL)')
    # Очищаем таблицу слов
    db_dst.execute('DELETE FROM Words')
    db_dst.execute('DELETE FROM Parents')
    db_dst.commit()
    # Чтение слов из исходной БД
    rows = []
    for r in db_src.execute('select * from Words;'):
        if not (r[3] and r[3].strip()):
            rows.append((r[0], r[1], r[2], np.array([])))
        else:
            # Извлечем дочерние элементы
            a = np.fromstring(r[3], dtype=np.int32, count=-1, sep=',')
            rows.append((r[0], r[1], r[2], a))
    # Вставка слов в новую БД
    for row in rows:
        values = (row[0], row[1], row[2], row[3].tobytes())
        db_dst.execute(
            'INSERT INTO Words(Id, Rank, Symbol, Childs) VALUES(?,?,?,?);',
            values)
    db_dst.commit()
    for r in db_src.execute('select WordId, ParentId from Parents'):
        db_dst.execute(
                'insert into Parents(WordId, ParentId) values(?,?);',
                (r[0], r[1]))
    db_dst.commit()
    db_dst.close()
    db_src.close()

def download_splitters(db_src_path, db_dst_path):
    db_src = sqlite.connect(db_src_path)
    db_dst = sqlite.connect(db_dst_path)
    db_dst.execute('CREATE TABLE IF NOT EXISTS Splitters (Rank, Expression, PRIMARY KEY(Rank, Expression))')
    db_dst.execute('delete from Splitters')
    for r in db_src.execute('select Rank, Expression from Splitters'):
        db_dst.execute(
                'insert into Splitters(Rank, Expression) values(?,?)',
                (r[0], r[1]))
    db_dst.commit()
    db_src.close()
    db_dst.close()

def test_words(dbpath):
    db = sqlite.connect(dbpath)
    # Проверим первые 100 записей таблицы Words
    rows = [(r[0], r[1], r[2], np.frombuffer(r[3], dtype=np.int32))
            for r in db.execute('select * from Words;')]
    for x in rows[0:100]:
        print(x)

