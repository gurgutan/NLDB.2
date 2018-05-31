#pragma once
#include<vector>
#include<string>
#include<regex>
#include<algorithm>

using namespace std;
namespace nldb
{
	class Parser
	{
	private:
		string remove_expr;
		string split_expr;

	public:
		Parser();
		Parser(const string& expr);
		~Parser();

		vector<string> Split(const string& text);
		string Normilize(const string& text);

	};
}