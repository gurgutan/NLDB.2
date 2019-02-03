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
	.Where(v=>v.Rank==2 && Math.Abs(v.Row-v.Column)>0 && v.Similarity>1/2)
	.OrderByDescending(v=>v.Similarity)
	.Take(1000)
	.Select(v=>new { v.Row, слово1=StringView(v.Row), v.Column, слово2=StringView(v.Column), совместность=v.Similarity })
	.ToList();
	s.Dump();
}

string StringView(int id)
{
	var wr = Words.First(w=>w.Id==id);
	if(wr.Rank==0) return wr.Symbol;
	return string.Join("", 
		Words
		.First(w=>w.Id==id)
		.Childs
		.Split(',')
		.Select(c=>{ int s=0; if(int.TryParse(c,out s)) return s; else return 0; })
		.Select(ci=>
			{ 
			var w = Words.First(cw=>cw.Id==ci);
			if(w.Rank==0) 
				return w.Symbol;
			else
				return " "+StringView(w.Id);
			}));
}