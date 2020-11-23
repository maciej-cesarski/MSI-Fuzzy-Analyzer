using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        static void PrintRandom(int rows, int columns)
        {
            DataFrame df = DataFrame.GenerateRandom(rows, columns);
            var data = df.ToStringTable();

            foreach (var v in data)
            {
                Console.WriteLine(v);
            }
        }

        static long PerformanceTest(int columns, int rows1, int rows2)
        {
            DataFrame df1 = DataFrame.GenerateRandom(rows1, columns);
            DataFrame df2 = DataFrame.GenerateRandom(rows2, columns);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            DataFrame results = DataFrame.Calculate(df1, df2);
            stopwatch.Stop();

            return stopwatch.ElapsedMilliseconds;
        }

        static void PerformTest()
        {
            int[] sizes = { 16, 32, 64, 128, 256, 512 };

            foreach (var v in sizes)
            {
                Console.WriteLine($"Calculating for size: {v}.");
                long milliseconds = PerformanceTest(v, v, v);
                Console.WriteLine($"Result: {milliseconds}ms.");
                Console.WriteLine();
            }
        }

        static void Main(string[] args)
        {
            int columns = 5;
            int rows = 35;

            //PrintRandom(rows: 5, columns: 35);
            PerformTest();

            Console.ReadKey();
        }
    }
}
