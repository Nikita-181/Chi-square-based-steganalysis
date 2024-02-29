using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Drawing.Imaging;
using ScottPlot;

namespace Chi_square_based_steganalysis
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //Оригинальная статья:
            //https://web.archive.org/web/20151123010933/http://users.ece.cmu.edu/~adrian/487-s06/westfeld-pfitzmann-ihw99.pdf
            //Хорошее пояснение на русском
            //https://github.com/desudesutalk/lsbtools/blob/master/docs/lsb_rus.md
        }
        private void buttonAtack_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string FilePath = openFileDialog.FileName;
                //Отображаем выбранный файл
                BitmapImage inputBitmap = new BitmapImage();
                inputBitmap.BeginInit();
                inputBitmap.UriSource = new Uri(FilePath, UriKind.Absolute);
                inputBitmap.EndInit();
                imageBox.Source = inputBitmap;
                //Результат стеганоанализа
                var result = chiSquareAttackTopToBottom(new Bitmap(FilePath));
                //заполняем график
                double[] Ox = new double[result.Length];
                for (int i = 0; i < Ox.Length; i++)
                {
                    Ox[i] = i;
                }
                ProbabillityPlot.Plot.Clear();
                ProbabillityPlot.Plot.AddScatter(Ox, result);
                ProbabillityPlot.Plot.YAxis.Label("Вероятность встраивания");
                ProbabillityPlot.Plot.XAxis.Label("Количество строк");
                ProbabillityPlot.Refresh();
            }
        }
        double[] chiSquareAttackTopToBottom(Bitmap image)
        {
            int imgWidth = image.Width;
            int imgHeight = image.Height;
            //получаем каналы 
            var channels = BitmapToByteRgb(image);

            float[] cropHist = new float[256]; //гистограмма
            for (int j = 0; j < 256; j++)//ставим единички, чтобы не делить на 0
            {
                cropHist[j] = 1;
            }

            double[] pVals = new double[imgHeight]; //массив значений вероятности

            //Анализ идет построчно, увеличивая колическво анализируемых строк на 1 за цикл
            for (int i = 0; i < imgHeight; i++)
            {
                //Высчитываем суммарную гистограмму по 3м каналам,
                //добавляя данные в предыдущий набор 
                for (int w = 0; w < imgWidth; w++)
                {
                    cropHist[channels[i, w, 0]] += 1;
                    cropHist[channels[i, w, 1]] += 1;
                    cropHist[channels[i, w, 2]] += 1;
                }

                //высчитывам наблюдаемую и ожидаемую частоту
                //expected и observed делят длину исследуемой последовательности пополам
                //так как у нас 256 цветов, то создаем 2 массива по 128 элементов
                double[] expected = new double[128];
                int[] observed = new int[128];
                for (int k = 0; k < cropHist.Length / 2; k++)
                {
                    expected[k] = (cropHist[2 * k] + cropHist[2 * k + 1]) / 2;
                    observed[k] = Convert.ToInt32(cropHist[2 * k + 1]);
                }

                //Если количество пикселей цвета 2k и 2k+1 будет сильно различаться,
                //то различаться будут измеренная частота и теоретически ожидаемая,
                //что нормально для незаполненного стегоконтейнера.

                //Считаем вероятность "p" исследуемой посследовательности
                var chi = chiSquare(expected, observed);//вычисляем Хи-квадрат
                int df = observed.Length - 1;//степени свободы
                var pVal = ChiSquarePval(chi, df);
                pVals[i] = pVal;
            }
            return pVals;
        }

        //https://learn.microsoft.com/en-us/archive/msdn-magazine/2017/march/test-run-chi-squared-goodness-of-fit-using-csharp
        public static double ChiSquarePval(double x, int df)
        {
            // x = a computed chi-square value.
            // df = degrees of freedom.
            // output = prob. x value occurred by chance.
            // ACM 299.
            if (x <= 0.0 || df < 1)
                throw new Exception("Bad arg in ChiSquarePval()");
            double a = 0.0; // 299 variable names
            double y = 0.0;
            double s = 0.0;
            double z = 0.0;
            double ee = 0.0; // change from e
            double c;
            bool even; // Is df even?
            a = 0.5 * x;
            if (df % 2 == 0) even = true; else even = false;
            if (df > 1) y = Exp(-a); // ACM update remark (4)
            if (even == true) s = y;
            else s = 2.0 * Gauss(-Math.Sqrt(x));
            if (df > 2)
            {
                x = 0.5 * (df - 1.0);
                if (even == true) z = 1.0; else z = 0.5;
                if (a > 40.0) // ACM remark (5)
                {
                    if (even == true) ee = 0.0;
                    else ee = 0.5723649429247000870717135;
                    c = Math.Log(a); // log base e
                    while (z <= x)
                    {
                        ee = Math.Log(z) + ee;
                        s = s + Exp(c * z - a - ee); // ACM update remark (6)
                        z = z + 1.0;
                    }
                    return s;
                } // a > 40.0
                else
                {
                    if (even == true) ee = 1.0;
                    else
                        ee = 0.5641895835477562869480795 / Math.Sqrt(a);
                    c = 0.0;
                    while (z <= x)
                    {
                        ee = ee * (a / z); // ACM update remark (7)
                        c = c + ee;
                        z = z + 1.0;
                    }
                    return c * y + s;
                }
            } // df > 2
            else
            {
                return s;
            }
        } // ChiSquarePval()
        private static double Exp(double x)
        {
            if (x < -40.0) // ACM update remark (8)
                return 0.0;
            else
                return Math.Exp(x);
        }
        public static double Gauss(double z)
        {
            // input = z-value (-inf to +inf)
            // output = p under Normal curve from -inf to z
            // ACM Algorithm #209
            double y; // 209 scratch variable
            double p; // result. called ‘z’ in 209
            double w; // 209 scratch variable
            if (z == 0.0)
                p = 0.0;
            else
            {
                y = Math.Abs(z) / 2;
                if (y >= 3.0)
                {
                    p = 1.0;
                }
                else if (y < 1.0)
                {
                    w = y * y;
                    p = ((((((((0.000124818987 * w
                      - 0.001075204047) * w + 0.005198775019) * w
                      - 0.019198292004) * w + 0.059054035642) * w
                      - 0.151968751364) * w + 0.319152932694) * w
                      - 0.531923007300) * w + 0.797884560593) * y
                      * 2.0;
                }
                else
                {
                    y = y - 2.0;
                    p = (((((((((((((-0.000045255659 * y
                      + 0.000152529290) * y - 0.000019538132) * y
                      - 0.000676904986) * y + 0.001390604284) * y
                      - 0.000794620820) * y - 0.002034254874) * y
                     + 0.006549791214) * y - 0.010557625006) * y
                     + 0.011630447319) * y - 0.009279453341) * y
                     + 0.005353579108) * y - 0.002141268741) * y
                     + 0.000535310849) * y + 0.999936657524;
                }
            }
            if (z > 0.0)
                return (p + 1.0) / 2;
            else
                return (1.0 - p) / 2;
        } // Gauss()
        double chiSquare(double[] expected, int[] observed)
        {
            double sumExpected = 0;
            int sumObserved = 0;

            for (int i = 0; i < observed.Length; i++)
            {
                sumExpected += expected[i];
                sumObserved += observed[i];
            }
            double ratio = 1;
            bool rescale = false;

            //проверка на согласованность
            if (Math.Abs(sumExpected - sumObserved) > 10E-6)
            {
                ratio = sumObserved / sumExpected;
                rescale = true;
            }

            double sumSq = 0;
            double dev = 0;
            for (int i = 0; i < observed.Length; i++)
            {
                if (rescale)//если не согласованно
                {
                    dev = observed[i] - ratio * expected[i];
                    sumSq += dev * dev / (ratio * expected[i]);
                }
                else
                {
                    dev = observed[i] - expected[i];
                    sumSq += dev * dev / expected[i];
                }
            }

            return sumSq;
        }

        /// <summary>
        /// Извлекает яркость пикселей по 3м каналам 
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns>
        /// 3-х мерный массив числовых значений яркости пикселей на манер матрицы numpy, где первое значение означает количество строк, а второе - количество столбцов.
        /// <para> Формат: [Высота, Ширина, Канал].</para>
        /// <para> "Канал" принимает значения:</para>
        /// <para>0 - Красный</para>
        /// <para>1 - Зеленый</para>
        /// <para>2 - Синий</para>
        /// </returns>
        public unsafe static byte[,,] BitmapToByteRgb(Bitmap bmp)
        {
            int width = bmp.Width,
                height = bmp.Height;
            byte[,,] res = new byte[height, width, 3];
            BitmapData bd = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            try
            {
                byte* curpos;
                for (int h = 0; h < height; h++)
                {
                    curpos = ((byte*)bd.Scan0) + h * bd.Stride;
                    for (int w = 0; w < width; w++)
                    {
                        res[h, w, 2] = *(curpos++);
                        res[h, w, 1] = *(curpos++);
                        res[h, w, 0] = *(curpos++);
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(bd);
            }
            return res;
        }
    }
}
