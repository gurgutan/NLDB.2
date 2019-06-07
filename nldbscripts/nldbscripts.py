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
    text = 'D:/Data/Wiki/ru/'+textname
    dbpath = 'D:/Data/Result/'+dbname


s = splitter.Splitter()
# print('Разбиение файла', text, 'на слова')
# text_tree = s.split_file(text)
# print('Токенизация')
# t = tokenizer.Tokenizer(dbpath)
# t.tokenize(text_tree, 3)

# engine = calc.Calculations(dbpath)

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
# ms = sparse.load_npz(names.fname_member_similarity(dbpath))
cs = sparse.load_npz(names.fname_context_similarity(dbpath))
start_time = timeit.default_timer()

# Размер обучающей выборки равен количеству ненулевых элементов матрицы
print("Подготовка обучающей выборки")
# train_size = 1000
# inputX_train = []
# inputY_train = []
# output_train = []
# for i in range(train_size):
#     n_x = random.randint(0, cm.shape[0])
#     n_y = random.randint(0, cm.shape[0])
#     output = [0]
#     inputX_train += [cm[n_x].toarray()[0]]
#     inputY_train += [cm[n_y].toarray()[0]]
#     output_train += output
# x_train = tf.data.Dataset.from_tensor_slices(([inputX_train, inputY_train]
# y_train = np.array(output_train)

transformer = shrinker.Shrinker(in_size=cm.shape[0], out_size=64)
print("Обучение")
transformer.train(cm)

text = 'причина'
print('Текст: ', text)

while text != '':
    text_tree = s.split_string(text)
    word = search.find_text_tree(text_tree, 1, wm)
    if word[0] is not None:
        # Вектор контекста для слова word
        v_long = cm[word[0]]
        v_short = transformer.shrink(v_long)

        print("Короткий вектор:", v_short)
        print(word[1], ':', engine.dbget_word(word[0]))
        # print("Поиск по принадлежности")
        # s1 = search.similars_by_membership(word[0], 8, ms)
        # for t in s1:
        #     print(t[1], ':', t[0], engine.dbget_word(int(t[0])))
        print("Поиск по контексту")
        s2 = search.similars_by_context(word[0], 8, cs)
        for t in s2:
            print(t[1], ':', t[0], engine.dbget_word(int(t[0])))
    print('Текст: ', end='')
    text = input()
# print(timeit.default_timer()-start_time)
