using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace ImageTextEncoder
{
    public static class TextEncoderDecoder
    {
        public static void FastEncode(Bitmap image, string text, int pixelsPerChar)
        {
            var bytes = new ASCIIEncoding().GetBytes(text);
            FastEncode(image, bytes, pixelsPerChar);
        }
        public static void FastEncode(Bitmap image, byte[] bytes, int pixelsPerChar)
        {
            var totalBytes = image.Height * image.Width / pixelsPerChar;
            if(totalBytes < bytes.Length)
                throw new ApplicationException("Image too small to encode text.");

            var byteList = bytes.ToList();
            byteList.AddRange(Enumerable.Repeat((byte)0, totalBytes - bytes.Length));
            byteList.Add(0);

            var bitmapData = image.LockBits( 
                new Rectangle(0, 0, image.Width, image.Height), 
                ImageLockMode.ReadWrite,
                image.PixelFormat);
            
            int length  = Math.Abs(bitmapData.Stride) * image.Height;
            var rgbValues = new byte[length];
            
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, rgbValues, 0, length);

            for (var c = 0; c < byteList.Count; c++)
            {
                if(!FastComputePixel(rgbValues, byteList[c], c, pixelsPerChar, Math.Abs(bitmapData.Stride) / bitmapData.Width))
                    break;
            }

            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, bitmapData.Scan0, length);
            image.UnlockBits(bitmapData);
        }

        private static bool FastComputePixel(byte[] rgbValues, byte b, int c, int pixelsPerChar, int bytesPerPixel)
        {
            var partial = b/pixelsPerChar;
            var last = b - (partial*(pixelsPerChar-1));
            var partialBytes = Enumerable.Repeat(partial, pixelsPerChar - 1).ToList();
            partialBytes.Add(last);
            for (var i = 0; i < pixelsPerChar; i++)
            {
                var pixel = ((c*pixelsPerChar) + i)*bytesPerPixel;
                if (pixel >= rgbValues.Length)
                    return false;

                var red = rgbValues[pixel];
                var blue = rgbValues[pixel + 2];

                var green = (red + blue)/2;
                if (green >= 128)
                    green -= partialBytes[i];
                else
                    green += partialBytes[i];

                rgbValues[pixel + 1] = (byte)green;
            }
            return true;
        }

        public static void Encode(Bitmap image, string text, int pixelsPerChar)
        {
            var bytes = new ASCIIEncoding().GetBytes(text).ToList();
            bytes.Add(0);
            for (var c = 0; c < bytes.Count; c++)
            {
                if (ComputePixel(image, bytes, c, pixelsPerChar)) break;
            }
        }
        public static void Encode(Bitmap image, byte[] bytes, int pixelsPerChar)
        {
            var totalBytes = image.Height * image.Width / pixelsPerChar;
            if (totalBytes < bytes.Length)
                throw new ApplicationException("Image too small to encode text.");

            var byteList = bytes.ToList();
            byteList.AddRange(Enumerable.Repeat((byte)0, totalBytes - bytes.Length));
            byteList.Add(0);

            for (var c = 0; c < byteList.Count; c++)
            {
                if (!ComputePixel(image, byteList, c, pixelsPerChar)) break;
            }
        }

        private static bool ComputePixel(Bitmap image, IReadOnlyList<byte> bytes, int c, int pixelsPerChar)
        {
            var b = bytes[c];
            var partial = b / pixelsPerChar;
            var last = b - (partial * (pixelsPerChar - 1));
            var partialBytes = Enumerable.Repeat(partial, pixelsPerChar - 1).ToList();
            partialBytes.Add(last);
            for (var i = 0; i < pixelsPerChar; i++)
            {
                var x = ((c * pixelsPerChar) + i) % image.Width;
                var y = ((c * pixelsPerChar) + i) / image.Width;
                if (y >= image.Height)
                    return false;
                var color = image.GetPixel(x, y);
                var green = (color.R + color.B) / 2;
                if (green >= 128)
                    green -= partialBytes[i];
                else
                    green += partialBytes[i];
                color = Color.FromArgb(color.A, color.R, green, color.B);
                image.SetPixel(x, y, color);
            }
            return true;
        }

        public static string FastDecode(Bitmap image, int pixelsPerChar)
        {
            return new ASCIIEncoding().GetString(FastDecodeBytes(image, pixelsPerChar));
        }
        public static byte[] FastDecodeBytes(Bitmap image, int pixelsPerChar)
        {
            var bitmapData = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly,
                image.PixelFormat);

            int length = Math.Abs(bitmapData.Stride) * image.Height;
            var rgbValues = new byte[length];

            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, rgbValues, 0, length);
            image.UnlockBits(bitmapData);

            var bytes = new List<byte>(length / pixelsPerChar);
            var partials = new List<byte>(pixelsPerChar);

            for (var c = 0; c < rgbValues.Length; c+=(Math.Abs(bitmapData.Stride) / bitmapData.Width))
            {
                if (!FastComputeByte(rgbValues, c, partials, bytes, pixelsPerChar))
                    break;
            }
            return bytes.ToArray();
        }

        private static bool FastComputeByte(byte[] rgbValues, int index, List<byte> partials, List<byte> bytes, int pixelsPerChar)
        {
            var red = rgbValues[index];
            var blue = rgbValues[index + 2];
            var green = (red + blue)/2;
            var partial = (byte)Math.Abs(rgbValues[index + 1] - green);
            partials.Add(partial);
            if (partials.Count == pixelsPerChar)
            {
                var item = (byte)partials.Sum(a => a);
                if (item == 0)
                    return false;
                bytes.Add(item);
                partials.Clear();
            }
            return true;
        }


        public static string Decode(Bitmap image, int pixelsPerChar)
        {
            return new ASCIIEncoding().GetString(DecodeBytes(image, pixelsPerChar));
        }
        public static byte[] DecodeBytes(Bitmap image, int pixelsPerChar)
        {
            var bytes = new List<byte>();
            var partials = new List<byte>();
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (!ComputeByte(image, x, y, partials, bytes, pixelsPerChar)) break;
                }
            }
            return bytes.ToArray();
        }

        private static bool ComputeByte(Bitmap image, int x, int y, List<byte> partials, List<byte> bytes, int pixelsPerChar)
        {
            var color = image.GetPixel(x, y);
            var green = (color.R + color.B)/2;
            var partial = (byte) Math.Abs(color.G - green);
            partials.Add(partial);
            if (partials.Count == pixelsPerChar)
            {
                var item = (byte) partials.Sum(a => a);
                if (item == 0)
                    return false;
                bytes.Add(item);
                partials.Clear();
            }
            return true;
        }
    }
}