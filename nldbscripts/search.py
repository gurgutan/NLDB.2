import scipy.sparse as sparse
import numpy as np
from sklearn.metrics.pairwise import cosine_similarity
from const import WORD_MAX_SIZE


def find_word(tokens_list, wm):
    size = len(tokens_list)
    data = [1 for i in range(size)]
    row = [0 for i in range(size)]
    column = [t*WORD_MAX_SIZE+min([i, WORD_MAX_SIZE])
              for i, t in enumerate(tokens_list)]
    w = sparse.csr_matrix(
        (data, (row, column)),
        shape=(1, wm.shape[1]), dtype=np.int8)
    cos_sim = cosine_similarity(w, wm, dense_output=False)
    result = int(cos_sim.argmax())
    return int(result)


def similars_by_membership(token, m):
    """
    Возвращает список пар (id_слова, величина_схожести).
    Близость определяется по совместным вхождениям в другие слова
    m - разреженная матрица принадлежности
    token - идентификатор слова
    """
    # m = sparse.load_npz(fname_member_dist())
    a = m[token].toarray()[0]  # i-я строка матрицы как 1-D массив
    # индексы колонок, отсортированные по значению
    row = np.argsort(a)
    return [(i, a[i]) for i in reversed(row) if a[i] > 0.0]


def similars_by_context(token, m):
    """
    Возвращает список пар (id_слова, величина_близости_к_i).
    Близость определяется по схожести контекстов.
    m - разреженная матрица контекстов
    token - идентификатор слова
    """
    # m = sparse.load_npz(fname_context_dist())
    a = m[token].toarray()[0]  # i-я строка матрицы как 1-D массив
    # индексы колонок, отсортированные по значению
    row = np.argsort(a)
    return [(i, a[i]) for i in reversed(row) if a[i] > 0.0]
