import dbtransform
import dbcalc

dbpath = '/home/ivan/dev/Data/Result/884mb.db'
dst_dbpath = '/home/ivan/dev/Data/Result/884.db'

dbtransform.convert_words(dbpath, dst_dbpath)
dbtransform.test_words(dst_dbpath)
dbcalc.calc_mean(dst_dbpath, 0)
dbcalc.calc_mean(dst_dbpath, 1)
dbcalc.calc_mean(dst_dbpath, 2)