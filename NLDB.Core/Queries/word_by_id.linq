<Query Kind="Program">
  <Connection>
    <ID>0889947e-f13f-490f-b1c1-63789e168985</ID>
    <Persist>true</Persist>
    <Driver Assembly="IQDriver" PublicKeyToken="5b59726538a49684">IQDriver.IQDriver</Driver>
    <Provider>System.Data.SQLite</Provider>
    <CustomCxString>Data Source=D:\Data\Result\5mb.db;FailIfMissing=True</CustomCxString>
    <AttachFileName>D:\Data\Result\5mb.db</AttachFileName>
    <DriverData>
      <StripUnderscores>false</StripUnderscores>
      <QuietenAllCaps>false</QuietenAllCaps>
    </DriverData>
  </Connection>
</Query>

void Main()
{
	var s = MatrixBs
	.Take(1000)
	.Where(v=>v.Rank==0 /*&& Math.Abs(v.Row-v.Column)>8000*/ && v.Similarity*5>4)
	.OrderByDescending(v=>v.Similarity)
	.Take(100)
	.Select(v=>new { v.Row, слово1=StringView(v.Row), v.Column, слово2=StringView(v.Column), совместность=v.Similarity })
	.ToList();
	s.Dump();
}

string StringView(int id)
{
	return string.Join("", 
		Words
		.First(w=>w.Id==id)
		.Childs
		.Split(',')
		.Select(c=>int.Parse(c))
		.Select(ci=>
			{ 
			var w = Words.First(cw=>cw.Id==ci);
			if(w.Rank==0) 
				return w.Symbol;
			else
				return " "+StringView(w.Id);
			}));
}