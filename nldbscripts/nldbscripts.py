import dbtransform
import nldb

old_dbpath = 'D:/Data/Result/5mb.db'#'/home/ivan/dev/Data/Result/884mb.db'
db = 'D:/Data/Result/5mb_clone.db'#'/home/ivan/dev/Data/Result/884.db'

dbtransform.download_splitters(old_dbpath, db)
dbtransform.download_words(old_dbpath, db)

engine = nldb.Calculations(db)

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
