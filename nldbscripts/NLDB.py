import sqlite3 as sqlite
import numpy as np
import scipy.sparse as sparse
from sklearn.metrics.pairwise import cosine_similarity
import timeit

#from scipy.spatial import distance


class Calculations(object):
    """description of class"""

    dbpath = ""
    
    def __init__(self, dbpath):
        self.dbpath = dbpath
        try:
            self.db = sqlite.connect(self.dbpath)
        except:
            print('Не удалось подключиться к БД ', self.dbpath)
            
    def __del__(self):
        try:
            self.db.close()
        except:
            print('Не удалось закрыть соединение с БД ', self.dbpath)

    def dbget_words(self, rank):
        query = self.db.execute(
            'select Id, Childs from Words where Rank=?;', (rank,))
        data = [(r[0], np.frombuffer(r[1], dtype=np.int32)) for r in query]
        return data
    

    def dbget_pos_mean(self, rank):        
        pass
    
        
    def calc_pos_mean(self, rank):
        """Расчет матрицы матожиданий взаимных расстояний для слов ранга rank"""
        
        if rank < 0:
            print("Параметр rank не может быть меньше 0")
            return
        # Пересоздаем таблицу для матрицы матожиданий
        self.db.execute(
            '''
            CREATE TABLE IF NOT EXISTS MatrixM (
            [Row]    INTEGER NOT NULL,
            [Column] INTEGER NOT NULL,
            Mean     REAL NOT NULL,
            Rank     INTEGER NOT NULL,
            PRIMARY KEY ([Row], [Column]));
            ''')
        self.db.execute('delete from MatrixM where Rank=?',(rank,))
        # Запрос слов ранга rank+1
        words = self.dbget_words(rank + 1)
        if(len(words)==0):
            print('Слов в БД не найдено')
            return
        # Определим размер результирующей матрицы - максимальны Id
        n = max([w[1].max() for w in words])+1
        # Создаем вспомогательные матрицы для вычислений
        m_sum = sparse.lil_matrix((n, n), dtype=np.float32)
        m_count = sparse.lil_matrix((n, n), dtype=np.float32)
        words_count = len(words)
        timestart = timeit.default_timer()
        for idx, word in enumerate(words):
            if(idx%771==0):
                print(idx, 'из', words_count)
            for i, row in enumerate(word[1]):
                for j, column in enumerate(word[1]):
                    m_sum[row, column] += j-i
                    m_count[row, column] += 1
        print("Время: ", timeit.default_timer()-timestart)    
        print('Вычисление среднего')
        timestart = timeit.default_timer()
        # Вычисление среднего
        m_means = m_sum.tocsr(copy=False).multiply(m_count.tocsr(copy=False).power(-1, dtype=np.float32))        
        print("Время: ", timeit.default_timer()-timestart)        
        # запись в БД
        timestart = timeit.default_timer()
        print("Сохранение...")
        #means_tuples = sparse.find(m_means.tocoo())
        sparse.save_npz('means_'+str(rank)+'.npz', m_means)
#        for i in range(len(means_tuples[0])):
#            self.db.execute(
#                'INSERT OR REPLACE INTO MatrixM([Row], [Column], Mean, Rank) VALUES(?,?,?,?);',
#                (int(means_tuples[0][i]), int(means_tuples[1][i]), int(means_tuples[2][i]), rank))
        print("Время: ", timeit.default_timer()-timestart)
        timestart = timeit.default_timer()
        #self.db.commit()
   

    def calc_pos_cos_distance(self, rank):
        """Расчет матрицы косинусных расстояний контекстов"""
        
        print('Чтение данных')
        m = sparse.load_npz('means_'+str(rank)+'.npz')
        print('Вычисление расстояний')
        d = cosine_similarity(m, dense_output=False)
        print('Сохранение...')
        # Пересоздаем таблицу для матрицы матожиданий
        m_d = d.tocsr()
        sparse.save_npz('dist_'+str(rank)+'.npz', m_d)
            
        
        
    
    def calc_word_memebership_cov(self, rank):
        pass
    
    
    
    