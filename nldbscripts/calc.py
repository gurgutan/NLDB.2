import sqlite3 as sqlite
import numpy as np
import scipy.sparse as sparse
from sklearn.metrics.pairwise import cosine_similarity
import timeit
import os
import names
from const import *
from tqdm import tqdm


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
        query = self.db.execute('select Id, Childs, Rank from Words')
        data = [
            (r[0], np.frombuffer(r[1], dtype=np.int32), r[2])
            for r in query
        ]
        return data

    def dbget_word(self, token):
        """Получение слова из БД по идентификатору token"""
        if token <= 0:
            return None
        if token <= 65535:
            return chr(token)
        cursor = self.db.cursor()
        first = cursor.execute(
            'select Id, Childs, Rank from Words where Id=?',
            (token,)).fetchone()
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
        # размерность матрицы на 1 больше максимального индекса
        n = max([w[0] for w in words]) + 1
        # количество столбцов в WORD_MAX_SIZE больше количества строк
        m = n*sum(WORD_SIZES)
        data, rows, columns = [], [], []
        with tqdm(total=len(words), ncols=100, mininterval=0.5) as progress:
            for w in words:
                word_id = w[0]
                word_size = len(w[1])
                word_rank = w[2]
                scale = WORD_SIZES[word_rank-1]
                for child_pos, child_id in enumerate(w[1]):
                    col = child_id*scale+int(child_pos*scale/word_size)
                    data.append(1)
                    rows.append(word_id)
                    columns.append(col)
                progress.update(1)
        csr_matrix = sparse.csr_matrix((data, (rows, columns)), dtype=np.int8)
        if save:
            print("Запись в файл ", names.fname_membership(self.dbpath))
            sparse.save_npz(names.fname_membership(self.dbpath), csr_matrix)
        return csr_matrix

    def context_mean_matrix(self, save=True):
        """Расчет матрицы матожиданий взаимных расстояний"""
        words = self.dbget_words()
        if(len(words) == 0):
            print('Слов в БД не найдено')
            return
        # Списки индексов и значений для разреженных матриц
        sum_row, sum_col, sum_data = [], [], []
        count_row, count_col, count_data = [], [], []
        with tqdm(total=len(words)*2, ncols=120, mininterval=0.5) as progress:
            for idx, word in enumerate(words):
                for i, row in enumerate(word[1]):
                    for j, column in enumerate(word[1]):
                        sum_row.append(row)
                        sum_col.append(column)
                        sum_data.append(j-i)
                progress.update(1)
            m_sum = sparse.csr_matrix(
                (sum_data, (sum_row, sum_col)), dtype=np.float32)
            sum_row, sum_col, sum_data = None, None, None
            for idx, word in enumerate(words):
                for i, row in enumerate(word[1]):
                    for j, column in enumerate(word[1]):
                        count_row.append(row)
                        count_col.append(column)
                        count_data.append(1)
                progress.update(1)
        m_count = sparse.csr_matrix(
            (count_data, (count_row, count_col)), dtype=np.float32)
        count_row, count_col, count_data = None, None, None
        # Вычисление среднего
        start_time = timeit.default_timer()
        print("Вычисление среднего")
        m_count = m_count.power(-1, dtype=np.float32)
        m_means = m_sum.multiply(m_count)
        m_means.eliminate_zeros()
        print("Время вычислений:", timeit.default_timer()-start_time)
        if save:
            print("Запись в файл ", names.fname_context_mean(self.dbpath))
            sparse.save_npz(names.fname_context_mean(self.dbpath), m_means)
        return m_means

    def context_similarity_matrix(self, save=True):
        """Расчет матрицы косинусных расстояний контекстов"""
        m = sparse.load_npz(names.fname_context_mean(self.dbpath))
        rows_count = m.shape[0]
        batch_size = min(rows_count, BATCH_SIZE)
        batch_count = int(rows_count / batch_size)
        start_time = timeit.default_timer()
        with tqdm(total=batch_count**2, ncols=120, mininterval=0.5) as progress:
            for i in range(batch_count):
                for j in range(batch_count):
                    x_first = i * batch_size
                    x_last = min((i + 1) * batch_size, rows_count)
                    y_first = j * batch_size
                    y_last = min((j + 1) * batch_size, rows_count)
                    xy = cosine_similarity(
                        m[x_first:x_last], m[y_first:y_last], dense_output=False)
                    self._eliminate_subzeroes(xy, 0.1)
                    progress.update(1)
                    if j == 0:
                        d = xy
                    else:
                        d = sparse.bmat([[d, xy]], format='csr')
                if(i == 0):
                    result = d
                else:
                    result = sparse.bmat([[result], [d]], format='csr')
        print("")
        print("Время вычислений:", timeit.default_timer()-start_time)
        result.eliminate_zeros()
        if save:
            print("Запись в файл ",
                  names.fname_context_similarity(self.dbpath))
            sparse.save_npz(
                names.fname_context_similarity(self.dbpath), result)
        return result

    def membeship_similarity_matrix(self, save=True):
        """Расчет матрицы схожестви по принадлежности"""
        try:
            m = sparse.load_npz(names.fname_membership(self.dbpath))
        except:
            print("Не найден файл ", names.fname_membership(self.dbpath))
            print("Файл содержит данные для расчета матрицы подобия")
            exit
        result = sparse.csr_matrix([0])
        rows_count = m.shape[0]
        batch_size = min(rows_count, BATCH_SIZE)
        batch_count = int(rows_count / batch_size)
        start_time = timeit.default_timer()
        with tqdm(total=batch_count**2, ncols=100, mininterval=0.5) as progress:
            for i in range(batch_count):
                for j in range(batch_count):
                    x_first = i * batch_size
                    x_last = min((i + 1) * batch_size, rows_count)
                    y_first = j * batch_size
                    y_last = min((j + 1) * batch_size, rows_count)
                    if (x_last-x_first)*(y_last-y_first) == 0:
                        continue
                    xy = cosine_similarity(
                        m[x_first:x_last], m[y_first:y_last], dense_output=False)
                    self._eliminate_subzeroes(xy, 0.5)
                    progress.update(1)
                    if j == 0:
                        d = xy
                    else:
                        d = sparse.bmat([[d, xy]], format='csr')
                if(i == 0):
                    result = d
                else:
                    result = sparse.bmat([[result], [d]], format='csr')
        result.eliminate_zeros()
        print("")
        print("Время вычислений:", timeit.default_timer()-start_time)
        if save:
            print("Запись в файл ", names.fname_member_similarity(self.dbpath))
            sparse.save_npz(names.fname_member_similarity(self.dbpath), result)
        return result

    def _eliminate_subzeroes(self, m: sparse.csr_matrix, epsilon):
        m.data = np.where(abs(m.data) < epsilon, 0, m.data)
        m.eliminate_zeros()
