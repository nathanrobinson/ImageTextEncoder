using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ImageTextEncoder
{
    public static class TextEncoderDecoder
    {
        public static void Encode(Bitmap image, string text, int pixelsPerByte)
        {
            var bytes = new ASCIIEncoding().GetBytes(text);
            Encode(image, bytes, pixelsPerByte);
        }
        public static void Encode(Bitmap image, byte[] bytes, int pixelsPerByte)
        {
            var totalBytes = image.Height*image.Width/pixelsPerByte;
            if(totalBytes < bytes.Length)
                throw new ApplicationException("Image too small to encode text.");

            var byteList = bytes.ToList();
            byteList.AddRange(Enumerable.Repeat((byte)0, totalBytes - bytes.Length));
            byteList.Add(0);
            for (var c = 0; c < byteList.Count; c++)
            {
                if (ComputePixel(image, byteList, c, pixelsPerByte)) break;
            }
        }

        private static bool ComputePixel(Bitmap image, IReadOnlyList<byte> bytes, int c, int pixelsPerByte)
        {
            var b = bytes[c];
            var partial = b/pixelsPerByte;
            var last = b - (partial*(pixelsPerByte-1));
            var partialBytes = Enumerable.Repeat(partial, pixelsPerByte - 1).ToList();
            partialBytes.Add(last);
            for (var i = 0; i < pixelsPerByte; i++)
            {
                var x = ((c * pixelsPerByte) + i) % image.Width;
                var y = ((c * pixelsPerByte) + i) / image.Width;
                if (y >= image.Height)
                    return true;
                var color = image.GetPixel(x, y);
                var green = (color.R + color.B)/2;
                if (green >= 128)
                    green -= partialBytes[i];
                else
                    green += partialBytes[i];
                color = Color.FromArgb(color.A, color.R, green, color.B);
                image.SetPixel(x, y, color);
            }
            return false;
        }

        public static string Decode(Bitmap image, int pixelsPerByte)
        {
            return new ASCIIEncoding().GetString(DecodeBytes(image, pixelsPerByte));
        }

        public static byte[] DecodeBytes(Bitmap image, int pixelsPerByte)
        {
            var bytes = new List<byte>();
            var partials = new List<byte>();
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (ComputeByte(image, x, y, partials, bytes, pixelsPerByte)) break;
                }
            }
            return bytes.ToArray();
        }

        private static bool ComputeByte(Bitmap image, int x, int y, List<byte> partials, List<byte> bytes, int pixelsPerByte)
        {
            var color = image.GetPixel(x, y);
            var green = (color.R + color.B)/2;
            var partial = (byte) Math.Abs(color.G - green);
            partials.Add(partial);
            if (partials.Count == pixelsPerByte)
            {
                var item = (byte) partials.Sum(a => a);
                if (item == 0)
                    return true;
                bytes.Add(item);
                partials.Clear();
            }
            return false;
        }
    }
}