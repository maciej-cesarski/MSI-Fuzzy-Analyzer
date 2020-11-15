using System;
using System.Collections.Generic;
using System.Linq;

namespace MSI_draft
{
    public static class Utils
    {
        public static float Neg(float x)
        {
            return 1 - x;
        }

        public static float Imp(float x, float y)
        {
            return MathF.Min(1, 1 - x + y);
        }

        public static float TNorm(float x, float y)
        {
            return MathF.Max(0, x + y - 1);
        }

        public static float GetDistHamming((float[] u, float[] v, float[] pi) A, (float[] u, float[] v, float[] pi) B)
        {
            int n = A.u.Length;

            float sum = 0;

            for (int i = 0; i < n; i++)
            {
                sum += MathF.Abs(A.u[i] - B.u[i]);
                sum += MathF.Abs(A.v[i] - B.v[i]);
                sum += MathF.Abs(A.pi[i] - B.pi[i]);
            }

            sum = sum / (2 * n);

            return sum;
        }
    }

    public class DataFrame
    {
        private readonly char defaultSeparator = ';';
        public float[][] data; //wiersz, kolumna

        public float[] GetRow(int index)
        {
            return data[index];
        }

        public int Rows => this.data.Length;
        public int Cols => this.data[0].Length;

        public DataFrame(int rows, int columns)
        {
            data = new float[rows][];

            for (int i = 0; i < rows; i++)
            {
                data[i] = new float[columns];
            }
        }

        private float[] ConvertStringToFloatArray(string row)
        {
            string[] splitted = row.Split(defaultSeparator);

            float[] numbers = new float[splitted.Length];

            for(int i=0; i<splitted.Length; i++)
            {
                numbers[i] = float.Parse(splitted[i]);
            }

            return numbers;
        }

        private void AssignData(string[] newRows)
        {
            this.data = new float[newRows.Length][];

            for(int i=0; i< newRows.Length; i++)
            {
                this.data[i] = ConvertStringToFloatArray(newRows[i]);
            }
        }

        private void AssignData(float[][] newData)
        {
            this.data = newData;
        }

        public DataFrame(string[] newRows)
        {
            this.AssignData(newRows);
        }

        public static DataFrame LoadFromFile(string filename)
        {
            string[] dataRows = System.IO.File.ReadAllLines(filename);
            return new DataFrame(dataRows);
        }

        public static DataFrame GetProjects()
        {
            DataFrame df = new DataFrame(3, 5);

            df.data[0] = new float[] { 0.9f, 0.6f, 0.8f, 0.2f, 0.8f };
            df.data[1] = new float[] { 0.4f, 0.8f, 1.0f, 0.9f, 0.6f };
            df.data[2] = new float[] { 1.0f, 0.5f, 0.6f, 0.7f, 0.9f };

            return df;
        }



        public static DataFrame GetRevisors()
        {
            DataFrame df = new DataFrame(4, 5);

            df.data[0] = new float[] { 0.9f, 0.0f, 1.0f, 0.6f, 0.3f };
            df.data[1] = new float[] { 0.7f, 0.9f, 0.6f, 1.0f, 0.5f };
            df.data[2] = new float[] { 1.0f, 0.4f, 1.0f, 0.2f, 0.5f };
            df.data[3] = new float[] { 0.6f, 0.7f, 0.8f, 1.0f, 0.6f };

            //df.data[0] = new float[] { 0.2f, 0.5f, 1.0f, 1.0f, 0.4f };
            //df.data[1] = new float[] { 0.4f, 1.0f, 1.0f, 0.4f, 0.6f };
            //df.data[2] = new float[] { 1.0f, 0.6f, 0.6f, 0.8f, 0.7f };
            //df.data[3] = new float[] { 0.6f, 0.3f, 0.9f, 1.0f, 0.0f };
            //df.data[4] = new float[] { 0.2f, 0.8f, 0.7f, 0.5f, 0.1f };
            //df.data[5] = new float[] { 1.0f, 0.5f, 0.7f, 0.6f, 0.9f };

            return df;
        }

        public static (DataFrame df1, DataFrame df2) Get2(DataFrame df)
        {
            DataFrame df1 = new DataFrame(df.Rows, df.Cols);
            DataFrame df2 = new DataFrame(df.Rows, df.Cols);

            for (int i = 0; i < df.Rows; i++)
            {
                df1.data[i] = df.Delta(i);
                df2.data[i] = df.Nabla(i);
            }

            return (df1, df2);
        }

        public static (DataFrame df1, DataFrame df2, DataFrame df3) Get3From2((DataFrame df1, DataFrame df2) data)
        {
            DataFrame df1 = new DataFrame(data.df1.Rows, data.df1.Cols);
            DataFrame df2 = new DataFrame(data.df1.Rows, data.df1.Cols);
            DataFrame df3 = new DataFrame(data.df1.Rows, data.df1.Cols);

            for (int i = 0; i < data.df1.Rows; i++)
            {
                for (int j = 0; j < data.df1.Cols; j++)
                {
                    df1.data[i][j] = data.df1.data[i][j];
                    df2.data[i][j] = 1 - data.df2.data[i][j];
                    df3.data[i][j] = data.df2.data[i][j] - data.df1.data[i][j];
                }
            }

            return (df1, df2, df3);
        }

        public static DataFrame Calculate(DataFrame projects, DataFrame revisors)
        {
            var projects2 = Get2(projects);
            var revisors2 = Get2(revisors);

            var projects3 = Get3From2(projects2);
            var revisors3 = Get3From2(revisors2);

            DataFrame df = new DataFrame(revisors.Rows, projects.Rows);

            for (int i = 0; i < revisors.Rows; i++)
            {
                for (int j = 0; j < projects.Rows; j++)
                {
                    df.data[i][j] = Utils.GetDistHamming(
                        (revisors3.df1.GetRow(i), revisors3.df2.GetRow(i), revisors3.df3.GetRow(i)),
                        (projects3.df1.GetRow(j), projects3.df2.GetRow(j), projects3.df3.GetRow(j))
                    );
                }
            }

            return df;
        }

        //czyli to jest źle?
        //powinno sie zwracac ARGUMENT dla którego to jest najmniejsze XD trzeba zwrócić najmniejszego Y/X

        public float Oper3(int rowIndex, int colIndex, float value)
        {
            float[] results = new float[Cols];

            for (int i = 0; i < Cols; i++)
            {
                results[i] = Utils.Imp(data[rowIndex][i], value);
            }

            int minIndex = 0;

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i] < results[minIndex]) minIndex = i;
            }

            return results[minIndex];
        }

        //x to jest konkretna wartość z tabelki data[rowIndex][colIndex]

        public float Oper4(int rowIndex, int colIndex, float value)
        {
            float[] results = new float[Cols];

            for (int i = 0; i < Cols; i++)
            {
                results[i] = Utils.TNorm(Utils.Neg(value), Utils.Neg(data[rowIndex][i]));
            }

            int maxIndex = 0;

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i] > results[maxIndex]) maxIndex = i;
            }

            return results[maxIndex];
        }


        //teraz to samo, ale przeszukujemy kolumnę
        public float Oper3_Inv(int rowIndex, int colIndex, float value)
        {
            float[] results = new float[Rows];

            for (int i = 0; i < Rows; i++)
            {
                results[i] = Utils.Imp(data[i][colIndex], value);
            }

            int minIndex = 0;

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i] < results[minIndex]) minIndex = i;
            }

            return results[minIndex];
        }

        //x to jest konkretna wartość z tabelki data[rowIndex][colIndex]

        public float Oper4_Inv(int rowIndex, int colIndex, float value)
        {
            float[] results = new float[Rows];

            for (int i = 0; i < Rows; i++)
            {
                results[i] = Utils.TNorm(Utils.Neg(value), Utils.Neg(data[i][colIndex]));
            }

            int maxIndex = 0;

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i] > results[maxIndex]) maxIndex = i;
            }

            return results[maxIndex];
        }

        public float DeltaHelper(float RXY, float BY)
        {
            return Utils.TNorm(Utils.Neg(RXY), Utils.Neg(BY));
        }

        public float NablaHelper(float RXY, float BY)
        {
            return Utils.Imp(BY, RXY);
        }

        public float[] Nabla(int rowIndex) //zwraca wektor nabli dla wiersza o numerze rowIndex
        {
            float[] vectorR = new float[Rows]; //wektor posredni

            for (int i = 0; i < Rows; i++)
            {
                float[] valuesR = new float[Cols];

                for (int j = 0; j < Cols; j++)
                {
                    valuesR[j] = NablaHelper(data[i][j], data[rowIndex][j]);
                }

                vectorR[i] = valuesR.Min();
            }

            float[] returnVector = new float[Cols];

            for (int i = 0; i < Cols; i++)
            {
                float[] valuesR = new float[Rows];

                for (int j = 0; j < Rows; j++)
                {
                    valuesR[j] = NablaHelper(data[j][i], vectorR[j]);
                }

                returnVector[i] = valuesR.Min();
            }

            return returnVector;
        }

        public float[] Delta(int rowIndex)
        {
            float[] vectorR = new float[Rows]; //wektor posredni

            for (int i = 0; i < Rows; i++)
            {
                float[] valuesR = new float[Cols];

                for (int j = 0; j < Cols; j++)
                {
                    valuesR[j] = DeltaHelper(data[rowIndex][j], data[i][j]);
                }

                vectorR[i] = valuesR.Max();
            }

            float[] returnVector = new float[Cols];

            for (int i = 0; i < Cols; i++)
            {
                float[] valuesR = new float[Rows];

                for (int j = 0; j < Rows; j++)
                {
                    valuesR[j] = DeltaHelper(vectorR[j], data[j][i]);
                }

                returnVector[i] = valuesR.Max();
            }

            return returnVector;
        }        

        public void Print()
        {
            Console.WriteLine();
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    Console.Write(String.Format("{0:0.0} ", Math.Abs(data[i][j])));
                }
                Console.WriteLine();
            }
        }

        public static void Print2(DataFrame df1, DataFrame df2)
        {
            Console.WriteLine();
            for (int i = 0; i < df1.Rows; i++)
            {
                for (int j = 0; j < df1.Cols; j++)
                {
                    Console.Write(String.Format("({0:0.0}, ", Math.Abs(df1.data[i][j])));
                    Console.Write(String.Format("{0:0.0}) ", Math.Abs(df2.data[i][j])));
                }
                Console.WriteLine();
            }
        }

        public static void Print3(DataFrame df1, DataFrame df2, DataFrame df3)
        {
            Console.WriteLine();
            for (int i = 0; i < df1.Rows; i++)
            {
                for (int j = 0; j < df1.Cols; j++)
                {
                    Console.Write(String.Format("({0:0.0}, ", Math.Abs(df1.data[i][j])));
                    Console.Write(String.Format("{0:0.0}, ", Math.Abs(df2.data[i][j])));
                    Console.Write(String.Format("{0:0.0}) ", Math.Abs(df3.data[i][j])));
                }
                Console.WriteLine();
            }
        }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            var dfP = DataFrame.GetProjects();
            var dfR = DataFrame.GetRevisors();

            dfP.Print();
            dfR.Print();

            var dfP2 = DataFrame.Get2(dfP);
            DataFrame.Print2(dfP2.df1, dfP2.df2);

            var dfR2 = DataFrame.Get2(dfR);
            DataFrame.Print2(dfR2.df1, dfR2.df2);

            var dfR3 = DataFrame.Get3From2(dfR2);
            var dfP3 = DataFrame.Get3From2(dfP2);

            DataFrame.Print3(dfP3.df1, dfP3.df2, dfP3.df3);
            DataFrame.Print3(dfR3.df1, dfR3.df2, dfR3.df3);

            var distances = DataFrame.Calculate(dfP, dfR);

            distances.Print();
        }
    }
}
