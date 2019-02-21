import sqlite3 as sqlite
import numpy as np
import scipy.sparse as sparse
from sklearn.metrics.pairwise import cosine_similarity
import timeit
import os
import names


class Calculations(object):
    """description of class"""

    dbpath = ""
    words_matrix = None

    def __init__(self, dbpath):
        """dbpath - полное имя файла БД sqlite3"""
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

    def dbget_words(self):
        """Получение списка слов из sqlite3 БД по пути dbpath"""
        query = self.db.execute('select Id, Childs from Words')
        data = [(r[0], np.frombuffer(r[1], dtype=np.int32)) for r in query]
        return data

    def dbget_word(self, token):
        """Получение слова из БД по идентификатору token"""
        if token <= 0:
            return None
        if token <= 65535:
            return chr(token)
        cursor = self.db.cursor()
        first = cursor.execute(
            'select Id, Childs from Words where Id=?', (token,)).fetchone()
        if first is None:
            return None
        a = np.frombuffer(first[1], dtype=np.int32)
        if len(a) == 0:
            return None
        if a[0] <= 65535:
            word = ''.join([chr(c) for c in a])
        else:
            word = [self.dbget_word(int(x)) for x in a]
        return word

    def dbget_word_binvec(self, token, length):
        """Вовзращает бинарное представление слова в разреженной матрице"""
        if token <= 0:
            return None
        if token <= 65535:
            return sparse.csr_matrix(([1], ([0], [token])), shape=(1, length))

    def memebership_matrix(self, save=True):
        """Расчет матрицы принадлежности"""
        words = self.dbget_words()
        n = max([w[1].max() for w in words]) + 1
        m = max([w[0] for w in words]) + 1
        tuples = [(w[0], w[1][i]) for w in words for i in range(len(w[1]))]
        matrix = sparse.lil_matrix((n, m), dtype=np.int16)
        for t in tuples:
            matrix[t[1], t[0]] = 1
        csr_matrix = matrix.tocsr()
        csr_matrix.eliminate_zeros()
        self.words_matrix = csr_matrix.tocsc()
        if save:
            sparse.save_npz(names.fname_membership(self.dbpath), csr_matrix)
        return csr_matrix

    def context_mean_matrix(self, save=True):
        """Расчет матрицы матожиданий взаимных расстояний"""
        words = self.dbget_words()
        if(len(words) == 0):
            print('Слов в БД не найдено')
            return
        # Определим размер результирующей матрицы - максимальны Id
        words_count = len(words)
        # Списки индексов и значений для разреженных матриц
        sum_row, sum_col, sum_data = [], [], []
        count_row, count_col, count_data = [], [], []
        for idx, word in enumerate(words):
            if(idx % 1771 == 0):
                print(idx, 'из', words_count, ' ', end='\r', flush=True)
            for i, row in enumerate(word[1]):
                for j, column in enumerate(word[1]):
                    sum_row.append(row)
                    sum_col.append(column)
                    sum_data.append(j-i)
                    count_row.append(row)
                    count_col.append(column)
                    count_data.append(1)
        m_sum = sparse.coo_matrix(
            (sum_data, (sum_row, sum_col)), dtype=np.float32)
        m_count = sparse.coo_matrix(
            (count_data, (count_row, count_col)), dtype=np.float32)
        # Вычисление среднего
        m_means = m_sum.tocsr(copy=False).multiply(
            m_count.tocsr(copy=False).power(-1, dtype=np.float32))
        m_means.eliminate_zeros()
        if save:
            sparse.save_npz(names.fname_context_mean(self.dbpath), m_means)
        return m_means

    def context_similarity_matrix(self, save=True):
        """Расчет матрицы косинусных расстояний контекстов"""
        m = sparse.load_npz(names.fname_context_mean(self.dbpath))
        result = sparse.csr_matrix([0])
        rows_count = m.shape[0]
        batch_size = min(rows_count, 1 << 12)
        batch_count = int(rows_count / batch_size)
        for i in range(batch_count):
            first = i * batch_size
            last = min((i + 1) * batch_size, rows_count)
            d = cosine_similarity(m[first:last], dense_output=False)
            segment_name = str(last)
            print(segment_name, '/', rows_count, ' ', end='\r', flush=True)
            if(i == 0):
                result = d
            else:
                result = sparse.bmat([[result], [d]], format='csr')
        result.eliminate_zeros()
        if save:
            sparse.save_npz(
                names.fname_context_similarity(self.dbpath), result)
        return result

    def membeship_similarity_matrix(self, save=True):
        """Расчет матрицы схожестви по принадлежности"""
        m = sparse.load_npz(names.fname_membership(self.dbpath))
        result = sparse.csr_matrix([0])
        rows_count = m.shape[0]
        batch_size = min(rows_count, 1 << 14)
        batch_count = int(rows_count / batch_size)
        for i in range(batch_count):
            first = i * batch_size
            last = min((i + 1) * batch_size, rows_count)
            d = cosine_similarity(m[first:last], dense_output=False)
            segment_name = str(last)
            print(segment_name, '/', rows_count, ' ', end='\r', flush=True)
            if(i == 0):
                result = d
            else:
                result = sparse.bmat([[result], [d]])
        result.eliminate_zeros()
        if save:
            sparse.save_npz(names.fname_member_similarity(self.dbpath), result)
        return result