using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace lab9vu
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        List<double> outcomes; 
        List<int> statistics; // frequency
        List<double> prob; 
        List<double> probEx;
        double theoreticalMean, theoreticalVariance;
        double sampleMean, sampleVariance;
        double chiSquareStat;
        Random rnd = new Random();

        class ChiCriteria
        {
            public double Value;
            public double Alpha;
            public ChiCriteria(double value, double alpha)
            {
                this.Value = value;
                this.Alpha = alpha;
            }
        }

        List<ChiCriteria> chiCriteria = new List<ChiCriteria>
        {
            new ChiCriteria(9.488, 0.05),
            new ChiCriteria(13.277, 0.01),
            new ChiCriteria(18.467, 0.001)
        };

        private int GenerateEvent()
        {
            double a = rnd.NextDouble();
            double A = a;
            int k = 1;

            while (k <= prob.Count)
            {
                A -= prob[k - 1];
                if (A <= 0)
                {
                    return k - 1;
                }
                k++;
            }
            return prob.Count - 1;
        }

        private void HandleTrial()
        {
            outcomes.Clear();
            statistics = new List<int> { 0, 0, 0, 0, 0 };
            probEx.Clear();

            int N = int.Parse(textBox6.Text);
            for (int i = 0; i < N; i++)
            {
                int eventIndex = GenerateEvent();
                outcomes.Add(eventIndex + 1); // 1 - 5
                statistics[eventIndex]++;
            }

            for (int i = 0; i < 5; i++)
            {
                probEx.Add((double)statistics[i] / N);
            }
        }

        private void CalculateTheoreticalStats()
        {
            theoreticalMean = 0;
            double ex2 = 0;
            for (int i = 0; i < prob.Count; i++)
            {
                double x = i + 1; // Outcomes are 1 to 5
                theoreticalMean += x * prob[i];
                ex2 += (x * x) * prob[i];
            }
            theoreticalVariance = ex2 - (theoreticalMean * theoreticalMean);
        }

        private void CalculateSampleStats()
        {
            sampleMean = outcomes.Average();

            sampleVariance = 0;
            foreach (double x in outcomes)
            {
                sampleVariance += (x - sampleMean) * (x - sampleMean);
            }
            sampleVariance /= (outcomes.Count - 1);
        }

        private double CalculateChiSquare()
        {
            double chiSquare = 0;
            int N = int.Parse(textBox6.Text);
            for (int i = 0; i < prob.Count; i++)
            {
                double expected = prob[i] * N; // Expected frequency
                double observed = statistics[i]; // Observed frequency
                if (expected > 0)
                {
                    chiSquare += ((observed - expected) * (observed - expected)) / expected;
                }
            }
            return chiSquare;
        }

        private string ChiSquareTest(double chiSquare)
        {
            string result = $"Chi-Square: {chiSquare:F2}\n";
            bool passedAny = false;
            for (int i = 0; i < chiCriteria.Count; i++)
            {
                if (chiSquare <= chiCriteria[i].Value)
                {
                    result += $"<= {chiCriteria[i].Value:F2} (α={chiCriteria[i].Alpha}), Принимается\n";
                    passedAny = true;
                }
                else
                {
                    result += $"> {chiCriteria[i].Value:F2} (α={chiCriteria[i].Alpha}), Отклоняется\n";
                }
            }
            if (!passedAny)
            {
                result += "Отклоняется на всех уровнях значимости";
            }
            return result;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            outcomes = new List<double>();
            statistics = new List<int> { 0, 0, 0, 0, 0 };
            prob = new List<double>();
            probEx = new List<double>();

            // Parse number of trials
            int N;
            if (!int.TryParse(textBox6.Text, out N) || N <= 0)
            {
                MessageBox.Show("Пожалуйста, введите корректное число попыток.");
                return;
            }

            // Parse probabilities
            prob.Add(double.Parse(textBox1.Text));
            prob.Add(double.Parse(textBox2.Text));
            prob.Add(double.Parse(textBox3.Text));
            prob.Add(double.Parse(textBox4.Text));
            prob.Add(1 - (prob[0] + prob[1] + prob[2] + prob[3]));
            textBox5.Text = prob[4].ToString("F3");

            double sum = prob.Sum();
            if (prob[4] < 0 || Math.Abs(sum - 1) > 0.001)
            {
                MessageBox.Show("Сумма вероятностей должна быть равна 1.");
                return;
            }

            HandleTrial();

            // Calculate theoretical and sample statistics
            CalculateTheoreticalStats();
            CalculateSampleStats();

            // Calculate relative errors
            double meanError = Math.Abs(sampleMean - theoreticalMean) / theoreticalMean * 100;
            double varianceError = Math.Abs(sampleVariance - theoreticalVariance) / theoreticalVariance * 100;

            // Calculate chi-square statistic
            double chiSquare = CalculateChiSquare();

            // Update textboxes
            textBox7.Text = theoreticalMean.ToString("F3"); 
            textBox8.Text = theoreticalVariance.ToString("F3"); 
            textBox9.Text = sampleMean.ToString("F3"); 
            textBox10.Text = sampleVariance.ToString("F3"); 
            textBox11.Text = meanError.ToString("F2") + "%"; 
            textBox12.Text = varianceError.ToString("F2") + "%"; 
            textBox5.Text = ChiSquareTest(chiSquare); 

            // Update chart
            chart1.Series[0].Points.Clear();
            chart1.Series[0].Name = "Эмпирические";
            if (chart1.Series.Count == 1)
            {
                chart1.Series.Add(new Series("Теоретические"));
                chart1.Series[1].ChartType = SeriesChartType.Column;
                chart1.Series[1].Color = Color.Red;
            }
            chart1.Series[1].Points.Clear();

            for (int i = 0; i < 5; i++)
            {
                chart1.Series[0].Points.AddXY(i + 1, probEx[i]);
                chart1.Series[0].Points[i].Label = probEx[i].ToString("F3");
                chart1.Series[1].Points.AddXY(i + 1, prob[i]);
                chart1.Series[1].Points[i].Label = prob[i].ToString("F3");
            }
        }
    }
}
