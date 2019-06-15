# Слеповичев И.И. 14.06.2019
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
        s = sparse.dok_matrix(M.shape, dtype=np.float32)
        c = sparse.dok_matrix(M.shape, dtype=np.float32)
        # Считаем матрицу средних расстояний между словами в терме t
        for i, row in enumerate(t):
            for j, column in enumerate(t):
                s[row, column] += j-i
                c[row, column] += 1
        c = c.power(-1, dtype=np.float32)
        s = s.multiply(c)
        # Считаем сумму квадратов расстояний между М и s
        r = 0
        for i, row in enumerate(t):
            for j, column in enumerate(t):
                # Считаем квадратичную ошибку
                r += (s[row, column]-M[row, column])**2
        return r

    bag = set([w[0] for w in words])
    term = []
    for i in range(size):
        min_dist = 2**32
        word = None
        for w in bag:
            # Повторять предыдущее слово нельзя, поэтому пропускаем
            temp_term = term + [w]
            # value = w[1]
            d = dist(temp_term, M)
            if(d < min_dist):
                min_dist = d
                word = w
        # Добавляем оптимальное слово к терму
        bag.remove(word)
        term.append(word)
        print(word, end=' ')  # !!!
    return term
