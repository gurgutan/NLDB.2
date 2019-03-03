import scipy.sparse as sparse
import numpy as np
from sklearn.metrics.pairwise import cosine_similarity
from const import WORD_SIZES
from scipy.spatial.distance import cdist


def ischar(c):
    return type(c) == str and len(c) == 1


def isstring(s):
    return type(s) == str


def istoken(t):
    return type(t) == int


def find_word(tokens_list, rank, wm):
    word_size = len(tokens_list)
    data = [1 for i in range(word_size)]
    row = [0 for i in range(word_size)]
    column = []
    scale = WORD_SIZES[rank-1]
    for child_pos, child_id in enumerate(tokens_list):
        c = child_id*scale+int(child_pos*scale/word_size)
        column.append(c)
        # column = [t*WORD_SIZES+min([i, WORD_SIZES])
        #           for i, t in enumerate(tokens_list)]
    w = sparse.csr_matrix(
        (data, (row, column)),
        shape=(1, wm.shape[1]),
        dtype=np.int8)
    cos_sim = cosine_similarity(w, wm, dense_output=False)
    t = int(cos_sim.argmax())
    score = cos_sim[0, t]
    result = (t, score)
    return result


def find_text_tree(text_tree, rank, wm):
    if ischar(text_tree):
        word = ord(text_tree)
        return (word, 1)
    elif isinstance(text_tree, (list, str)):
        e = [find_text_tree(t, rank-1, wm) for t in text_tree]
        childs = [t[0] for t in e if t[0] is not None]
        scores = np.array(
            [t[1] for t in e if t[0] is not None], dtype=np.float32)
    word = find_word(childs, rank, wm)
    if word[0] is None:
        return (None, None, None)
    childs_score = np.sqrt(np.dot(scores, scores.T))
    score = word[1]*childs_score/len(text_tree)
    return (word[0], score)


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
