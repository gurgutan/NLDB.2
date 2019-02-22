import dbtransform
import calc
import splitter
import tokenizer
import search
import scipy.sparse as sparse
import names
import timeit


# old_dbpath = 'D:/Data/Result/5mb.db'#'/home/ivan/dev/Data/Result/884mb.db'
text = 'D:/Data/Wiki/ru/5mb.txt'
dbpath = 'D:/Data/Result/py5mb.db'  # '/home/ivan/dev/Data/Result/884.db'

# print('Разбиение файла', text, 'на слова')
# s = splitter.Splitter()
# text_tree = s.split_file(text)
# print('Токенизация')
# t = tokenizer.Tokenizer(dbpath)
# t.tokenize(text_tree, 3)

engine = calc.Calculations(dbpath)

print('Вычисление memebership_matrix')
wm = engine.memebership_matrix()

# print('Вычисление context_mean_matrix')
# cmm=engine.context_mean_matrix()

# print('Вычисление context_similarity_matrix')
# csm=engine.context_similarity_matrix()

# print('Вычисление membeship_similarity_matrix')
# msm=engine.membeship_similarity_matrix()

wm = sparse.load_npz(names.fname_membership(dbpath))
start_time = timeit.default_timer()
token = search.find_word([ord(c) for c in 'патока'], wm)
print(timeit.default_timer()-start_time)
word = engine.dbget_word(token)
print(token, word)
# print(engine.dbget_word(70000))
