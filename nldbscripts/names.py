# ###################################################################################
# В рамках проекта NLDB. Слеповичев И.И. 26.07.2019.
# -----------------------------------------------------------------------------
# В модуле функциями объявлены соглашения по именованию файлов данных.
# Функции возвращают имена файлов для указанного имени файла. Как правило,
# результирующее имя получается добавлением суффикса и соответствующего расширения.
# ###################################################################################
import os




def fname_context_mean(fname):
    """
    Возвращает имя файла с матрицей матожиданий взаимных расстояний слов
    """
    name, _ = os.path.splitext(fname)
    return name+'_cm.npz'

def fname_context_similarity(fname):
    name, _ = os.path.splitext(fname)
    return name+'_cs.npz'

def fname_membership(fname):
    name, _ = os.path.splitext(fname)
    return name+'_wm.npz'

def fname_member_similarity(fname):
    name, _ = os.path.splitext(fname)
    return name+'_ms.npz'
