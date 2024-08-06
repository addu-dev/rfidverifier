using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Text;

namespace AdDU_Student_Verifier
{
    internal class Utilities
    {
        public Utilities() { }

        public static string getTransformedData(byte[] data, int s, int e)
        {
            string strData = string.Empty;
            for (int i = 0; i < e; i++)
            {
                if (data[s + i] < 0)
                    data[s + i] = Convert.ToByte(Convert.ToInt32(data[s + i]) + 256);
            }

            for (int i = 0; i < e; i++)
            {
                strData += data[s + i].ToString("X2") + " ";
            }

            return strData;
        }

        public static string ConvertHex(String hexString)
        {
            try
            {
                string ascii = string.Empty;
                string[] hexGroup = hexString.Split(' ');

                for (int i = 0; i < hexGroup.Length - 1; i++)
                {
                    uint decval = Convert.ToUInt32(hexGroup[i], 16);
                    char character =Convert.ToChar(decval);

                    if (decval < 128)
                        ascii += character;
                }

                return ascii;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static byte[] convertSNR(string str, int keyN)
        {
            string regex = "[^a-fA-F0-9]";
            string tmpJudge = Regex.Replace(str, regex, "");

            //长度不对，直接退回错误
            if (tmpJudge.Length != 12) return null;

            string[] tmpResult = Regex.Split(str, regex);
            byte[] result = new byte[keyN];
            int i = 0;
            foreach (string tmp in tmpResult)
            {
                result[i] = Convert.ToByte(tmp, 16);
                i++;
            }
            return result;
        }

        public static string FormatErrorCode(byte[] byteArray)
        {
            string strErrorCode = "";
            switch (byteArray[0])
            {
                case 0x80:
                    strErrorCode = "Success";
                    break;

                case 0x81:
                    strErrorCode = "Parameter Error";
                    break;

                case 0x82:
                    strErrorCode = "communication TimeOut";
                    break;

                case 0x83:
                    strErrorCode = "Couldn't Find Card ";
                    break;

                default:
                    strErrorCode = "Command Error";
                    break;
            }

            return strErrorCode;
        }

        private static byte[] StringToByteArray(string hex)
        {
            if (hex == null) return null;

            if (hex.Length == 0) return null;

            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public static Image GetImage(string rawImage)
        {
            // Replace this with your raw JPEG image data in the "0xFFD8FFE0..." format.
            byte[] rawImageData = StringToByteArray(rawImage);

            Image image = null;

            if (rawImageData != null && rawImageData.Length > 0)
            {
                // Create a MemoryStream from the raw image data.
                using (MemoryStream ms = new MemoryStream(rawImageData))
                {
                    try
                    {
                        // Create an Image object from the MemoryStream.
                        image = Image.FromStream(ms);
                        Console.WriteLine("Here");
                        return image;
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(ex.Message);
                        return image;
                    }
                }
            }
            else
            {
                return image;
            }
        }

        public static Bitmap byteToImage(string blob)
        {
            MemoryStream mStream = new MemoryStream();
            byte[] pData = Encoding.ASCII.GetBytes(blob);
            Console.WriteLine(pData);
            mStream.Write(pData, 0, Convert.ToInt32(pData.Length));
            Bitmap bm = new Bitmap(mStream, false);
            mStream.Dispose();
            return bm;
        }

        public static Image StringToImage(string inputString)
        {
            byte[] imageBytes = Encoding.Unicode.GetBytes(inputString);

            // Don't need to use the constructor that takes the starting offset and length
            // as we're using the whole byte array.
            MemoryStream ms = new MemoryStream(imageBytes);

            Image image = Image.FromStream(ms, true, true);

            return image;
        }

        public static string getYesOrNo(bool condition)
        {
            return condition ? "YES" : "NO";
        }
    }
}
