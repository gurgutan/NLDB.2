# Слеповичев И.И. 14.06.2019
# Построитель фраз

import numpy as np
import scipy.sparse as sparse
from sklearn.metrics.pairwise import cosine_similarity


# def build_term(words, consistency_matrix, term_size=8):
#     """
#     words - список кортежей вида (id, confidence), где id - слово, confidence - значение из [0,1]
#     consistency_matrix - csr матрица совместности слов
#     """
#     words = sorted(words, key=lambda w: w[1], reverse=True)
#     term = []
#     result = optimus_prime(term_size)

#     def optimus_prime(size):
#         for i in range(size):
#             optimus_consistency = 0
#             optimus_word = bag_of_words[0]
#             for w in bag_of_words:
#                 if(len(term) == 0):
#                     term = [w[0]]
#                     continue
#                 temp_term = term + [w[0]]
#                 dm = dist_matrix(temp_term)
#                 x = consistency_matrix[term]
#                 y = consi
#                 c = consistency(x, y)
#                 if(c > optimus_consistency):
#                     optimus_consistency = c
#                     optimus_word = w[0]
#             term.append(optimus_word)

#     def dist_matrix(t):
#         rows, columns, sum_data = [], [], []
#         count_row, count_col, count_data = [], [], []
#         for i, row in enumerate(t):
#             for j, column in enumerate(t):
#                 rows.append(row)
#                 columns.append(column)
#                 sum_data.append(j-i)
#                 count_data.append(1)
#         m_sum = sparse.csr_matrix(
#             (sum_data, (rows, columns)), dtype=np.float32)
#         m_count = sparse.csr_matrix(
#             (count_data, (rows, columns)), dtype=np.float32)
#             m_count.power(-1, dtype=np.float32)
#         m_count = m_count.power(-1, dtype=np.float32)
#         m = m_sum.multiply(m_count)
#         return m
#         # cos_xy = cosine_similarity(
#         #                 m[x_first:x_last], m[y_first:y_last], dense_output=False)
