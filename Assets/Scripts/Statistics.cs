using Accord.Math.Optimization.Losses;
using UnityEngine;
using System;
using Accord.Statistics.Models;
using Accord.Math;
using Accord.Statistics.Models.Regression.Linear;
using Accord.Statistics;
using System.Linq;
public class Statistics : MonoBehaviour
{
    public static Stats checkValues(float[][] values, string[] names)
    {
        Stats toReturn = new Stats();
        double[][] X = new double[values.Length][];
        for(int i=0; i<values.Length; i++)
        {
            X[i] = Array.ConvertAll(values[i], item=>(double)item);
        }
        double[][] data = X.Transpose();

        toReturn.VIFS = new double[names.Length];
        for(int i=0; i<data.Columns(); i++)
        {
            double vif = ComputeVIF(data, i);
            toReturn.VIFS[i] = vif;
        }

        double[][] correlationMatrix = Measures.Correlation(data);

        string nn = " ".PadRight(15, ' ');
        foreach(string name in names)
        {
            nn += name.PadRight(15, ' ');
        }
        string matrix = nn + "\n";
        for(int i = 0; i < correlationMatrix.Length; i++)
        {
            string str = names[i].PadRight(15);
            for (int j = 0; j < correlationMatrix[i].Length; j++)
            {
                str += correlationMatrix[i][j].ToString("0.000").PadRight(15);
            }
            matrix += str + "\n";

        }
        toReturn.correlationMatrix = correlationMatrix;
        return toReturn;
    }

    public class Stats
    {
        public double[] VIFS;
        public double[][] correlationMatrix;
    }
    public static double ComputeVIF(double[][] data, int columnIndex)
    {
        int n = data.Length;    // Number of samples
        int k = data[0].Length; // Number of variables

        // Extract dependent variable Y (selected column)
        double[] Y = data.GetColumn(columnIndex);

        // Extract independent variables X (all other columns)
        double[][] X = data.RemoveColumn(columnIndex);

        // Create and fit linear regression model
        var ols = new OrdinaryLeastSquares();
        var regression = ols.Learn(X, Y);
        // Get R-squared value
        double r2 = regression.CoefficientOfDetermination(X, Y);

        // Compute VIF
        return 1 / (1 - r2);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
