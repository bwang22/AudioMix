using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;
using NAudio.Wave;

namespace AudioMix
{
    /// <summary>
    /// Control for viewing waveforms
    /// </summary>
    public class WaveViewer : System.Windows.Forms.UserControl
    {
        public Color PenColor { get; set; }
        public float PenWidth { get; set; }
        public List<float> Datax = new List<float>();
        public List<float> Datay = new List<float>();
        public bool GetTotal { get; set; }
        public List<int> peaks { get; set; }
        public List<int> trough { get; set; }
        public int[] peakclus { get; set; }
        public int[] troughclus { get; set; }
        public int peakcounter = 0;
        public int troughcounter = 0;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private WaveStream waveStream;
        private int samplesPerPixel = 128;
        public long startPosition;
        private int bytesPerSample;
        /// <summary>
        /// Creates a new WaveViewer control
        /// </summary>
        public WaveViewer()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
            this.DoubleBuffered = true;

            this.PenColor = Color.DodgerBlue;
            this.PenWidth = 1;
            this.GetTotal = false;
        }

        /// <summary>
        /// sets the associated wavestream
        /// </summary>
        public WaveStream WaveStream
        {
            get
            {
                return waveStream;
            }
            set
            {
                waveStream = value;
                if (waveStream != null)
                {
                    bytesPerSample = (waveStream.WaveFormat.BitsPerSample / 8) * waveStream.WaveFormat.Channels;
                }
                this.Invalidate();
            }
        }

        /// <summary>
        /// The zoom level, in samples per pixel
        /// </summary>
        public int SamplesPerPixel
        {
            get
            {
                return samplesPerPixel;
            }
            set
            {
                samplesPerPixel = value;
                this.Invalidate();
            }
        }

        /// <summary>
        /// Start position (currently in bytes)
        /// </summary>
        public long StartPosition
        {
            get
            {
                return startPosition;
            }
            set
            {
                startPosition = value;
            }
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// <see cref="Control.OnPaint"/>
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            Datax.Clear();
            Datay.Clear();
            if (waveStream != null)
            {
                waveStream.Position = 0;
                int bytesRead;
                byte[] waveData = new byte[samplesPerPixel * bytesPerSample];
                waveStream.Position = startPosition + (e.ClipRectangle.Left * bytesPerSample * samplesPerPixel);
                using (Pen linePen = new Pen(PenColor, PenWidth))
                {
                    for (float x = e.ClipRectangle.X; x < e.ClipRectangle.Right; x += 1)
                    {
                        short low = 0;
                        short high = 0;
                        bytesRead = waveStream.Read(waveData, 0, samplesPerPixel * bytesPerSample);
                        if (bytesRead == 0)
                            break;
                        for (int n = 0; n < bytesRead; n += 2)
                        {
                            short sample = BitConverter.ToInt16(waveData, n);
                            if (sample < low) low = sample;
                            if (sample > high) high = sample;
                        }
                        float lowPercent = ((((float)low) - short.MinValue) / ushort.MaxValue);
                        float highPercent = ((((float)high) - short.MinValue) / ushort.MaxValue);
                        Datax.Add(low);
                        Datay.Add(high);
                        int pos = (int)(waveStream.Position / 512);
                        if (this.peaks != null && this.peakclus != null)
                        {
                            int index = peaks.IndexOf(pos);
                            bool clusflag = true;
                            if (index != -1)
                            {
                                Color c = new Color();
                                if (peakclus[index] == 0)
                                    c = Color.Black;
                                else if (peakclus[index] == 1)
                                    c = Color.Yellow;
                                else if (peakclus[index] == 2)
                                    c = Color.Brown;
                                else if (peakclus[index] == 3)
                                    c = Color.Red;
                                else
                                    c = Color.Olive;
                                e.Graphics.DrawLine(new Pen(c, 3), x + 25, this.Height * lowPercent, x + 25, this.Height * highPercent);
                                clusflag = false;
                            }
                            /*index = trough.IndexOf(pos);
                            if (index != -1)
                            {
                                Color c = new Color();
                                if (troughclus[index] == 0)
                                    c = Color.Teal;
                                else if (troughclus[index] == 1)
                                    c = Color.Green;
                                else if (troughclus[index] == 2)
                                    c = Color.HotPink;
                                else if (troughclus[index] == 3)
                                    c = Color.Navy;
                                else
                                    c = Color.HotPink;
                                e.Graphics.DrawLine(new Pen(c, 3), x + 25, this.Height * lowPercent, x + 25, this.Height * highPercent);
                                clusflag = false;
                            }*/
                            if(clusflag)
                                e.Graphics.DrawLine(linePen, x + 25, this.Height * lowPercent, x + 25, this.Height * highPercent);
                        }
                    }
                    if (this.GetTotal)
                    {
                        for (float x = e.ClipRectangle.Right; x < 20000000; x++)
                        {
                            short low = 0;
                            short high = 0;
                            bytesRead = waveStream.Read(waveData, 0, samplesPerPixel * bytesPerSample);
                            if (bytesRead == 0)
                                break;
                            for (int n = 0; n < bytesRead; n += 2)
                            {
                                short sample = BitConverter.ToInt16(waveData, n);
                                if (sample < low) low = sample;
                                if (sample > high) high = sample;
                            }
                            float lowPercent = ((((float)low) - short.MinValue) / ushort.MaxValue);
                            float highPercent = ((((float)high) - short.MinValue) / ushort.MaxValue);
                            Datax.Add(low);
                            Datay.Add(high);
                        }
                        GetTotal = false;
                    }
                    e.Graphics.DrawLine(new Pen(Color.Red, 1), 0, this.Height / 2, this.Width, this.Height / 2);
                    e.Graphics.DrawLine(new Pen(Color.Red, 1), 25, 0, 25, this.Height);
                }
            }

            base.OnPaint(e);
        }


        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }
        #endregion
    }
}