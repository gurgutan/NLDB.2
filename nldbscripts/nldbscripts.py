import dbtransform
import nldb
import splitter
import tokenizer

# old_dbpath = 'D:/Data/Result/5mb.db'#'/home/ivan/dev/Data/Result/884mb.db'
text = 'D:/Data/Wiki/ru/5mb.txt'
dbpath = 'D:/Data/Result/py5mb.db'#'/home/ivan/dev/Data/Result/884.db'


# print('Разбиение файла',text,'на слова')
# s = splitter.Splitter()
# text_tree = s.split_file(text)
# print('Токенизация')
# t = tokenizer.Tokenizer(dbpath)
# t.tokenize(text_tree, 3)

# dbtransform.download_splitters(old_dbpath, db)
# dbtransform.download_words(old_dbpath, db)

engine = nldb.Calculations(dbpath)

# print('Вычисление calc_memebership_matrix(0) ')
# engine.calc_memebership_matrix(0)
# print('Вычисление calc_memebership_matrix(1) ')
# engine.calc_memebership_matrix(1)
# print('Вычисление calc_memebership_matrix(2) ')
# engine.calc_memebership_matrix(2)

# print('Вычисление calc_context_mean_matrix(0)')
# engine.calc_context_mean_matrix(0)
# print('Вычисление calc_context_mean_matrix(1)')
# engine.calc_context_mean_matrix(1)
# print('Вычисление calc_context_mean_matrix(2)')
# engine.calc_context_mean_matrix(2)

# print('Вычисление calc_context_similarity_matrix(0)')
# engine.calc_context_similarity_matrix(0)
# print('Вычисление calc_context_similarity_matrix(1)')
# engine.calc_context_similarity_matrix(1)
# print('Вычисление calc_context_similarity_matrix(2)')
# engine.calc_context_similarity_matrix(2)

print('Вычисление calc_membeship_similarity_matrix(0)')
engine.calc_membeship_similarity_matrix(0)
print('Вычисление calc_membeship_similarity_matrix(1)')
engine.calc_membeship_similarity_matrix(1)
print('Вычисление calc_membeship_similarity_matrix(2)')
engine.calc_membeship_similarity_matrix(2)
