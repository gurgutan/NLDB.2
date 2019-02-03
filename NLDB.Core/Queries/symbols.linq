<Query Kind="SQL">
  <Connection>
    <ID>18fe903e-7abc-4322-bec1-63b4ae725e8b</ID>
    <Persist>true</Persist>
    <Driver Assembly="IQDriver" PublicKeyToken="5b59726538a49684">IQDriver.IQDriver</Driver>
    <Provider>System.Data.SQLite</Provider>
    <CustomCxString>Data Source=D:\Data\Result\1mb.db;FailIfMissing=True</CustomCxString>
    <AttachFileName>D:\Data\Result\1mb.db</AttachFileName>
    <DriverData>
      <StripUnderscores>false</StripUnderscores>
      <QuietenAllCaps>false</QuietenAllCaps>
    </DriverData>
  </Connection>
</Query>

--select * from Words where Words.Rank=0;
select W1.Id, W1.Symbol, W2.Id, W2.Symbol, B.Similarity, B.Rank from MatrixB AS B
inner join Words W1 ON B.Row=W1.Id and W1.Rank=B.Rank
inner join Words W2 ON B.Column==W2.Id and W2.Rank=B.Rank
where B.Rank=0 and ABS(B.Row-B.Column)>0 and B.Similarity > 0
order by B.Similarity desc
limit 1000;