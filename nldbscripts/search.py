# #############################################################################
# В рамках проекта NLDB. Слеповичев И.И. 26.07.2019.
# -----------------------------------------------------------------------------
# Модуль с функциями нечеткого поиска структурных слов в словаре.
# Для поиска используется мера схожести косинусное расстояние.
# Словарем является разреженная матрица принадлежности слов, в которой
#  - столбцы соответствуют словам ранга r;
#  - строки соответствуют словам ранга r-1;
#  - на пересечении i-й строки и j-го столбца стоит 1 только в том случае,
#  когда слово с идентификатором i является подсловом слова с идентификатором j.
# Искомое слово передается либо как список идентификаторов подслов, либо как
# дерево букв.
# Например, слово ранга 1 - "АЗ", может быть представлено как [1,13]
# или как ['А','З'], а слово ранга 2 (предложение) "АЗ ЕСМЬ ЦАРЬ",
# как [65701, 65709, 65777] - идентификаторами слов "АЗ", "ЕСМЬ" и "ЦАРЬ",
# или  как [['А','З'],['Е','С','М','Ь'],['Ц','А','Р','Ь']]
# #############################################################################


import scipy.sparse as sparse
import numpy as np
from sklearn.metrics.pairwise import cosine_similarity
from constants import WORD_SIZES
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
    '''
    Функция возвращает пару (id, score), где c - id искомого слова, а score - величина
    схожести найденного слова со словом ids.
    Нечеткий поиск слова ранга rank в матрице wm по дочерним словам, заданным списком ids.
    Так как слово представлено списком id, функция ищет в двоичной матрице wm строку, 
    косинусная схожесть которой с двоичным представлением слова (вектора ids) максимально.
    ids - вектор идентификаторов дочерних слов (подслов, т.е. букв для слова, слов для фразы и т.п.) искомого слова;
    scores - вектор уверенности в соответствующих подсловах из вектора ids (сопоставляются в естеств-м порядке в списках)
    rank - ранг искомого слова, нужен для правильного выбора масштаба
    wm - матрица принадлежности слов (столбцы - номера слов ранга r (напр. слов), строки - номера слов ранга r-1 (напр., букв)
    '''
    word_size = len(ids)
    row = [0 for i in ids]
    scale = WORD_SIZES[rank-1]
    # Преобразуем вектор id в двоичное представление с учетом масштабирования индекса id
    # как числа к масштабу заданному WORD_SIZES
    column = [child_id*scale+int(child_pos*scale/word_size)
              for child_pos, child_id in enumerate(ids)]
    # получим для искомого слова представление в виде матрицы 1xN, где N - число столбцов wm
    w = sparse.csr_matrix(
        (scores, (row, column)),
        shape=(1, wm.shape[1]),
        dtype=np.float32)
    # результат - вектор величин схожести w с каждой из строк wm
    cos_sim = cosine_similarity(w, wm, dense_output=False)
    # номер максимального элемента вектора cos_sim - номер (id) искомого слова
    id = int(cos_sim.argmax())
    # также получим величину схожести
    score = cos_sim[0, id]
    return (id, score)


def find_id(text_tree, rank, wm):
    '''
    Нечеткий поиск слова (id слова) в матрице wm по древовидному представлению text_tree.
    Возвращает кортеж (id, confidence), где id - идентификатор слова, confidence - уверенность в слове.
    text_tree - список, элементы которого - либо списки (такие же), либо строки
    rank - ранг искомого слова, который не должен быть больше глубины дерева text_tree
    '''
    if ischar(text_tree):
        return (letters.get_int(text_tree), 1)
    elif isinstance(text_tree, (list, str)):
        e = [find_id(t, rank-1, wm) for t in text_tree]
        childs = [t[0] for t in e if t[0] is not None]
        scores = [t[1] for t in e if t[0] is not None]
    return find_word(childs, scores, rank, wm)


def get_similars(id, count, m):
    """
    Возвращает список пар (id, confidence), где id - идентификатор слова, confidence - уверенность в слове
    Близость определяется по совместным вхождениям в другие слова
    m - разреженная матрица сходства
    token - идентификатор слова
    """
    # m = sparse.load_npz(fname_member_dist())
    a = m[id].toarray()[0]  # id-я строка матрицы как 1-D массив
    # индексы колонок, отсортированные по значению
    indices = np.argsort(a)
    row = [(i, a[i]) for i in indices if a[i] > 0.0]
    result = sorted(row, key=lambda t: t[1], reverse=True)
    return result[:count]
