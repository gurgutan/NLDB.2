//***********************************************************************************
// Модуль заголовка класса Word для работы со словами
// Базовые понятия
// Слово - набор символов, представленных целыми числами в диапазоне int32. Слово 
// имеет собственный уникальный идентификатор и набор идентификаторов дочерних слов,
// его составляющих. Слово релизовано классом Word.
//
// Соглашение об именовании
// Методы класса следуют соглашению об именовании:
// 
//***********************************************************************************


#pragma once
#include<algorithm>
#include<vector>
#include<tuple>

using namespace std;
namespace nldb
{
	///Максимальный размер слова. 
	const int MAX_WORD_SIZE = 64;
	///Максимальное количество слов
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
