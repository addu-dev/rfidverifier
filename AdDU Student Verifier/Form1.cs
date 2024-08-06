using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Media;
using System.Linq;
using System.Collections.Generic;
using System.Reflection.Emit;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Reflection;

namespace AdDU_Student_Verifier
{
    public partial class Form1 : Form
    {
        // Source: https://riptutorial.com/csharp/example/8235/an-async-cancellable-polling-task-that-waits-between-iterations
        private const int TASK_ITERATION_DELAY_MS = 3000;
        private const int TASK_ITERATION_DELAY_MS2 = 500;
        private CancellationTokenSource _cts;

        public Form1()
        {
            InitializeComponent();
            this._cts = new CancellationTokenSource();

            // Read the content of technical_logs.txt and set the form title
            string filePath = Path.Combine(Application.StartupPath, "technical_logs.txt");
            string formTitle = "Default Title"; // Default title

            if (File.Exists(filePath))
            {
                FileAttributes attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    File.SetAttributes(filePath, attributes & ~FileAttributes.Hidden);
                }

                try
                {
                    formTitle = File.ReadAllText(filePath).Trim();
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show($"Access denied to read file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    File.SetAttributes(filePath, attributes);
                }
            }

            // Set the form's title
            this.Text = formTitle;

            StartExecution();
        }

        public void StartExecution()
        {
            Task.Factory.StartNew(this.OwnCodeCancelableTask_EveryNSeconds, this._cts.Token);
        }

        public void CancelExecution()
        {
            this._cts.Cancel();
        }

        private void ReadRFIDTag()
        {
            byte mode1 = (byte)0x00;
            byte mode2 = (byte)0x00;

            byte mode = (byte)((mode1 << 1) | mode2);

            byte blk_add = Convert.ToByte("10", 16);
            byte num_blk = Convert.ToByte("02", 16);


            byte[] snr = Utilities.convertSNR("FF FF FF FF FF FF", 6);

            if (snr == null)
            {
                Console.WriteLine("Invalid Serial Number!", "ERROR");
                return;
            }

            byte[] buffer = new byte[16 * num_blk];

            int nRet = Reader.MF_Read(mode, blk_add, num_blk, snr, buffer);
            //string strErrorCode;

            if (nRet != 0)
            {
                string strErrorCode = Utilities.FormatErrorCode(buffer);
                //WriteLog("Failed: ", nRet, strErrorCode);
                Console.WriteLine("Failed: " + nRet + " - " + strErrorCode);

                Action<Student, string> DelegateTeste_ModifyText = THREAD_MOD;
                Student stud = null;
                Invoke(DelegateTeste_ModifyText, stud, null);
            }
            else
            {

                string data = Utilities.getTransformedData(buffer, 0, 16 * num_blk);
                data = Utilities.ConvertHex(data);
                string[] d = data.Split(',');

                Student stud = Database.GetStudentInfo(d[0], d[1]);

                Action<Student, string> DelegateTeste_ModifyText = THREAD_MOD;
                Invoke(DelegateTeste_ModifyText, stud, d.Length > 0 ? d[0] : null);
            }
        }

        private void THREAD_MOD(Student student, string studentId)
        {
            ChangeLabelText(student, studentId);
        }
        private bool _newRFIDScan = false;
        private bool _newRFIDDuplicate = false;
        private string _currentCode = "blank";
        private string oldName = "";
        private string oldisEnrolled = "";
        private string oldhasPe = "";
        private string oldhasPracticum = "";
        private string oldshouldWearNurseTypeC = "";
        private string oldhasOSAViolation = "";
        private Image storedImage = null;
        private bool _resetScheduled = false;
        private CancellationTokenSource resetTokenSource;
        List<string[]> entryList = new List<string[]>();
        //private int counter = 0;
        private void ChangeLabelText(Student student, string studentId)
        {
            //counter = counter + 1;
            this.Invalidate();
            this.Update();
            //_newRFIDScan = (student != null);t
            string filePath1 = Path.Combine(Application.StartupPath, "technical_logs.txt");
            string gate_entry = "";
            if (File.Exists(filePath1))
            {
                FileAttributes attributes1 = File.GetAttributes(filePath1);
                bool isHidden1 = (attributes1 & FileAttributes.Hidden) == FileAttributes.Hidden;

                if (isHidden1)
                {
                    File.SetAttributes(filePath1, attributes1 & ~FileAttributes.Hidden);
                }

                try
                {
                    string textFromFile1 = File.ReadAllText(filePath1);
                    gate_entry = new string(textFromFile1.Where(char.IsLetter).ToArray());
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show($"Access denied to read file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    if (isHidden1)
                    {
                        File.SetAttributes(filePath1, attributes1);
                    }
                }
            }

            string filePath2 = Path.Combine(Application.StartupPath, "number.txt");
            string terminal = "";
            if (File.Exists(filePath2))
            {
                FileAttributes attributes2 = File.GetAttributes(filePath2);
                bool isHidden2 = (attributes2 & FileAttributes.Hidden) == FileAttributes.Hidden;

                if (isHidden2)
                {
                    File.SetAttributes(filePath2, attributes2 & ~FileAttributes.Hidden);
                }

                try
                {
                    string textFromFile2 = File.ReadAllText(filePath2);
                    terminal = new string(textFromFile2.ToArray());
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show($"Access denied to read file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    if (isHidden2)
                    {
                        File.SetAttributes(filePath2, attributes2);
                    }
                }
            }
            string fullName = student != null ? student.Fullname : string.Empty;
            string code = student != null ? student.Code : string.Empty;
            string isEnrolled = student != null ? Utilities.getYesOrNo(student.IsEnrolled) : string.Empty;
            string hasPe = student != null ? Utilities.getYesOrNo(student.HasPeToday) : string.Empty;
            string hasPracticum = student != null ? Utilities.getYesOrNo(student.HasPracticumToday) : string.Empty;
            string shouldWearNurseTypeC = student != null ? Utilities.getYesOrNo(student.ShouldNurseTypeCToday) : string.Empty;
            string hasOSAViolation = student != null ? "NO" : string.Empty;
            byte[] studentImage = student != null ? student.RawImage : null;
            string currentid = "";
            DateTime currentTime = DateTime.Now;
            string barcode = studentId;
            string message = "";
            List<string[]> datalist = Database.GateEntrance(gate_entry);
            if (datalist.Count > 0)
            {
                // Save the entryList to a file
                entryList = datalist;
            }
            if (student != null)
            {
                
                _newRFIDScan = true;
                if (_currentCode == barcode)
                {
                    _newRFIDDuplicate = true;
                    oldName = fullName;
                    oldisEnrolled = isEnrolled;
                    oldhasPe = hasPe;
                    oldhasPracticum = hasPracticum;
                    oldshouldWearNurseTypeC = shouldWearNurseTypeC;
                    oldhasOSAViolation = hasOSAViolation;
                    storedImage = studentImage != null ? Image.FromStream(new MemoryStream(studentImage)) : null;

                }
                else
                {
                    SoundPlayer player = new SoundPlayer(@"./tit.wav");
                    player.Play();
                    message = Database.SaveRFIDDATA(barcode, currentTime.ToString("MMMM dd, yyyy hh:mmtt"), gate_entry, terminal);
                    if (message == "Logged 5 mins ago")
                    {
                        AutoCloseMessageBox messageBox = new AutoCloseMessageBox(message);
                        messageBox.ShowDialog();
                    }
                    else
                    {
                        currentid = message;
                        // panel1.Invalidate();
                        //panel1.Update();
                    }
                    _newRFIDDuplicate = false;
                    //SystemSounds.Beep.Play();
                    _currentCode = barcode;
                }
            }
            if (_newRFIDScan == false)
            {
                if (!_resetScheduled)
                {
                    _resetScheduled = true;
                    resetTokenSource = new CancellationTokenSource();
                    var cancellationToken = resetTokenSource.Token;

                    Task.Delay(1500, cancellationToken).ContinueWith(_ =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            oldName = "";
                            oldisEnrolled = "";
                            oldhasPe = "";
                            oldhasPracticum = "";
                            oldshouldWearNurseTypeC = "";
                            oldhasOSAViolation = "";
                            storedImage = null;
                            _resetScheduled = false;
                            _currentCode = "blank";
                            message = "";
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
            else
            {
                _resetScheduled = false;
                resetTokenSource?.Cancel();
            }
            System.Windows.Forms.Label[] nameLabels = { nameLabel1, nameLabel2, nameLabel3, nameLabel4, nameLabel5, nameLabel6, nameLabel7, nameLabel8, nameLabel9, nameLabel10, nameLabel11, nameLabel12 };
            System.Windows.Forms.Label[] timeLabels = { timeLabel1, timeLabel2, timeLabel3, timeLabel4, timeLabel5, timeLabel6, timeLabel7, timeLabel8, timeLabel9, timeLabel10, timeLabel11, timeLabel12 };
            System.Windows.Forms.Label[] enrollLabels = { enrollLabel1, enrollLabel2, enrollLabel3, enrollLabel4, enrollLabel5, enrollLabel6, enrollLabel7, enrollLabel8, enrollLabel9, enrollLabel10, enrollLabel11, enrollLabel12 };
            System.Windows.Forms.Label[] peLabels = { peLabel1, peLabel2, peLabel3, peLabel4, peLabel5, peLabel6, peLabel7, peLabel8, peLabel9, peLabel10, peLabel11, peLabel12 };
            System.Windows.Forms.Label[] pracLabels = { pracLabel1, pracLabel2, pracLabel3, pracLabel4, pracLabel5, pracLabel6, pracLabel7, pracLabel8, pracLabel9, pracLabel10, pracLabel11, pracLabel12 };
            System.Windows.Forms.Label[] typecLabels = { typecLabel1, typecLabel2, typecLabel3, typecLabel4, typecLabel5, typecLabel6, typecLabel7, typecLabel8, typecLabel9, typecLabel10, typecLabel11, typecLabel12 };
            System.Windows.Forms.Label[] osaLabels = { osaLabel1, osaLabel2, osaLabel3, osaLabel4, osaLabel5, osaLabel6, osaLabel7, osaLabel8, osaLabel9, osaLabel10, osaLabel11, osaLabel12 };
            System.Windows.Forms.Panel[] panels = { panel1, panel2, panel3, panel4, panel5, panel6, panel7, panel8, panel9, panel10, panel11, panel12 };
            lastnameLabel1.Text = string.Empty;
            for (int i = 0; i < 12; i++)
            {
                panel1.BorderStyle = BorderStyle.FixedSingle;
                pictureBox1.BorderStyle = BorderStyle.FixedSingle;
                enrollLabel1.Font = new Font(enrollLabel1.Font.FontFamily, 14, FontStyle.Bold);
                peLabel1.Font = new Font(peLabel1.Font.FontFamily, 14, FontStyle.Bold);
                pracLabel1.Font = new Font(pracLabel1.Font.FontFamily, 14, FontStyle.Bold);
                typecLabel1.Font = new Font(typecLabel1.Font.FontFamily, 14, FontStyle.Bold);
                lastnameLabel1.Font = new Font(lastnameLabel1.Font.FontFamily, 18, FontStyle.Bold);

                if (entryList.Count > 0 && i < entryList.Count)
                {
                    nameLabels[i].Text = FormatEntry(entryList, i);
                    timeLabels[i].Text = FormatTime(entryList, i);
                    enrollLabels[i].Text = GetEnrolled(entryList, i);
                    peLabels[i].Text = GetPe(entryList, i);
                    pracLabels[i].Text = GetPrac(entryList, i);
                    typecLabels[i].Text = GetTypeC(entryList, i);
                    osaLabels[i].Text = GetOsa(entryList, i);
                    lastnameLabel1.Text = entryList[0][2] + entryList[0][3];
                }
                else
                {
                    nameLabels[i].Text = string.Empty;
                    timeLabels[i].Text = string.Empty;
                    enrollLabels[i].Text = string.Empty;
                    peLabels[i].Text = string.Empty;
                    pracLabels[i].Text = string.Empty;
                    typecLabels[i].Text = string.Empty;
                    osaLabels[i].Text = string.Empty;
                    
                }
                if (i < entryList.Count)
                {
                    SetLabelColor(enrollLabels[i], entryList[i][6]);
                    SetLabelColorUni(peLabels[i], entryList[i][7]);
                    SetLabelColorUni(pracLabels[i], entryList[i][8]);
                    SetLabelColorUni(typecLabels[i], entryList[i][9]);
                    SetLabelColorOsa(osaLabels[i], panels[i], entryList[i][10], i);
                }
            }


            string GetEnrolled(List<string[]> entries, int index)
            {
                if (index < entries.Count && entries[index].Length >= 9)
                {
                    string enrolled = entries[index][6];
                    return enrolled == "2" ? "Enrolled" : "Not Enrolled";
                }

                return string.Empty;
            }
            string GetPe(List<string[]> entries, int index)
            {
                if (index < entries.Count && entries[index].Length >= 9)
                {
                    string enrolled = entries[index][7];
                    return enrolled == "1" ? "Allowed PE Uniform" : "PE Uniform Not Allowed";
                }

                return string.Empty;
            }
            string GetPrac(List<string[]> entries, int index)
            {
                if (index < entries.Count && entries[index].Length >= 9)
                {
                    string enrolled = entries[index][8];
                    return enrolled == "1" ? "Allowed OJT Uniform" : "OJT Uniform Not Allowed";
                }

                return string.Empty;
            }
            string GetTypeC(List<string[]> entries, int index)
            {
                if (index < entries.Count && entries[index].Length >= 9)
                {
                    string enrolled = entries[index][9];
                    return enrolled == "1" ? "Allowed Nursing Type C" : "Nursing Type C Not Allowed";
                }

                return string.Empty;
            }
            string GetOsa(List<string[]> entries, int index)
            {
                if (index < entries.Count && entries[index].Length >= 9)
                {
                    //string enrolled = entries[index][10];
                    //return enrolled == "1" ? "Has Osa Violation" : "";
                    //return entries[index][10];
                    return "";
                }

                return string.Empty;
            }
            void SetLabelColor(System.Windows.Forms.Label label, string status)
            {
                if (status == "2")
                {
                    label.ForeColor = Color.Black; // Enrolled
                }
                else
                {
                    label.ForeColor = Color.Red; // Not Enrolled
                }
            }
            void SetLabelColorUni(System.Windows.Forms.Label label, string status)
            {
                if (status == "1")
                {
                    label.ForeColor = Color.Black; // Enrolled
                }
                else
                {
                    label.ForeColor = Color.Red; // Not Enrolled
                }
            }

        void SetLabelColorOsa(System.Windows.Forms.Label label, Panel panel, string status, int index)
            {
                switch (status)
                {
                    case "1":
                        label.ForeColor = Color.Red; // Enrolled
                        panel.BackColor = Color.Gold;// Yellow
                        break;
                    case "2":
                        label.ForeColor = Color.Red; // Enrolled
                        panel.BackColor = Color.Orange;// Light orange
                    break;
                    case "3":
                        label.ForeColor = Color.Red; // Enrolled
                        panel.BackColor = Color.Pink; // Light red
                        break;
                    default:
                        label.ForeColor = Color.Black; // Not Enrolled
                        panel.BackColor = (index == 0) ? Color.LightBlue : Color.Transparent;
                        break;
                }
            }



            // Helper method to format the entry name
            string FormatEntry(List<string[]> entries, int index)
            {
                if (index < entries.Count && entries[index] != null && entries[index].Length >= 3)
                {
                    string firstName = entries[index][0];
                    string middleInitial = !string.IsNullOrEmpty(entries[index][1]) ? entries[index][1] + ". " : string.Empty;
                    string lastName = entries[index][2];
                    string suffix = !string.IsNullOrEmpty(entries[index][3]) ? entries[index][3] + " " : string.Empty;

                    if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(middleInitial) || !string.IsNullOrEmpty(lastName) || !string.IsNullOrEmpty(suffix))
                    {
                        if(index != 0)
                        {
                            return $"{firstName} {middleInitial} {lastName} {suffix}";
                        }
                        else
                        {
                            return $"{firstName} {middleInitial}";
                        }
                    }
                }
                return string.Empty;
            }

            // Helper method to format the time logged
            string FormatTime(List<string[]> entries, int index)
            {
                if (index < entries.Count && entries[index] != null && entries[index].Length >= 5)
                {
                    string dateString = entries[index][4];

                    if (DateTime.TryParse(dateString, out DateTime dateTime))
                    {
                        return dateTime.ToString("MMMM d, yyyy h:mm tt");
                    }
                }

                return string.Empty;
            }



            List<PictureBox> pictureBoxes = new List<PictureBox>
            {
                pictureBox1,
                pictureBox2,
                pictureBox3,
                pictureBox4,
                pictureBox5,
                pictureBox6,
                pictureBox7,
                pictureBox8,
                pictureBox9,
                pictureBox10,
                pictureBox11,
                pictureBox12,
            };
            // Define default image path
            string defaultImagePath = Path.Combine(Application.StartupPath, "id_person.jpg");

            for (int i = 0; i < pictureBoxes.Count; i++)
            {
                if (i < entryList.Count && entryList[i].Length >= 6 && !string.IsNullOrEmpty(entryList[i][5]))
                {
                    // Check if there is an image data available
                    byte[] imageBytes = Convert.FromBase64String(entryList[i][5]);
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        pictureBoxes[i].Image = Image.FromStream(ms);
                    }
                }
                else if (i < entryList.Count)
                {
                    // Load default image only if there is data in entryList but no image
                    byte[] defaultImageBytes = File.ReadAllBytes(defaultImagePath);
                    using (MemoryStream ms = new MemoryStream(defaultImageBytes))
                    {
                        pictureBoxes[i].Image = Image.FromStream(ms);
                    }
                }
                else
                {
                    // Clear the image if there is no data in entryList
                    pictureBoxes[i].Image = null;
                }
            }



        }

        /// <summary>
        /// "Infinite" loop that runs every N seconds. Good for checking for a heartbeat or updates.
        /// </summary>
        /// <param name="taskState">The cancellation token from our _cts field, passed in the StartNew call</param>


        private void OwnCodeCancelableTask_EveryNSeconds(object taskState)
        {
            var token = (CancellationToken)taskState;
            Console.WriteLine("Do the work that needs to happen every N seconds in this loop");

            while (!token.IsCancellationRequested)
            {
                ReadRFIDTag();

                // Check if a new RFID scan has occurred since the last iteration
                if (_newRFIDScan)
                {
                    if (_newRFIDDuplicate)
                    {
                        //await Task.Delay(TASK_ITERATION_DELAY_MS, token);
                    }
                    else
                    {
                        //await Task.Delay(TASK_ITERATION_DELAY_MS, token);
                    }

                }

                // Reset the flag for the next iteration
                _newRFIDScan = false;
            }
        }

        private void picBoxAdDULogo_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void flowLayoutFooter_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click_2(object sender, EventArgs e)
        {

        }

        private void lblStudentName_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_3(object sender, EventArgs e)
        {

        }

        private void tblLayoutHeader_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click_4(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_5(object sender, EventArgs e)
        {

        }

        private void panel7_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel8_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label15_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click_1(object sender, EventArgs e)
        {

        }

        private void label34_Click(object sender, EventArgs e)
        {

        }

        private void uniLabel2_Click(object sender, EventArgs e)
        {

        }

        private void enrollLabel1_Click(object sender, EventArgs e)
        {

        }

        private void peLabel1_Click(object sender, EventArgs e)
        {

        }
    }
}
