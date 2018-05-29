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


namespace nldb
{
	///Максимальный размер слова. 
	const int MAX_WORD_SIZE = 64;
	///Максимальное количество слов
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
