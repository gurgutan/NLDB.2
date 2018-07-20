using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using NLDB;

//Структура БД

namespace NLDB
{
    public partial class Language
    {
        DataContainer data;

        public void Save(string filename)
        {
            data.Save(filename);
        }
       
        public void Load(string filename)
        {
            data.Load(filename);
        }
    }
}
