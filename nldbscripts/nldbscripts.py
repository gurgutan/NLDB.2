import dbtransform
import calc
import splitter
import tokenizer
import search
import scipy.sparse as sparse
import names
import timeit
import numpy as np
import random
from platform import system
import shrinker
import tensorflow as tf



textname = '5mb.txt'
dbname = 'py5mb.db'

if system() == 'Linux':
    dbpath = '/home/ivan/dev/Data/Result/'+dbname
    text = '/home/ivan/dev/Data/Wiki/ru/'+textname
else:
    dbpath = 'D:/Data/Result/'+dbname
    text = 'D:/Data/Wiki/ru/'+textname


s = splitter.Splitter()
# print('Разбиение текста', text, 'на слова')
# text_tree = s.split_file(text)
# print('Векторизация текста')
# v = tokenizer.Vectorizer(dbpath, max_rank=3)
# v.vectorize(text_tree)

engine = calc.Calculations(dbpath)

# print('Вычисление memebership_matrix')
# engine.memebership_matrix()

# print('Вычисление context_mean_matrix')
# engine.context_mean_matrix()

# print('Вычисление context_similarity_matrix')
# engine.context_similarity_matrix()

# print('Вычисление membeship_similarity_matrix')
# engine.membeship_similarity_matrix()

cm = sparse.load_npz(names.fname_context_mean(dbpath))
wm = sparse.load_npz(names.fname_membership(dbpath))
cs = sparse.load_npz(names.fname_context_similarity(dbpath))
# ms = sparse.load_npz(names.fname_member_similarity(dbpath))

# start_time = timeit.default_timer()

# transformer = shrinker.Shrinker(in_size=cm.shape[0], out_size=16)
# print("Обучение")
# transformer.train(cm)

# print("Сохранение модели")
# transformer.save('p5mb_model.h5')

text = 'причина гражданской войны'
words_to_find_count = 4
print('Текст: ', text)

while text != '':
    text_tree = s.split_string(text)
    word = search.find_text(text_tree, 2, wm)
    if word[0] is not None:
        # Вектор контекста для слова word
        # v_long = cm[word[0]]
        # v_short = transformer.shrink(v_long)
        # print("Короткий вектор:", v_short)
        print('С уверенностью %.2f введено "%s"' % (word[1], engine.dbget_word(word[0])))
        # print("Поиск по принадлежности")
        # s1 = search.similars_by_membership(word[0], words_to_find_count, ms)
        # for t in s1:
        #     print(t[1], ':', t[0], engine.dbget_word(int(t[0])))
        print("Поиск по контексту:")
        s2 = search.similars_by_context(word[0], words_to_find_count, cs)
        for v in s2:
            print('  (%.2f, %i) "%s"' % (v[1], v[0], engine.dbget_word(int(v[0]))))
    print('Текст: ', end='')
    text = input()
# print(timeit.default_timer()-start_time)
