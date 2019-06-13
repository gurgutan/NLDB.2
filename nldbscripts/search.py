import scipy.sparse as sparse
import numpy as np
from sklearn.metrics.pairwise import cosine_similarity
from const import WORD_SIZES
from scipy.spatial.distance import cdist
import alphabet

letters = alphabet.Alphabet()


def ischar(c):
    return type(c) == str and len(c) == 1


def isstring(s):
    return type(s) == str


def istoken(t):
    return type(t) == int


def find_word(ids, scores, rank, wm):
    word_size = len(ids)
    row = [0 for i in ids]
    scale = WORD_SIZES[rank-1]
    column = [child_id*scale+int(child_pos*scale/word_size)
              for child_pos, child_id in enumerate(ids)]
    # вектор-строка для расчета имеет размерность равную количеству строк матрицы wm
    w = sparse.csr_matrix(
        (scores, (row, column)),
        shape=(1, wm.shape[1]),
        dtype=np.float32)
    # результат - матрица состоит из одной строки
    cos_sim = cosine_similarity(w, wm, dense_output=False)
    c = int(cos_sim.argmax())
    score = cos_sim[0, c]
    result = (c, score)
    return result


def find_text(text_tree, rank, wm):
    '''
    Возвращает кортеж (id, confidence), где id - идентификатор слова, confidence - уверенность в слове
    '''
    if ischar(text_tree):
        return (letters.get_int(text_tree), 1)
    elif isinstance(text_tree, (list, str)):
        e = [find_text(t, rank-1, wm) for t in text_tree]
        childs = [t[0] for t in e if t[0] is not None]
        scores = [t[1] for t in e if t[0] is not None]
    return find_word(childs, scores, rank, wm)


def similars_by_membership(id, count, m):
    """
    Возвращает список пар (id_слова, величина_схожести).
    Близость определяется по совместным вхождениям в другие слова
    m - разреженная матрица принадлежности
    token - идентификатор слова
    """
    # m = sparse.load_npz(fname_member_dist())
    a = m[id].toarray()[0]  # id-я строка матрицы как 1-D массив
    # индексы колонок, отсортированные по значению
    indices = np.argsort(a)
    row = [(i, a[i]) for i in indices if a[i] > 0.0]
    result = sorted(row, key=lambda t: t[1], reverse=True)
    return result[:count]


def similars_by_context(id, count, m):
    """
    Возвращает список пар (id_слова, величина_близости_к_id).
    Близость определяется по схожести контекстов.
    m - разреженная матрица контекстов
    token - идентификатор слова
    """
    # m = sparse.load_npz(fname_context_dist())
    a = m[id].toarray()[0]  # id-я строка матрицы как 1-D массив
    # индексы колонок, отсортированные по значению
    indices = np.argsort(a)
    row = [(i, a[i]) for i in indices if a[i] > 0.0]
    result = sorted(row, key=lambda t: t[1], reverse=True)
    return result[:count]
