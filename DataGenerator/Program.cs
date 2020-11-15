using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSI_Logic;

namespace DataGenerator
{
    //Simple program for generating data input. Hardcode columns and rows to generate certain data matrix.
    //Generated data will be printed on console.
    class Program
    {
        static void Main(string[] args)
        {
            int columns = 5;
            int rows = 35;

            DataFrame df = new DataFrame(rows, columns);

            Random RNG = new Random();

            for(int i=0; i< rows; i++)
            {
                for(int j=0; j<columns; j++)
                {
                    df.data[i][j] = (float)RNG.NextDouble();
                }
            }

            var data = df.ToStringTable();

            foreach(var v in data)
            {
                Console.WriteLine(v);
            }

            Console.ReadKey();
        }
    }
}
