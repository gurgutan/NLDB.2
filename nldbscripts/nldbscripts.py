import dbtransform
import calc
import splitter
import tokenizer


# old_dbpath = 'D:/Data/Result/5mb.db'#'/home/ivan/dev/Data/Result/884mb.db'
text = 'D:/Data/Wiki/ru/5mb.txt'
dbpath = 'D:/Data/Result/py5mb.db'#'/home/ivan/dev/Data/Result/884.db'

# print('Разбиение файла', text, 'на слова')
# s = splitter.Splitter()
# text_tree = s.split_file(text)
# print('Токенизация')
# t = tokenizer.Tokenizer(dbpath)
# t.tokenize(text_tree, 3)

engine = calc.Calculations(dbpath)

# print('Вычисление memebership_matrix')
# mm=engine.memebership_matrix()

# print('Вычисление context_mean_matrix')
# cmm=engine.context_mean_matrix()

# print('Вычисление context_similarity_matrix')
# csm=engine.context_similarity_matrix()

# print('Вычисление membeship_similarity_matrix')
# msm=engine.membeship_similarity_matrix()

# print(engine.dbget_word(70000))