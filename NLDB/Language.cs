using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLDB
{
    public class Language
    {
        private readonly int bufferSize = 1 << 25;
        private readonly string Name;
        public int Rank;

        public List<Lexicon> Lexicons = new List<Lexicon>();

        public Language(string name, int rank, string[] splitters)
        {
            this.Name = name;
            this.Init(rank, splitters);
        }

        public Lexicon this[int r]
        {
            get
            {
                return this.Lexicons[r];
            }
        }

        public void CreateFromTextFile(string filename)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException("Файл не найден");
            using (StreamReader file = File.OpenText(filename))
            {
                char[] buffer = new char[this.bufferSize];
                int count = this.bufferSize;
                int total = 0;
                while (count == this.bufferSize)
                {
                    count = file.ReadBlock(buffer, 0, this.bufferSize);
                    total += count;
                    string text = new string(buffer);
                    for (int i = 0; i <= this.Rank; i++)
                    {
                        this.Lexicons[i].TryAddMany(text);
                    }
                    Console.WriteLine($"Считано {total} байт");
                }
            }
        }

        private void Init(int rank, string[] splitters)
        {
            for (int i = 0; i <= rank; i++)
                this.Lexicons.Add(
                    new Lexicon(splitters[i], i > 0 ? this.Lexicons[i - 1] : null)
                    );
        }
    }
}
