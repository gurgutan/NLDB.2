#include "stdafx.h"
#include "Word.h"

using namespace nldb;

nldb::Word::Word()
{
}

nldb::Word::Word(int _id)
{
	this->id = _id;
}

nldb::Word::Word(int _id, std::vector<int> _childs)
{
	this->id = _id;
	this->childs = _childs;
	this->assurance.assign(_childs.size, 1);
}

nldb::Word::~Word()
{
	this->childs.clear();
	this->assurance.clear();
}

int nldb::Word::Size()
{
	return childs.size;
}

/// Возвращает разреженный вектор-строку в формате 2 вектора - ненулевые значения и номера колонок ненулевых значений
nldb::SparseRow nldb::Word::ToSparseRow()
{
	SparseRow result;
	std::vector<int> values;
	std::vector<int> cols;

	for (int i = 0; i < this->childs.size; ++i)
	{
		int index = MAX_WORD_SIZE*this->childs[i] + i % MAX_WORD_SIZE;
		values.push_back(this->assurance[i]);
		cols.push_back(index);
	}
	result = std::make_tuple(values, cols);
	return result;
}

/// Сопоставление слов по буквам или по id. Слова считаются эквивалентными, если соблюдается хотя-бы одно из условий:
///  - совпадают id (быстрое сопоставление)
///  - длины слов равны и совпадают все буквы в соответствующих позициях
bool nldb::Word::Equals(const Word& w)
{
	if (w.id == this->id) return true;
	if (w.childs.size != this->Size()) return false;
	if (w.childs.size == 0) return w.id == this->id;
	for (int i = 0;i < this->childs.size;++i)
		if (w.childs[i] != this->childs[i]) return false;
	return true;
}

/// Вычисление хэш-функции на наборе букв слова. Применяется линейный конгруэнтный метод для набора символов:
/// H(c[i+1]) <- H(c[i]) * 1664525 + 1013904223
/// c[i] - i-й символ слова, i=0...n, где n - длина слова
int nldb::Word::GetHashCode()
{
	if (this->childs.size == 0)return this->id;
	int hash = 0;
	for (int i = 0; i < this->childs.size; i++)
	{
		hash += this->childs[i] + 1013904223;
		hash *= 1664525;
	}
	return hash;
}


