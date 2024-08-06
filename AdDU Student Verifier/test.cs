using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Media;

namespace AdDU_Student_Verifier
{
    public partial class Form1 : Form
    {
        // Source: https://riptutorial.com/csharp/example/8235/an-async-cancellable-polling-task-that-waits-between-iterations
        private const int TASK_ITERATION_DELAY_MS = 3000;
        private const int TASK_ITERATION_DELAY_MS2 = 1000;
        private CancellationTokenSource _cts;

        public Form1()
        {
            InitializeComponent();
            this._cts = new CancellationTokenSource();

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
                Console.WriteLine(d[0]);
                Console.WriteLine(d[1]);

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
        private void ChangeLabelText(Student student, string studentId)
        {
            //_newRFIDScan = (student != null);
            string fullName = student != null ? student.Fullname : string.Empty;
            string code = student != null ? student.Code : string.Empty;
            string isEnrolled = student != null ? Utilities.getYesOrNo(student.IsEnrolled) : string.Empty;
            string hasPe = student != null ? Utilities.getYesOrNo(student.HasPeToday) : string.Empty;
            string hasPracticum = student != null ? Utilities.getYesOrNo(student.HasPracticumToday) : string.Empty;
            string shouldWearNurseTypeC = student != null ? Utilities.getYesOrNo(student.ShouldNurseTypeCToday) : string.Empty;
            string hasOSAViolation = student != null ? "NO" : string.Empty;
            byte[] studentImage = student != null ? student.RawImage : null;
            DateTime currentTime = DateTime.Now;
            string barcode = studentId;
            
            if (student != null)
            {
                Database.SaveRFIDDATA(barcode, currentTime.ToString("MMMM dd, yyyy hh:mmtt"));
                _newRFIDScan = true;
                if (_currentCode == barcode)
                {
                    _newRFIDDuplicate = true;
                }
                else
                {
                    _currentCode = barcode;
                    _newRFIDDuplicate = false;
                    SystemSounds.Beep.Play();
                }
            }
            lblStudentName.Text = "Name: " + fullName;
            lblEnrolled.Text = "Enrolled: " + isEnrolled;
            lblPE.Text = "PE: " + hasPe;
            lblPracticum.Text = "Practicum: " + hasPracticum;
            lblNurseTypeC.Text = "Nurse Type C: " + shouldWearNurseTypeC;
            lblOSAViolation.Text = "OSA Violation: " + hasOSAViolation;
            lblScanTime.Text = currentTime.ToString("MMMM dd, yyyy hh:mmtt");
            if (studentImage != null)
            {
                using (MemoryStream ms = new MemoryStream(studentImage))
                {
                    try
                    {
                        // Create an Image from the MemoryStream
                        Image image = Image.FromStream(ms);

                        // Set the PictureBox's Image property to the retrieved image
                        picBoxStudent.Image = image;
                    }
                    catch (Exception ex)
                    {
                        // Handle any exceptions that may occur when converting the image
                        MessageBox.Show("An error occurred: " + ex.Message);
                    }
                }
            } else
            {
                picBoxStudent.Image = null;
            }
        }

        /// <summary>
        /// "Infinite" loop that runs every N seconds. Good for checking for a heartbeat or updates.
        /// </summary>
        /// <param name="taskState">The cancellation token from our _cts field, passed in the StartNew call</param>
        

        private async void OwnCodeCancelableTask_EveryNSeconds(object taskState)
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
                        await Task.Delay(TASK_ITERATION_DELAY_MS, token);
                    }
                    else
                    {
                        await Task.Delay(TASK_ITERATION_DELAY_MS2, token);
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
    }
}
