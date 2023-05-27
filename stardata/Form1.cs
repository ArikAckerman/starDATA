using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace StarData
{
    public partial class Form1 : Form
    {
        private readonly StarDataManager starDataManager;

        public Form1()
        {
            InitializeComponent();
            starDataManager = new StarDataManager();
            Commentary.Text += "1). Format for a new star: 'hours(RA) minutes(RA) seconds(RA) degrees minutes(DEC) seconds(DEC) distance(pc) radius name':";
        }

        private void buttonWrite_Click(object sender, EventArgs e)
        {
            string line = EnterInf.Text;
            StarDataEntry entry = StarDataParser.Parse(line);

            if (entry == null)
            {
                MessageBox.Show("Invalid input format");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    starDataManager.SaveData(entry, saveFileDialog.FileName);
                    Commentary.Text += $"\r2). Data Saved '{saveFileDialog.FileName}':\r\n{entry}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving data: {ex.Message}");
                }
            }
        }

        private void buttonRead_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files (*.csv)|*.csv";
            openFileDialog.Title = "Select File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    List<StarDataEntry> entries = starDataManager.LoadData(openFileDialog.FileName);
                    DisplayStarData(entries);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading data: {ex.Message}");
                }
            }
        }

        private void DisplayStarData(List<StarDataEntry> entries)
        {
            string header = "Hr(RA) Min(RA) Sec(RA)  Deg(DEC) Hr(DEC) Sec(DEC)  Dst(PC) Rad  Nm";
            string output = "";

            foreach (StarDataEntry entry in entries)
            {
                output += $"{entry}\r\n";
            }

            string abcOutput = starDataManager.GetFormattedABCData(entries);
            string xyzOutput = starDataManager.GetFormattedXYZData(entries);

            Commentary.Text += $"\r2). File: '{starDataManager.LastLoadedFilePath}':\r\n\n{header}\n{output}";
            Commentary.Text += $"\n3). A,B,C:\n{abcOutput}";
            Commentary.Text += $"\n\n4). X,Y,Z:\n{xyzOutput}";
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you wish to quit?", "Exit App", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Commentary_TextChanged(object sender, EventArgs e)
        {

        }
    }

    public static class StarDataParser
    {
        public static StarDataEntry Parse(string input)
        {
            string[] values = input.Split(' ');

            if (values.Length < 7)
            {
                return null;
            }

            try
            {
                int hoursRA = int.Parse(values[0]);
                int minutesRA = int.Parse(values[1]);
                double secondsRA = double.Parse(values[2]);

                int degreesDEC = int.Parse(values[3]);
                int minutesDEC = int.Parse(values[4]);
                double secondsDEC = double.Parse(values[5]);

                double distance = double.Parse(values[6]);

                double radius = 0;
                double.TryParse(values[7], out radius);

                string name = values[8];

                return new StarDataEntry(hoursRA, minutesRA, secondsRA, degreesDEC, minutesDEC, secondsDEC, distance, radius, name);
            }
            catch
            {
                return null;
            }
        }
    }

    public class StarDataEntry
    {
        public int HoursRA { get; }
        public int MinutesRA { get; }
        public double SecondsRA { get; }
        public int DegreesDEC { get; }
        public int MinutesDEC { get; }
        public double SecondsDEC { get; }
        public double Distance { get; }
        public double Radius { get; }
        public string Name { get; }

        public StarDataEntry(int hoursRA, int minutesRA, double secondsRA, int degreesDEC, int minutesDEC, double secondsDEC, double distance, double radius, string name)
        {
            HoursRA = Math.Max(0, Math.Min(23, hoursRA));
            MinutesRA = Math.Max(0, Math.Min(59, minutesRA));
            SecondsRA = Math.Max(0, Math.Min(59.9999, secondsRA));
            DegreesDEC = Math.Max(-360, Math.Min(360, degreesDEC));
            MinutesDEC = Math.Max(0, Math.Min(59, minutesDEC));
            SecondsDEC = Math.Max(0, Math.Min(59.9999, secondsDEC));
            Distance = Math.Max(0, Math.Min(40, distance));
            Radius = Math.Max(0, Math.Min(30, radius));
            Name = name;
        }

        public override string ToString()
        {
            return $"{HoursRA,-9} {MinutesRA,-12} {SecondsRA,-7:F4} {DegreesDEC,-14} {MinutesDEC,-12} {SecondsDEC,-10} {Distance,-2} {Radius,-2} {Name}";
        }
    }

    public class StarDataManager
    {
        public string LastLoadedFilePath { get; private set; }

        public void SaveData(StarDataEntry entry, string filePath)
        {
            using (StreamWriter file = new StreamWriter(filePath, true))
            {
                file.WriteLine(entry);
            }
        }

        public List<StarDataEntry> LoadData(string filePath)
        {
            LastLoadedFilePath = filePath;
            List<StarDataEntry> entries = new List<StarDataEntry>();

            using (StreamReader file = new StreamReader(filePath))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    StarDataEntry entry = StarDataParser.Parse(line);
                    if (entry != null)
                    {
                        entries.Add(entry);
                    }
                }
            }

            return entries;
        }

        public string GetFormattedABCData(List<StarDataEntry> entries)
        {
            string abcOutput = "A             B             C";
            foreach (StarDataEntry entry in entries)
            {
                double a = Math.Round((entry.HoursRA * 15) + (entry.MinutesRA * 0.25) + (entry.SecondsRA * 0.004166), 3);
                double b = Math.Round(Math.Abs(entry.DegreesDEC + (entry.MinutesDEC / 60) + (entry.SecondsDEC / 3600)) * Math.Sign(entry.DegreesDEC), 3);
                abcOutput += $"\r\n{a,-12} {b,-12} {entry.Distance}";
            }

            return abcOutput;
        }

        public string GetFormattedXYZData(List<StarDataEntry> entries)
        {
            string xyzOutput = "X             Y             Z";
            foreach (StarDataEntry entry in entries)
            {
                double x = Math.Round(entry.Distance * Math.Cos(entry.HoursRA * 15 * Math.PI / 180) * Math.Cos(entry.DegreesDEC * Math.PI / 180), 3);
                double y = Math.Round(entry.Distance * Math.Sin(entry.HoursRA * 15 * Math.PI / 180) * Math.Cos(entry.DegreesDEC * Math.PI / 180), 3);
                double z = Math.Round(entry.Distance * Math.Sin(entry.DegreesDEC * Math.PI / 180), 3);
                xyzOutput += $"\r\n{x,-12} {y,-12} {z}";
            }

            return xyzOutput;
        }
    }
}
