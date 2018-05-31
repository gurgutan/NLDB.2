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

using namespace std;
namespace nldb
{
	///������������ ������ �����. 
	const int MAX_WORD_SIZE = 64;
	///������������ ���������� ����
	const int MAX_WORDS_COUNT = MAXINT32 / MAX_WORD_SIZE;


	class Word
	{
	public:
		int id;
		vector<int> childs;
		vector<float> assurance;

		Word();
		Word(int _id);
		Word(int _id, vector<int>& _childs);
		~Word();

		unsigned int size() const;
		bool Equals(const Word& w);
		unsigned int GetHashCode();

		void ToSparseRow(float* values, int* indexes);

	};

}
