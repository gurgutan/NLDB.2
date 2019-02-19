import sqlite3 as sqlite
import numpy as np
import scipy.sparse as sparse
from sklearn.metrics.pairwise import cosine_similarity
import timeit
import os


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

    def fname_mean(self, rank):
        name, _ = os.path.splitext(self.dbpath)
        return name+'_r'+str(rank)+'_mean.npz'

    def fname_context_dist(self, rank):
        name, _ = os.path.splitext(self.dbpath)
        return name+'_r'+str(rank)+'_dist.npz'

    def fname_membership(self, rank):
        name, _ = os.path.splitext(self.dbpath)
        return name+'_r'+str(rank)+'_memb.npz'

    def fname_member_dist(self, rank):
        name, _ = os.path.splitext(self.dbpath)
        return name+'_r'+str(rank)+'_memb_dist.npz'

    def dbget_words(self, rank):
        query = self.db.execute(
            'select Id, Childs from Words where Rank=?;', (rank,))
        data = [(r[0], np.frombuffer(r[1], dtype=np.int32)) for r in query]
        return data

    def calc_context_mean_matrix(self, rank):
        """Расчет матрицы матожиданий взаимных расстояний для слов ранга rank"""

        if rank < 0:
            print("Параметр rank не может быть меньше 0")
            return
        # Запрос слов ранга rank+1
        words = self.dbget_words(rank + 1)
        if(len(words) == 0):
            print('Слов в БД не найдено')
            return
        # Определим размер результирующей матрицы - максимальны Id
        n = max([w[1].max() for w in words]) + 1
        # Создаем вспомогательные матрицы для вычислений
        m_sum = sparse.lil_matrix((n, n), dtype=np.float32)
        m_count = sparse.lil_matrix((n, n), dtype=np.float32)
        words_count = len(words)
        timestart = timeit.default_timer()
        for idx, word in enumerate(words):
            if(idx % 1771 == 0):
                print(idx, 'из', words_count,' ', end='\r', flush=True)
            for i, row in enumerate(word[1]):
                for j, column in enumerate(word[1]):
                    m_sum[row, column] += j - i
                    m_count[row, column] += 1
        print("Время: ", timeit.default_timer() - timestart)
        print('Вычисление среднего')
        timestart = timeit.default_timer()
        # Вычисление среднего
        m_means = m_sum.tocsr(copy=False).multiply(
            m_count.tocsr(copy=False).power(-1, dtype=np.float32))
        print("Время: ", timeit.default_timer() - timestart)
        # запись в БД
        timestart = timeit.default_timer()
        print("Сохранение...")
        sparse.save_npz(self.fname_mean(rank), m_means)
        print("Время: ", timeit.default_timer() - timestart)
        timestart = timeit.default_timer()
        # self.db.commit()

    def calc_context_similarity_matrix(self, rank):
        """Расчет матрицы косинусных расстояний контекстов"""

        print('Чтение данных')
        m = sparse.load_npz(self.fname_mean(rank))
        result = sparse.csr_matrix([0])
        rows_count = m.shape[0]
        batch_size = min(rows_count, 1 << 12)
        batch_count = int(rows_count / batch_size)
        print('Вычисление расстояний')
        for i in range(batch_count):
            first = i * batch_size
            last = min((i + 1) * batch_size, rows_count)
            d = cosine_similarity(m[first:last], dense_output=False)
            segment_name = str(last)
            print(segment_name,'  ', end='\r', flush=True)
            if(i == 0):
                result = d
            else:
                result = sparse.bmat([[result], [d]])
        result.eliminate_zeroes()
        print('Сохранение ', self.fname_context_dist(rank), '...')
        sparse.save_npz(self.fname_context_dist(rank), result)

    def calc_memebership_matrix(self, rank):

        words = self.dbget_words(rank+1)
        n = max([w[1].max() for w in words]) + 1
        m = max([w[0] for w in words])+1
        tuples = [(w[1][i], w[0]) for w in words for i in range(len(w[1]))]
        matrix = sparse.lil_matrix((n, m), dtype=np.int8)
        for t in tuples:
            matrix[t[0], t[1]] = 1 
        matrix.eliminate_zeroes()
        sparse.save_npz(self.fname_membership(rank), matrix.tocsr())
    
    def calc_membeship_similarity_matrix(self, rank):
        fname = self.fname_membership(rank)
        print('Чтение данных из', fname)
        m = sparse.load_npz(fname)
        result = sparse.csr_matrix([0])
        rows_count = m.shape[0]
        batch_size = min(rows_count, 1 << 14)
        batch_count = int(rows_count / batch_size)
        print('Вычисление схожести по включению')
        for i in range(batch_count):
            first = i * batch_size
            last = min((i + 1) * batch_size, rows_count)
            d = cosine_similarity(m[first:last], dense_output=False)
            segment_name = str(last)
            print(segment_name,'  ', end='\r', flush=True)
            if(i == 0):
                result = d
            else:
                result = sparse.bmat([[result], [d]])
        result.eliminate_zeroes()
        print('Сохранение ', self.fname_member_dist(rank), '...')
        sparse.save_npz(self.fname_member_dist(rank), result)
        
    def nearest_by_membership(self, i, rank):
        """Возвращает список пар (id_слова, величина_близости_к_i). Близость определяется по совместным вхождениям в другие слова"""
        m=sparse.load_npz(self.fname_member_dist(rank))
        a=m[i].toarray()[0] # i-я строка матрицы как 1-D массив
        row=np.argsort(a)   # индексы колонок, отсортированные по значению
        return [(i, a[i]) for i in reversed(row) if a[i]>0.0]
    
    def nearest_by_context(self, i, rank):
        """Возвращает список пар (id_слова, величина_близости_к_i). Близость определяется по схожести контекстов"""
        m=sparse.load_npz(self.fname_context_dist(rank))
        a=m[i].toarray()[0] # i-я строка матрицы как 1-D массив
        row=np.argsort(a)   # индексы колонок, отсортированные по значению
        return [(i, a[i]) for i in reversed(row) if a[i]>0.0]

    def get_word(self, token):
        pass

    
    