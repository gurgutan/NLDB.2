import dbtransform
import calc
import splitter
import tokenizer
import search
import scipy.sparse as sparse
import names
import timeit
from platform import system


textname = '23mb.txt'
dbname = 'py23mb.db'

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

engine = calc.Calculations(dbpath)

# print('Вычисление memebership_matrix')
# engine.memebership_matrix()

# print('Вычисление context_mean_matrix')
# engine.context_mean_matrix()

# print('Вычисление context_similarity_matrix')
# engine.context_similarity_matrix()

# print('Вычисление membeship_similarity_matrix')
# engine.membeship_similarity_matrix()

wm = sparse.load_npz(names.fname_membership(dbpath))
# ms = sparse.load_npz(names.fname_member_similarity(dbpath))
cs = sparse.load_npz(names.fname_context_similarity(dbpath))
start_time = timeit.default_timer()
# token = search.find_word([ord(c) for c in 'патока'], 1, wm)
text = 'причины'
print('Текст: ', text)
while text != '':
    text_tree = s.split_string(text)
    word = search.find_text_tree(text_tree, 1, wm)
    if word[0] is not None:
        print(word[1], ':', engine.dbget_word(word[0]))
        # s1 = search.similars_by_membership(word[0], 4, ms)
        s2 = search.similars_by_context(word[0], 4, cs)
        # for t in s1:
        #     print(t[1], ':', engine.dbget_word(t[0]))
        for t in s2:
            print(t[1], ':', t[0], engine.dbget_word(int(t[0])))
    print('Текст: ', end='')
    text = input()
# print(timeit.default_timer()-start_time)
