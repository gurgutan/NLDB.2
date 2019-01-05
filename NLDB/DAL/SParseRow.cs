using System.Collections.Generic;

namespace NLDB.DAL
{
    public class SparseRow<T> : Dictionary<int, T>
    {
    }

    public class SparseMatrix<T>: Dictionary<int, SparseRow<T>>
    {

    }
}
