using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ImageTextEncoder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            encoderModelBindingSource.AddNew();
            encoderModelBindingSource.MoveFirst();
        }

        private void OpenToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                ((EncoderModel)encoderModelBindingSource.Current).Image = (Bitmap)Image.FromFile(openFileDialog1.FileName);
            pbImage.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void SaveToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                ((EncoderModel) encoderModelBindingSource.Current).Image.Save(saveFileDialog1.FileName, ImageFormat.MemoryBmp);
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            Close();
        }

        private void EncodeToolStripMenuItemClick(object sender, EventArgs e)
        {
            ((EncoderModel) encoderModelBindingSource.Current).Encode();
        }

        private void DecodeToolStripMenuItemClick(object sender, EventArgs e)
        {
            ((EncoderModel) encoderModelBindingSource.Current).Decode();
        }

        private void TextToolStripMenuItemDropDownOpening(object sender, EventArgs e)
        {
            encoderModelBindingSource.EndEdit();
        }
    }
}
