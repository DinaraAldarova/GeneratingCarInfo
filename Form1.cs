using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace GeneratingCarInfo
{
    public partial class Form1 : Form
    {
        CarInfo carInfo;
        public Form1()
        {
            InitializeComponent();
            carInfo = new CarInfo();
            comboBox1.SelectedItem = comboBox1.Items[0];
            label6.Text = "Сейчас топлива " + (carInfo.fuel / 1000.0).ToString() + " л";

            //Следующий кусок кода использовался для перевода бинарного файла в формат txt для удобного рассмотрения в блокноте

            //StreamReader reader = new StreamReader(/*"C:\\Users\\Developer\\Downloads\\trip"*/"C:\\Users\\Developer\\Desktop\\trip.rec");
            //StreamWriter writer = new StreamWriter("C:\\Users\\Developer\\Desktop\\trip.txt", false, System.Text.Encoding.Default);

            //const int block = 25;
            //int[] array = new int[block];
            //string report = "";

            //for (int i = 0; !reader.EndOfStream; i += block)
            //{
            //    for (int j = 0; j < block; j++)
            //        array[j] = reader.Read();
            //    //reader.Read(array, 0, block);
            //    report = ToHex(array[0], 2) + ToHex(array[1], 2) + ToHex(array[2], 2) + ToHex(array[3], 2) //+ " "
            //        + ToHex(array[4], 2) + ToHex(array[5], 2) + ToHex(array[6], 2) + ToHex(array[7], 2) //+ " "
            //        + ToHex(array[8], 2) //+ " "
            //        + ToHex(array[9], 2) + ToHex(array[10], 2) + ToHex(array[11], 2) + ToHex(array[12], 2) //+ " "
            //        + ToHex(array[13], 2) //+ " " 
            //        + ToHex(array[14], 2) //+ " "
            //        + ToHex(array[15], 2) + ToHex(array[16], 2) //+ " " 
            //        + ToHex(array[17], 2) + ToHex(array[18], 2) //+ " "
            //        + ToHex(array[19], 2) + ToHex(array[20], 2) //+ " " 
            //        + ToHex(array[21], 2) + ToHex(array[22], 2) //+ " "
            //        + ToHex(array[23], 2) + ToHex(array[24], 2) + "\n";
            //    writer.Write(report);
            //}
            ////richTextBox1.Text = report;
            //reader.Close();
            //writer.Close();
        }
        public string ToHex(int value, int count)
        {
            string hex = new string('0', count) + Convert.ToString(value, 16);
            return hex.Substring(hex.Length - count);
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            TypeActivity activity = (TypeActivity)comboBox1.SelectedIndex;
            if (activity == TypeActivity.Stay)
            {
                numericUpDown1.Visible = false;
                label4.Visible = false;
            }
            else if (activity == TypeActivity.Ride)
            {
                numericUpDown1.Visible = true;
                label4.Visible = true;
                label4.Text = "Со скоростью (км/ч)";
            }
            else if (activity == TypeActivity.ReFuel)
            {
                numericUpDown1.Visible = true;
                label4.Visible = true;
                label4.Text = "Объем топлива (л)";
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            TypeActivity activity = (TypeActivity)comboBox1.SelectedIndex;
            int value = Convert.ToInt32(numericUpDown1.Value);
            if (activity == TypeActivity.ReFuel)
                value *= 1000;

            carInfo.AddActivity(dateTimePicker1.Value, dateTimePicker2.Value, activity, value);

            richTextBox1.Text = carInfo.report;
            label6.Text = "Сейчас топлива " + (carInfo.fuel / 1000.0).ToString() + " л";
            dateTimePicker1.Value = dateTimePicker2.Value;
            dateTimePicker2.Value = dateTimePicker2.Value.AddMinutes(5);
            dateTimePicker1.Enabled = false;
            button1.Enabled = true;
        }
    }

    public enum TypeActivity
    {
        Stay = 0,
        Ride = 1,
        ReFuel = 2
    }

    struct BinaryReport
    {
        public uint timestamp;
        public uint id;
        public byte is29bit;
        public uint vallen;
        public byte[] val;
        public byte colorspec;
        public ushort[] colors;
    }

    public class CarInfo
    {
        public string report = "";
        public string puthShortReport = "C:\\Users\\Developer\\Desktop\\reportShort.rec";
        public string puthFullReport = "C:\\Users\\Developer\\Desktop\\reportFull.txt";

        /// <summary>
        /// Объем топлива в баке (мл)
        /// </summary>
        public int fuel { get; private set; } = 15000;
        /// <summary>
        /// Размер топливного бака (мл)
        /// </summary>
        public int maxFuel { get; private set; } = 47000;

        /// <summary>
        /// Расход топлива л/км для линейной функции изменения расхода топлива
        /// </summary>
        private const double d = 0.1;

        /// <summary>
        /// Время в секундах от включения машины. Используется для записи в бинарный файл
        /// </summary>
        private uint timeNow = 0;

        private Random random = new Random();
        public CarInfo()
        {
            BinaryWriter fileShort = new BinaryWriter(File.Open(puthShortReport, FileMode.Create), System.Text.Encoding.Default);
            StreamWriter fileFull = new StreamWriter(puthFullReport, false, System.Text.Encoding.Default);
            report += "id\tpackets\t\t\tdate\t   time\t\tid\tfuel\t\trealFuel\n";
            fileFull.Write(report);
            report += "Вывод данных на экран отключен. Для включения необходимо раскомментировать соответствующие строки кода";
            fileFull.Close();
            fileShort.Close();
        }

        public bool AddActivity(DateTime timeStart, DateTime timeFinish, TypeActivity activity, int value)
        {
            string localReport = "";
            BinaryWriter fileShort = new BinaryWriter(File.Open(puthShortReport, FileMode.Open), System.Text.Encoding.Default);
            StreamWriter fileFull = new StreamWriter(puthFullReport, true, System.Text.Encoding.Default);
            int ms = Convert.ToInt32((timeFinish - timeStart).Ticks / TimeSpan.TicksPerMillisecond);
            uint stepMs = 200;
            int startFuel = fuel;
            //Показание счетчика для отчета, с погрешностью
            int valueFuel = fuel;
            //id пакета данный
            int id = 0;

            // Расход топлива за данный промежуток времени (мл)
            double fuelConsumption = 0;
            if (activity == TypeActivity.Ride)
            {
                fuelConsumption = FuelConsumption(value, d);
                fuelConsumption *= 1.0 * ms / 3600000 * 1000;
            }

            //Проверка на переполнение топливного бака или досрочное окончание топлива
            if((fuelConsumption > fuel && activity == TypeActivity.Ride) ||
                activity == TypeActivity.ReFuel && fuel + value > maxFuel)
            {
                return false;
            }

            timeNow = Convert.ToUInt32(((timeStart.Hour * 60 + timeStart.Minute) * 60 + timeStart.Second) * 1000 + timeStart.Millisecond);

            uint startTime = timeNow;
            BinaryReport binaryReport;
            for (uint i = stepMs; i <= ms; i += stepMs)
            {
                binaryReport = new BinaryReport();
                timeNow = startTime + i;
                binaryReport.timestamp = timeNow;

                //Формирование пакета с id = 125: уровень топлива в баке
                id = 125;
                localReport = ToHex(id, 4) + "\t";
                binaryReport.id = Convert.ToUInt32(id);

                if (activity == TypeActivity.ReFuel)
                {
                    fuel = startFuel + Convert.ToInt32(Math.Truncate(1.0 * value / ms * i));
                    //Показание счетчика для отчета, с погрешностью
                    valueFuel = NoizeFuel(30);
                }
                else if (activity == TypeActivity.Ride)
                {
                    fuel = startFuel - Convert.ToInt32(Math.Truncate(fuelConsumption / ms * i));
                    //Показание счетчика для отчета, с погрешностью
                    valueFuel = NoizeFuel(10);
                }
                else if (activity == TypeActivity.Stay)
                    //Показание счетчика для отчета, с погрешностью
                    valueFuel = NoizeFuel(2);

                const byte leng = 8;
                binaryReport.vallen = leng;
                binaryReport.val = new byte[leng];
                random.NextBytes(binaryReport.val);
                binaryReport.val[5] = FuelInPercentage(valueFuel);
                for (int j = 0; j < leng; j++)
                    localReport += ToHex(binaryReport.val[j], 2);

                localReport += "\t" + (timeStart.AddMilliseconds(i)).ToString();
                localReport += "\t" + id.ToString() + "\t" + (1.0 * FuelInPercentage(valueFuel) / 2).ToString() + " %";
                localReport += "\t\t" + (1.0 * fuel / 1000).ToString() + " л";
                localReport += "\t" + (1.0 * FuelInPercentage(fuel) / 2).ToString() + " %";
                localReport += "\n";

                fileFull.Write(localReport);

//ОТКЛЮЧИЛА ВЫВОД НА ЭКРАН
//report += localReport;

                binaryReport.colorspec = 1;
                binaryReport.colors = new ushort[] { 0xfdfd, 0xfdfd, 0xfdfd, 0xfdfd, 0 };
                WriteBinaryReport(fileShort, binaryReport);

                if (i % 500 == 0)
                {
                    //рандомные пакеты каждые 30 мс
                    binaryReport = new BinaryReport();
                    binaryReport.timestamp = timeNow;

                    id = 77;
                    localReport = ToHex(id, 4) + "\t";
                    binaryReport.id = Convert.ToUInt32(id);

                    const byte len = 8;
                    binaryReport.vallen = len;
                    binaryReport.val = new byte[len];
                    random.NextBytes(binaryReport.val);

                    for (int j = 0; j < len; j++)
                        localReport += ToHex(binaryReport.val[j], 2);

                    localReport += "\t" + (timeStart.AddMilliseconds(i)).ToString() + "\n";
                    fileFull.Write(localReport);

//ОТКЛЮЧИЛА ВЫВОД НА ЭКРАН
//report += localReport;

                    binaryReport.colorspec = 1;
                    binaryReport.colors = new ushort[] { 0xfdfd, 0xfdfd, 0xfdfd, 0xfdfd, 0 };
                    WriteBinaryReport(fileShort, binaryReport);
                }

                if (i % 1000 == 0)
                {
                    //рандомные пакеты каждые 0,5 сек
                    binaryReport = new BinaryReport();
                    binaryReport.timestamp = timeNow;

                    id = 88;
                    localReport = ToHex(id, 4) + "\t";
                    binaryReport.id = Convert.ToUInt32(id);

                    const byte len = 6;
                    binaryReport.vallen = len;
                    binaryReport.val = new byte[len];
                    random.NextBytes(binaryReport.val);

                    for (int j = 0; j < len; j++)
                        localReport += ToHex(binaryReport.val[j], 2);

                    localReport += "\t\t" + (timeStart.AddMilliseconds(i)).ToString() + "\n";
                    fileFull.Write(localReport);

//ОТКЛЮЧИЛА ВЫВОД НА ЭКРАН
//report += localReport;

                    binaryReport.colorspec = 1;
                    binaryReport.colors = new ushort[] { 0xfdfd, 0xfdfd, 0xfdfd, 0xfdfd, 0 };
                    WriteBinaryReport(fileShort, binaryReport);
                }

                if (i % 3000 == 0)
                {
                    //рандомные пакеты каждую секунду
                    binaryReport = new BinaryReport();
                    binaryReport.timestamp = timeNow;

                    id = 99;
                    localReport = ToHex(id, 4) + "\t";
                    binaryReport.id = Convert.ToUInt32(id);

                    const byte len = 6;
                    binaryReport.vallen = len;
                    binaryReport.val = new byte[len];
                    random.NextBytes(binaryReport.val);

                    for (int j = 0; j < len; j++)
                        localReport += ToHex(binaryReport.val[j], 2);

                    localReport += "\t\t" + (timeStart.AddMilliseconds(i)).ToString() + "\n";
                    fileFull.Write(localReport);

                    //ОТКЛЮЧИЛА ВЫВОД НА ЭКРАН
                    //report += localReport;

                    binaryReport.colorspec = 1;
                    binaryReport.colors = new ushort[] { 0xfdfd, 0xfdfd, 0xfdfd, 0xfdfd, 0 };
                    WriteBinaryReport(fileShort, binaryReport);
                }
            }
            fileFull.Close();
            fileShort.Close();
            return true;
        }

        /// <summary>
        /// Доля заполненности топливного бака
        /// </summary>
        /// <param name="volume">Объем топлива в баке (fuel или Noise(percent)) (мл)</param>
        /// <param name="maxPercent">Максимальное значение заполненности(0..maxPercent)</param>
        /// <returns>Доля заполненности топливного бака от 0 до maxPercent</returns>
        private byte FuelInPercentage(int volume, byte maxPercent = 200)
        {
            return Convert.ToByte(1.0 * maxPercent * volume / maxFuel);
        }

        /// <summary>
        /// Преобразует число в бинарный формат с фиксированным количеством бит
        /// </summary>
        /// <param name="value">Преобразовываемое значение</param>
        /// <param name="count">Количество получаемых бит</param>
        /// <returns>Строка, содержащая число в бинарном формате с указанным числом бит</returns>
        private string ToBits(int value, int count)
        {
            string buf = new string('0', count) + Convert.ToString(value, 2);
            return buf.Substring(buf.Length - count);
        }

        /// <summary>
        /// Преобразует число в шестнадцатеричный формат с фиксированным количеством знаков
        /// </summary>
        /// <param name="value">Преобразовываемое значение</param>
        /// <param name="count">Количество получаемых знаков</param>
        /// <returns>Строка, содержащая число в шестнадцатеричном формате с указанным числом знаков</returns>
        private string ToHex(int value, int count)
        {
            string hex = new string('0', count) + Convert.ToString(value, 16);
            return hex.Substring(hex.Length - count);
        }

        /// <summary>
        /// Линейная функция расхода топлива за единицу времени в зависимости от скорости (л/ч)
        /// </summary>
        /// <param name="speed">Скорость движения (км/ч)</param>
        /// <param name="d">Расход топлива на километр (л/км)</param>
        /// <returns>Расход топлива (л/ч)</returns>
        private double FuelConsumption(int speed, double d)
        {
            return speed * d;
        }

        /// <summary>
        /// Функция вычисления объема топлива в баке с зашумлением  (мл)
        /// </summary>
        /// <param name="percent">Диапазон шума в каждую сторону (+/-, %)</param>
        /// <returns>Полученный объем топлива с шумом (мл)</returns>
        private int NoizeFuel(int percent)
        {
            //Диапазон разбега шума в мл в каждую сторону (+/-)
            int n = maxFuel * percent / 100;
            //Полученная случайная величина шума
            int d = random.Next(0, n * 2) - n;
            //Объем топлива с шумом
            int res = fuel + d;
            if (res < 0)
                res = 0;
            else if (res > maxFuel)
                res = maxFuel;
            return res;
        }

        /// <summary>
        /// Записывает в бинарный файл fileShort пакет типа binaryReport
        /// </summary>
        /// <param name="writer">Бинарный файл fileShort</param>
        /// <param name="binaryReport">Пакет данных</param>
        private void WriteBinaryReport(BinaryWriter writer, BinaryReport binaryReport)
        {
            writer.Write(binaryReport.timestamp);
            writer.Write(binaryReport.id);
            writer.Write(binaryReport.is29bit);
            writer.Write(binaryReport.vallen);
            for (int i = 0; i < binaryReport.vallen; i++)
                writer.Write(binaryReport.val[i]);
            writer.Write(binaryReport.colorspec);
            writer.Write(binaryReport.colors[0]);
            writer.Write(binaryReport.colors[1]);
            writer.Write(binaryReport.colors[2]);
            writer.Write(binaryReport.colors[3]);
            writer.Write(binaryReport.colors[4]);
        }
    }

}
