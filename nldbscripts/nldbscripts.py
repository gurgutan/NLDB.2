import dbtransform
import nldb
import splitter
import tokenizer

# old_dbpath = 'D:/Data/Result/5mb.db'#'/home/ivan/dev/Data/Result/884mb.db'
text = 'D:/Data/Wiki/ru/5mb.txt'
dbpath = 'D:/Data/Result/py5mb.db'#'/home/ivan/dev/Data/Result/884.db'

# dbtransform.download_splitters(old_dbpath, db)
# dbtransform.download_words(old_dbpath, db)

#print('Разбиение файла', text, 'на слова')
#s = splitter.Splitter()
#text_tree = s.split_file(text)
#print('Токенизация')
#t = tokenizer.Tokenizer(dbpath)
#t.tokenize(text_tree, 3)

engine = nldb.Calculations(dbpath)

print(engine.get_word(70000))

# print('Вычисление calc_memebership_matrix')
# engine.calc_memebership_matrix()

# print('Вычисление calc_context_mean_matrix')
# engine.calc_context_mean_matrix()

# print('Вычисление calc_context_similarity_matrix')
# engine.calc_context_similarity_matrix()

# print('Вычисление calc_membeship_similarity_matrix')
# engine.calc_membeship_similarity_matrix()
