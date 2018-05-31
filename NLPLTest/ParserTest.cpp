#include "stdafx.h"
#include "CppUnitTest.h"
#include<string>
#include<vector>
#include "..\NLPL\Parser.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace std;

namespace nldb
{
	TEST_CLASS(ParserTest)
	{
	public:

		TEST_METHOD(Split)
		{
			string txt = "Привет всем!";
			string expr = "\\s+";
			Parser parser(expr);
			vector<string> substr = parser.Split(txt);
		}

	};
}