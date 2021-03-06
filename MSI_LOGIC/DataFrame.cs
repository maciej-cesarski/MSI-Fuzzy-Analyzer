﻿using System;
using System.Linq;

namespace MSI_Logic
{
    public static class Utils
    {
        public static float Neg(float x)
        {
            return 1 - x;
        }

        public static float Imp(float x, float y)
        {
            return (float)Math.Min(1, 1 - x + y);
        }

        public static float TNorm(float x, float y)
        {
            return (float)Math.Max(0, x + y - 1);
        }

        public static float GetDistHamming((float[] u, float[] v, float[] pi) A, (float[] u, float[] v, float[] pi) B)
        {
            int n = A.u.Length;

            float sum = 0;

            for (int i = 0; i < n; i++)
            {
                sum += (float)Math.Abs(A.u[i] - B.u[i]);
                sum += (float)Math.Abs(A.v[i] - B.v[i]);
                sum += (float)Math.Abs(A.pi[i] - B.pi[i]);
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

            for (int i = 0; i < splitted.Length; i++)
            {
                splitted[i] = splitted[i].Replace(',', '.');
                numbers[i] = float.Parse(splitted[i], System.Globalization.CultureInfo.InvariantCulture);
            }

            return numbers;
        }

        private void AssignData(string[] newRows)
        {
            this.data = new float[newRows.Length][];

            for (int i = 0; i < newRows.Length; i++)
            {
                this.data[i] = ConvertStringToFloatArray(newRows[i]);
            }
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
        
        private static (DataFrame df1, DataFrame df2) Get2(DataFrame df)
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

        private static (DataFrame df1, DataFrame df2, DataFrame df3) Get3From2((DataFrame df1, DataFrame df2) data)
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

        public string ToStringRow(int index)
        {
            string s = "";

            for (int i = 0; i < Cols; i++)
            {
                float value = data[index][i];
                s += String.Format("{0:0.00};", value);
            }

            return s.Substring(0, s.Length - 1);
        }

        public string[] ToStringTable()
        {
            string[] lines = new string[Rows];

            for(int i=0; i<Rows; i++)
            {
                lines[i] = ToStringRow(i);
            }

            return lines;
        }

        public static DataFrame GenerateRandom(int rows, int columns)
        {
            DataFrame df = new DataFrame(rows, columns);

            Random RNG = new Random();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    df.data[i][j] = (float)RNG.NextDouble();
                }
            }

            return df;
        }
    }
}
