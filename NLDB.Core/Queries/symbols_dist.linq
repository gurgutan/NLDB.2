<Query Kind="SQL">
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
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Concurrent</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.Data.Common</Namespace>
  <Namespace>System.Data.SQLite</Namespace>
  <Namespace>System.Linq</Namespace>
</Query>

select Words1.Symbol, Words2.Symbol, MatrixA.Sum/MatrixA.Count as avgsum from MatrixA 
Left join Words Words1 ON MatrixA.Row=Words1.Id 
left join Words Words2 ON MatrixA.Column=Words2.Id
order by Words1.Symbol, avgsum desc;