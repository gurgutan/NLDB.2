import timeit
import numpy as np
import scipy.sparse as sparse
import sqlite3 as sqlite


#/Data/Result/5mbclone.db')


def calc_mean(db_src_path, rank):
    """Функция расчета матрицы матожиданий"""
    if(rank<0):
        print("Параметр rank не может быть меньше 0")
        return
    db = sqlite.connect(db_src_path)
    #Создаем таблицу для матрицы матожиданий
    db.execute('DROP TABLE IF EXISTS MatrixM')
    db.execute(
        '''CREATE TABLE IF NOT EXISTS MatrixM (
            [Row]    INTEGER NOT NULL,
            [Column] INTEGER NOT NULL,
            Mean     NOT NULL,
            Rank     INTEGER NOT NULL,
            PRIMARY KEY ([Row], [Column])
            );''')
    rows_src = [x for x in db.execute(
        'select id,rank,symbol,childs from Words where Rank=?;', (int(rank + 1),))]
    if(len(rows_src)==0):
        print('Слов в БД не найдено')
        return
    rows = [np.frombuffer(r[3], dtype=np.int32) for r in rows_src]
    #размер квадратной матрицы матожиданий
    n = max([r.max() for r in rows])+1
    #создаем вспомогательные матрицы для вычислений
    mSum = sparse.lil_matrix((n, n), dtype=np.float32)
    mCount = sparse.lil_matrix((n, n), dtype=np.float32)
    rows_count = len(rows)
    print('Вычисление сумм')
    timestart = timeit.default_timer()
    for e,x in enumerate(rows):
        if(e%771==0):
            print(e, 'из', rows_count)
        for i in x:
            for j in x:
                mSum[i, j] += j-i
                mCount[i, j] += 1
    print("Время: ", timeit.default_timer()-timestart)    
    print('Вычисление среднего')
    timestart = timeit.default_timer()
    #Вычисление среднего
    means = mSum.tocsr().multiply(mCount.tocsr().power(-1, dtype=np.float32))
    print("Время: ", timeit.default_timer()-timestart)
    
    #запись в БД
    print("Запись в БД")
    timestart = timeit.default_timer()
    means_coo = sparse.find(means.tocoo())
    for i in range(len(means_coo)):
        row=means_coo[0][i]
        col=means_coo[1][i]
        val=means_coo[2][i]
        values = (row, col, val, rank-1)
        db.execute(
            'INSERT OR REPLACE INTO MatrixM([Row], [Column], Mean, Rank) VALUES(?,?,?,?);',
            values)
    print("Время: ", timeit.default_timer()-timestart)
    timestart = timeit.default_timer()
    print('Комит БД')
    db.commit()
    print("Время: ", timeit.default_timer()-timestart)
    print('Завершено')
    
