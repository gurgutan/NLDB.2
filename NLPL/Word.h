//***********************************************************************************
// ������ ��������� ������ Word ��� ������ �� �������
// ������� �������
// ����� - ����� ��������, �������������� ������ ������� � ��������� int32. ����� 
// ����� ����������� ���������� ������������� � ����� ��������������� �������� ����,
// ��� ������������. ����� ���������� ������� Word.
//
// ���������� �� ����������
// ������ ������ ������� ���������� �� ����������:
// 
//***********************************************************************************


#pragma once
#include<algorithm>
#include<vector>
#include<tuple>


namespace nldb
{
	///������������ ������ �����. 
	const int MAX_WORD_SIZE = 64;
	///������������ ���������� ����
	const int MAX_WORDS_COUNT = MAXINT32 / MAX_WORD_SIZE;


	typedef std::tuple<std::vector<float>, std::vector<int>> SparseRow;


	class Word
	{
	public:
		int id;
		std::vector<int> childs;
		std::vector<float> assurance;

		Word();
		Word(int _id);
		Word(int _id, std::vector<int> _childs);
		~Word();

		int Size();
		bool Equals(const Word& w);
		int GetHashCode();

		SparseRow ToSparseRow();

	};

}
