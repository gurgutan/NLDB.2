import scipy.sparse as sparse
import numpy as np
from sklearn.metrics.pairwise import cosine_similarity
from const import WORD_MAX_SIZE
from scipy.spatial.distance import cdist


def ischar(c):
    return type(c) == str and len(c) == 1


def isstring(s):
    return type(s) == str


def istoken(t):
    return type(t) == int


def find_word(tokens_list, wm):
    size = len(tokens_list)
    data = [1 for i in range(size)]
    row = [0 for i in range(size)]
    column = [t*WORD_MAX_SIZE+min([i, WORD_MAX_SIZE])
              for i, t in enumerate(tokens_list)]
    w = sparse.csr_matrix(
        (data, (row, column)),
        shape=(1, wm.shape[1]),
        dtype=np.int8)
    cos_sim = cosine_similarity(w, wm, dense_output=False)
    t = int(cos_sim.argmax())
    score = cos_sim[0][t]
    result = (t, score)
    return result


def estimate_text_tree(text_tree, wm):
    if ischar(text_tree):
        childs = ord(text_tree)
        scores = np.ones((1, len(text_tree)), dtype=np.float32)
    if isinstance(text_tree, list):
        e = [estimate_text_tree(t, wm) for t in text_tree]
        childs = [t[0] for t in e]
        scores = np.array([t[1] for t in e], dtype=np.float32)
    word = find_word(childs, wm)
    childs_score = np.sqrt(np.dot(scores, scores.T))
    score = word[1]*childs_score/len(text_tree)
    return


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
