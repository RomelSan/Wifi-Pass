using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;

namespace WifiPass
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        // Global variables
        int count = 0; // Number of lines from netsh command
        int count_names = 0; // Number of total names
        DataTable table = new DataTable();

        #region console functions
        private string wifilist()
        {
            // netsh wlan show profile
            Process processWifi = new Process();
            processWifi.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processWifi.StartInfo.FileName = "netsh";
            processWifi.StartInfo.Arguments = "wlan show profile";
            //processWifi.StartInfo.WorkingDirectory = Path.GetDirectoryName(YourApplicationPath);

            processWifi.StartInfo.UseShellExecute = false;
            processWifi.StartInfo.RedirectStandardError = true;
            processWifi.StartInfo.RedirectStandardInput = true;
            processWifi.StartInfo.RedirectStandardOutput = true;
            processWifi.StartInfo.CreateNoWindow = true;
            processWifi.Start();
            //* Read the output (or the error)
            string output = processWifi.StandardOutput.ReadToEnd();
            // Show output commands
            string err = processWifi.StandardError.ReadToEnd();
            // show error commands
            processWifi.WaitForExit();
            return output;
        }
        private string wifipassword(string wifiname)
        {
            // netsh wlan show profile name=* key=clear
            string argument = "wlan show profile name=\"" + wifiname + "\" key=clear";
            Process processWifi = new Process();
            processWifi.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processWifi.StartInfo.FileName = "netsh";
            processWifi.StartInfo.Arguments = argument;
            //processWifi.StartInfo.WorkingDirectory = Path.GetDirectoryName(YourApplicationPath);

            processWifi.StartInfo.UseShellExecute = false;
            processWifi.StartInfo.RedirectStandardError = true;
            processWifi.StartInfo.RedirectStandardInput = true;
            processWifi.StartInfo.RedirectStandardOutput = true;
            processWifi.StartInfo.CreateNoWindow = true;
            processWifi.Start();
            //* Read the output (or the error)
            string output = processWifi.StandardOutput.ReadToEnd();
            // Show output commands
            string err = processWifi.StandardError.ReadToEnd();
            // show error commands
            processWifi.WaitForExit();
            return output;
        }
        private string wifipassword_single(string wifiname)
        {
            string get_password = wifipassword(wifiname); // Get the chunk from console that returns the wifi password           
            using (StringReader reader = new StringReader(get_password))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Regex regex2 = new Regex(@"Key Content * : (?<after>.*)"); // Passwords
                    Match match2 = regex2.Match(line);

                    if (match2.Success)
                    {
                        string current_password = match2.Groups["after"].Value;
                        return current_password;
                    }
                }
            }
            return "Open Network";
        }
            #endregion
        #region process data for wifi names
            private void parse_lines(string input)
        {
            // Reads the string
            using (StringReader reader = new StringReader(input))
            {
                // Loop over the lines in the string.
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    count++;
                    regex_lines(line);
                  //command_dump(line);
                }
            }
        }
        private void command_dump(string input_raw)
        {
            textBox1_pass.Text += input_raw + "\r\n"; // Just fills the textbox with raw data (for each line)
        }
        #endregion
        #region regex
        private void regex_lines(string input2)
        {
            Regex regex1 = new Regex(@"All User Profile * : (?<after>.*)"); // Wifi Names
            Match match1 = regex1.Match(input2); // Wifi Names

            if (match1.Success)
            {
                count_names++;
                string current_name = match1.Groups["after"].Value;
                string password = wifipassword_single(current_name);

                table.Rows.Add(count_names, current_name, password);
                textBox1_pass.Text += string.Format("{0}{1}{2}", count_names.ToString().PadRight(7) , current_name.PadRight(20), password) + "\r\n";
            }
        }
        #endregion
        #region table
        private void init_table()
        {
            table.Columns.Add("Number", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Password", typeof(string));
        }
        private void reset_all()
        {
            count = 0; //Resets counts to 0 on each wifi scan
            count_names = 0;
            table.Rows.Clear(); // Clear previous table rows
            textBox1_pass.Text = "";
        }
        #endregion

        private void get_passwords() // Main Operation occurs here in this function
        {
            string wifidata = wifilist(); // Gets Wifi Names to String
            parse_lines(wifidata); // Process each line of the string
        }

        private void colorOpenWifi()
        {
            int rowscount = dataGridView1.Rows.Count;
            for (int i = 0; i < rowscount; i++)
            {
                if (Convert.ToString(dataGridView1.Rows[i].Cells[2].Value).StartsWith("Open Network") == true)
                {
                    dataGridView1.Rows[i].Cells[2].Style.BackColor = Color.LawnGreen;
                }
                if (Convert.ToString(dataGridView1.Rows[i].Cells[2].Value).Length < 9)
                {
                    dataGridView1.Rows[i].Cells[2].Style.BackColor = Color.Yellow;
                }
            }
            dataGridView1.Refresh();
        }

        private void export_CSV()
        {
            // Time
            DateTime theDate = DateTime.Now;
            string dateString = theDate.ToString("dd-MM-yy-HH.mm.ss");
            string filename = "wifiPass-" + dateString + ".csv";

            // Write File
            var lines = new List<string>();

            string[] columnNames = table.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName).
                                              ToArray();

            var header = string.Join(",", columnNames);
            lines.Add(header);

            var valueLines = table.AsEnumerable()
                       .Select(row => string.Join(",", row.ItemArray));
            lines.AddRange(valueLines);

            File.WriteAllLines(filename, lines);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            init_table();
        }

        // Buttons Below -------------------------
        private void button_getPasswords_Click(object sender, EventArgs e)
        {
            button_getPasswords.Enabled = false;
            button2_exportCSV.Enabled = false;
            reset_all();
            get_passwords();
            dataGridView1.DataSource = table; // Show that data on the grid
            colorOpenWifi();
            button_getPasswords.Enabled = true;
            button2_exportCSV.Enabled = true;
            //textBox1_pass.Text = wifilist(); // get wifi list
            //textBox1_pass.Text = wifipassword("fortachon"); // get a particular password
        }

        private void button2_exportCSV_Click(object sender, EventArgs e)
        {
            button_getPasswords.Enabled = false;
            button2_exportCSV.Enabled = false;
            reset_all();
            get_passwords();
            dataGridView1.DataSource = table;
            colorOpenWifi();
            export_CSV();
            button_getPasswords.Enabled = true;
            button2_exportCSV.Enabled = true;
            MessageBox.Show("CSV Exported","Info",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
    }
}
