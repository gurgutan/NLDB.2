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


textname = '23mb.txt'
dbname = '23mb.db'

if system() == 'Linux':
    dbpath = '/home/ivan/dev/Data/Result/'+dbname
    text = '/home/ivan/dev/Data/Wiki/ru/'+textname
else:
    dbpath = 'D:/Data/Result/'+dbname
    text = 'D:/Data/Wiki/ru/'+textname


s = splitter.Splitter()
# print('Разбиение текста "%s" на слова' % text)
# text_tree = s.split_file(text)
# print('Векторизация текста "%s"' % text)
# v = tokenizer.Vectorizer(dbpath, max_rank=3)
# v.vectorize(text_tree)

engine = calc.Calculations(dbpath)

print('Вычисление memebership_matrix')
engine.memebership_matrix()

print('Вычисление context_mean_matrix')
engine.context_mean_matrix()

print('Вычисление context_similarity_matrix')
engine.context_similarity_matrix()

print('Вычисление membeship_similarity_matrix')
engine.membeship_similarity_matrix()

print("Загрузка данных...")
cm = sparse.load_npz(names.fname_context_mean(dbpath))
wm = sparse.load_npz(names.fname_membership(dbpath))
cs = sparse.load_npz(names.fname_context_similarity(dbpath))
ms = sparse.load_npz(names.fname_member_similarity(dbpath))

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
        print('С уверенностью %.2f введено "%s"' %
              (word[1], engine.dbget_word(word[0])))
        print("Поиск по контексту:")
        s = search.get_similars(word[0], words_to_find_count, cs)
        print(['(%.2f, %i) %s' % (v[1], v[0], engine.dbget_word(int(v[0]))) for v in s])
        s = search.get_similars(word[0], words_to_find_count, ms)
        print(['(%.2f, %i) %s' % (v[1], v[0], engine.dbget_word(int(v[0]))) for v in s])
    print('Текст: ', end='')
    text = input()
# print(timeit.default_timer()-start_time)
