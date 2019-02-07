import numpy as np
import sqlite3 as sqlite

db5mb_conn=sqlite.connect('D:/data/result/5mb.db')
clone_conn=sqlite.connect('D:/data/result/clone5mb.db')
clone_conn.execute('CREATE TABLE IF NOT EXISTS Words (Id INTEGER PRIMARY KEY, Rank INTEGER NOT NULL, Symbol TEXT, Childs BLOB)')
clone_conn.execute('CREATE TABLE Splitters (Rank, Expression, PRIMARY KEY(Rank, Expression)')
clone_conn.execute('CREATE TABLE Parents (WordId INTEGER NOT NULL, ParentId INTEGER NOT NULL)')

clone_conn.execute('DELETE FROM Words')

rows=[]
for r in db5mb_conn.execute('select * from Words;'):
    if not (r[3] and r[3].strip()):
        rows.append((r[0], r[1], r[2], np.array([])))
    else:
        a = np.fromstring(r[3],dtype=np.int32,count=-1,sep=',')
        rows.append((r[0], r[1], r[2], a))

for row in rows:
    values=(row[0], row[1], row[2], row[3].tobytes())
    clone_conn.execute('INSERT INTO Words(Id, Rank, Symbol, Childs) VALUES(?,?,?,?);',values)
clone_conn.commit()

#Проверим первые 100 записей таблицы Words
rows=[(r[0], r[1], r[2], np.frombuffer(r[3],dtype=np.int32)) for r in clone_conn.execute('select * from Words;')]
for x in rows[0:100]:
    print(x)



