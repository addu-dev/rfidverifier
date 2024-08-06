using System;
using System.Windows.Forms;
using System.Timers;

namespace AdDU_Student_Verifier
{
    public partial class AutoCloseMessageBox : Form
    {
        private System.Timers.Timer _timer;

        public AutoCloseMessageBox(string message)
        {
            InitializeComponent();
            labelMessage.Text = message;

            _timer = new System.Timers.Timer(1000); // 2000 ms = 2 seconds
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            _timer.Dispose();
            this.Invoke(new Action(this.Close));
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.Activate();
        }
    }
}
