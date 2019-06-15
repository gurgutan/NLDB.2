import scipy as sp
import scipy.sparse as sparse


rows = [2**i for i in range(5)]
cols = [2**i for i in range(5)]
data = [1 for i in range(5)]

m1 = sparse.csr_matrix((data, (rows, cols)), shape=(2**10+1, 2**10+1))
ind_rows = [[1, 2, 4], [1, 2, 4]]
ind_cols = [[1, 2, 3], [1, 2, 4]]
print(m1[ind_rows, ind_cols].toarray())
