select W1.Id, W1.Symbol, W2.Id, W2.Symbol, B.Similarity, B.Rank from MatrixB AS B
inner join Words W1 ON B.Row=W1.Id and W1.Rank=B.Rank
inner join Words W2 ON B.Column==W2.Id and W2.Rank=B.Rank
where B.Rank=2 and ABS(B.Row-B.Column)>10000 and B.Similarity > 0.
order by B.Similarity desc
limit 1000;