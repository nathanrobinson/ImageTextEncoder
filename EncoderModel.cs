using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

namespace ImageTextEncoder
{
    public class EncoderModel : INotifyPropertyChanged
    {
        public EncoderModel()
        {
            PixelsPerByte = 32;
            Text = string.Empty;
            Image = new Bitmap(1,1);
        }
        private string _text;
        private Bitmap _image;
        private int _pixelsPerByte;
        private string _password;
        private string _runTime;

        public string Text
        {
            get { return _text; }
            set
            {
                if (_text == value)
                    return;
                _text = value;
                OnPropertyChanged("Text");
            }
        }

        public Bitmap Image
        {
            get { return _image; }
            set
            {
                if (_image == value)
                    return;
                _image = value;
                OnPropertyChanged("Image");
                OnPropertyChanged("MaxLength");
            }
        }

        public int MaxLength
        {
            get
            {
                return _image == null
                           ? int.MaxValue
                           : (_image.Width*_image.Height/PixelsPerByte) > int.MaxValue
                                 ? int.MaxValue
                                 : (_image.Width*_image.Height/PixelsPerByte);
            }
            set
            {}
        }

        public int PixelsPerByte
        {
            get { return _pixelsPerByte; }
            set
            {
                if (_pixelsPerByte == value)
                    return;
                _pixelsPerByte = value;
                OnPropertyChanged("PixelsPerByte");
            }
        }

        public string Password
        {
            get { return _password; }
            set
            {
                if (_password == value)
                    return;
                _password = value;
                OnPropertyChanged("Password");
            }
        }

        public string RunTime
        {
            get { return _runTime; }
            set
            {
                if (_runTime == value)
                    return;
                _runTime = value;
                OnPropertyChanged("RunTime");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Encode()
        {
            var sw = new Stopwatch();
            sw.Start();
            TextEncoderDecoder.FastEncode(Image, Text, PixelsPerByte);
            sw.Stop();
            RunTime = sw.ElapsedMilliseconds.ToString() + " ms";
            OnPropertyChanged("Image");
        }

        public void Decode()
        {
            var sw = new Stopwatch();
            sw.Start();
            Text = TextEncoderDecoder.FastDecode(Image, PixelsPerByte);
            sw.Stop();
            RunTime = sw.ElapsedMilliseconds.ToString() + " ms";
        }

        public void Encrypt()
        {
            TextEncoderDecoder.Encode(Image, TextEncryptorDecryptor.EncryptStringAES(Text, Password), PixelsPerByte);
            OnPropertyChanged("Image");
        }

        public void Decrypt()
        {
            Text = TextEncryptorDecryptor.DecryptStringAES(TextEncoderDecoder.Decode(Image, PixelsPerByte), Password);
        }
    }
}