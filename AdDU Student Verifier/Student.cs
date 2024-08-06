using System;
using System.Drawing;

namespace AdDU_Student_Verifier
{
    internal class Student
    {
        private string code;
        private string firstname;
        private string lastname;
        private bool isEnrolled;
        private byte[] rawImage;
        private bool hasPeToday;
        private bool hasPracticumToday;
        private bool shouldWearTypeCToday;

        public string Code { 
            get { return code; }  
            set { code = value; }
        }

        public string Firstname
        {
            get { return firstname; }
            set { firstname = value; }
        }

        public string Lastname
        {
            get { return lastname; }
            set { lastname = value; }
        }

        public string Fullname
        {
            get { return firstname + " " + lastname; }
        }

        public bool IsEnrolled
        {
            get { return isEnrolled; }
            set { isEnrolled = value; }
        }

        public byte[] RawImage
        {
            get { return rawImage; }
            set { rawImage = value; }
        }

        public bool HasPeToday
        {
            get { return hasPeToday; }
            set { hasPeToday = value; }
        }

        public bool HasPracticumToday
        {
            get { return hasPracticumToday; }
            set { hasPracticumToday = value; }
        }

        public bool ShouldNurseTypeCToday
        {
            get { return shouldWearTypeCToday; }
            set { shouldWearTypeCToday = value; }
        }

        public Student(string c, string fn, string ln, bool enrolled, byte[] rawImg, char peToday, char practicumToday, char nurseToday)
        {
            code = c;
            firstname = fn;
            lastname = ln;
            isEnrolled = enrolled;
            rawImage = rawImg;
            hasPeToday = peToday.Equals("1");
            hasPracticumToday = practicumToday.Equals("1");
            shouldWearTypeCToday = nurseToday.Equals("1");
        }
    }
}
