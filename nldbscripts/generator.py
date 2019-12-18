# #############################################################################
# В рамках проекта NLDB. Слеповичев И.И. 26.07.2019.
# -----------------------------------------------------------------------------
# Построитель фраз

import numpy as np
import scipy.sparse as sparse
from sklearn.metrics.pairwise import cosine_similarity


def build_term(words, M, size=8):
    """
    words - список кортежей вида (id, confidence), где id - слово, confidence - значение из [0,1],
    M - csr матрица средних расстояний между словами.
    Возвращает список id слов длиной 8
    """

    def dist(t, M):
        r = 0
        s = {}
        for i, row in enumerate(t):
            for j, col in enumerate(t):
                key = 10000019*row + col
                if(not key in s):
                    s[key] = (j-i, 1)
                else:
                    prev = s[key]
                    s[key] = (prev[0]+j-i, prev[1]+1)
        for i, row in enumerate(t):
            for j, col in enumerate(t):
                key = 10000019*row + col
                r += (s[key][0]/s[key][1]*M[row, col])
        return r
        # s = sparse.dok_matrix(M.shape, dtype=np.float32)
        # c = sparse.dok_matrix(M.shape, dtype=np.float32)
        # # Считаем матрицу средних расстояний между словами в терме t
        # for i, row in enumerate(t):
        #     for j, column in enumerate(t):
        #         s[row, column] += j-i
        #         c[row, column] += 1
        # c = c.power(-1, dtype=np.float32)
        # s = s.multiply(c)
        # # Считаем сумму квадратов расстояний между М и s
        # r = 0
        # for i, row in enumerate(t):
        #     for j, column in enumerate(t):
        #         # Считаем ошибку
        #         r += (s[row, column]-M[row, column])**2
        return r

    bag = {w[0]: w[1] for w in words}
    term = []
    for i in range(size):
        min_dist = 2**32
        word = None
        for w in bag.keys():
            # Повторять предыдущее слово нельзя, поэтому пропускаем
            temp_term = term + [w]
            # value = w[1]
            d = dist(temp_term, M)
            if(d/bag[w] < min_dist):
                min_dist = d/bag[w]
                word = w
        # Добавляем оптимальное слово к терму
        if word is None:
            return term
        bag.pop(word)
        term.append(word)
        print(word, end=' ')  # !!!
    return term
