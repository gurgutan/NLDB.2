import dbtransform
import nldb
import splitter
import tokenizer

#old_dbpath = 'D:/Data/Result/5mb.db'#'/home/ivan/dev/Data/Result/884mb.db'
text = 'D:/Data/Wiki/ru/5mb.txt'
dbpath = 'D:/Data/Result/py5mb.db'#'/home/ivan/dev/Data/Result/884.db'


print('Разбиение файла',text,'на слова')
s = splitter.Splitter()
text_tree = s.split_file(text)
print('Токенизация')
t = tokenizer.Tokenizer(dbpath)
t.tokenize(text_tree, 3)

#dbtransform.download_splitters(old_dbpath, db)
#dbtransform.download_words(old_dbpath, db)

engine = nldb.Calculations(dbpath)

print('Вычисление calc_pos_mean(0)')
engine.calc_pos_mean(0)
print('Вычисление calc_pos_mean(1)')
engine.calc_pos_mean(1)
print('Вычисление calc_pos_mean(2)')
engine.calc_pos_mean(2)

print('Вычисление calc_pos_cos_distance(0)')
engine.calc_pos_cos_similarity(0)
print('Вычисление calc_pos_cos_distance(1)')
engine.calc_pos_cos_similarity(1)
print('Вычисление calc_pos_cos_distance(2)')
engine.calc_pos_cos_similarity(2)
