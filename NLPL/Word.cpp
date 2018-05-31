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

nldb::Word::Word(int _id, vector<int>& _childs)
{
	this->id = _id;
	this->childs = _childs;
	this->assurance.assign(_childs.size(), 1);
}

nldb::Word::~Word()
{
	this->childs.clear();
	this->assurance.clear();
}

unsigned int nldb::Word::size() const
{
	return this->childs.size();
}

/// ���������� ����������� ������-������ � ������� 2 ������� - ��������� �������� � ������ ������� ��������� ��������
void nldb::Word::ToSparseRow(float* values, int* indexes)
{
	values = new float[this->childs.size()];
	indexes = new int[this->childs.size()];
	for (UINT32 i = 0; i < this->childs.size(); ++i)
	{
		values[i] = this->assurance[i];
		indexes[i] = MAX_WORD_SIZE*this->childs[i] + i % MAX_WORD_SIZE;
	}
}

/// ������������� ���� �� ������ ��� �� id. ����� ��������� ��������������, ���� ����������� ����-�� ���� �� �������:
///  - ��������� id (������� �������������)
///  - ����� ���� ����� � ��������� ��� ����� � ��������������� ��������
bool nldb::Word::Equals(const Word& w)
{
	if (w.id == this->id) return true;
	if (w.size() != this->size()) return false;
	if (w.size() == 0) return w.id == this->id;
	for (UINT32 i = 0;i < this->size();++i)
		if (w.childs[i] != this->childs[i]) return false;
	return true;
}

/// ���������� ���-������� �� ������ ���� �����. ����������� �������� ������������ ����� ��� ������ ��������:
/// H(c[i+1]) <- H(c[i]) * 1664525 + 1013904223
/// c[i] - i-� ������ �����, i=0...n, ��� n - ����� �����
unsigned int nldb::Word::GetHashCode()
{
	if (this->childs.size() == 0)return this->id;
	unsigned int hash = 0;
	for (UINT32 i = 0; i < this->childs.size(); i++)
	{
		hash += this->childs[i] + 1013904223;
		hash *= 1664525;
	}
	return hash;
}


