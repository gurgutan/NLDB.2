// NLPL.cpp: определяет экспортированные функции для приложения DLL.
//

#include "stdafx.h"




//extern "C" __declspec(dllexport) float Dist(int* a, int* b, int rows, int columns)
//{
//	// Размерность матрицы соответствий
//	int max_dist = max(rows, columns);
//	int max_dist_sqr = max_dist * max_dist;
//	float result = max_dist_sqr - rows * max_dist;
//	for (int i = 0;i < rows;i++)
//	{
//		int d_min(INT32_MAX);
//		for (int j = 0;j < columns;j++)
//		{
//			int d_temp = (a[i] == b[j]) ? abs(i - j) : max_dist;
//			d_min = min(d_min, d_temp);
//		}
//		result += d_min;
//	}
//	result /= max_dist_sqr;
//	return result;
//}
