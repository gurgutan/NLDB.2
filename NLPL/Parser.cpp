#include "stdafx.h"
#include "Parser.h"


using namespace nldb;

Parser::Parser()
{
	this->split_expr = "\\s+";
	this->remove_expr = "[^à-ÿÀ-ß¸¨a-zA-Z0-9\\s\\t\\r\\n\\!\\?\\,\\;\\:\\%\\$\\#\\@\\*\\+\\-\\&\\^\\(\\)\\[\\]\\{\\}\\=\\<\\>]";

}

Parser::~Parser()
{
}

Parser::Parser(const string& expr)
	: split_expr(expr)
{ }

vector<string> Parser::Split(const string& text)
{
	vector<string> substr;
	regex rgx(split_expr);
	sregex_token_iterator iter(text.begin(), text.end(), rgx, -1);
	sregex_token_iterator end;
	for (; iter != end; ++iter)
		substr.push_back(*iter);
	return substr;
}

string Parser::Normilize(const string& text)
{
	string result;
	transform(text.begin(), text.end(), result.begin(), ::tolower);
	return result;
}
