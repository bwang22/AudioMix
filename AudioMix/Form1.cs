using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using NAudio;
using NAudio.Wave;
using System.Runtime.InteropServices;

namespace AudioMix
{
    public partial class Form1 : Form
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect, // x-coordinate of upper-left corner
            int nTopRect, // y-coordinate of upper-left corner
            int nRightRect, // x-coordinate of lower-right corner
            int nBottomRect, // y-coordinate of lower-right corner
            int nWidthEllipse, // height of ellipse
            int nHeightEllipse // width of ellipse
         );

        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));
        
        }
        public static void Mp3ToWav(string mp3File, string outputFile)
        {
            using (Mp3FileReader reader = new Mp3FileReader(mp3File))
            {
                WaveFileWriter.CreateWaveFile(outputFile, reader);
            }
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            waveViewer1.Refresh();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string outputFileName = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Wave File (*.wav) | *.wav|MP3 Files (*.mp3) | *.mp3|All Files (*.*) | *.*";
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                outputFileName = openFileDialog.FileName;
            }
            else 
                return;
            if (openFileDialog.FileName.Contains(".mp3"))
            {
                outputFileName = outputFileName.Substring(0, outputFileName.Length - 3) + "wav";
                Mp3ToWav(openFileDialog.FileName, outputFileName);
            }

            //OpenFileDialog open = new OpenFileDialog();
            //open.Filter = "Wave File (*.wav) | *.wav";
            //if (open.ShowDialog() != DialogResult.OK) return;
         
            waveViewer1.WaveStream = new NAudio.Wave.WaveFileReader(outputFileName);
            waveViewer1.GetTotal = true;
            waveViewer1.Refresh();

            var datalow = waveViewer1.Datax;
            var datahigh = waveViewer1.Datay;
            //take the data and look for patterns in the frame of 8 secs
            var peaks = detectpeak(200, datahigh, true);
            var trough = detectpeak(200, datahigh, false);
            peaks = filterpeak(20, peaks);
            trough = filterpeak(20, trough);
            waveViewer1.peaks = peaks;
            waveViewer1.trough = trough;

            double[][] highamp = new double[peaks.Count][]; 
            for(int a = 0; a < peaks.Count-1; a++)
            {
                highamp[a] = new double[2];
                highamp[a][0] = peaks[a+1] - peaks[a];
                highamp[a][1] = datahigh[peaks[a]];

            }
            highamp[peaks.Count - 1] = new double[2];
            highamp[peaks.Count - 1][0] = 0; highamp[peaks.Count - 1][1] = 0;
            double[][] lowamp = new double[trough.Count][]; 
            for (int a = 0; a < trough.Count-1; a++)
            {
                lowamp[a] = new double[2];
                lowamp[a][0] = trough[a+1] - trough[a];
                lowamp[a][1] = datahigh[trough[a]];

            }
            lowamp[trough.Count - 1] = new double[2];
            lowamp[trough.Count - 1][0] = 0; lowamp[trough.Count - 1][1] = 0;
            //cluster both

            Accord.MachineLearning.KMeans gm = new Accord.MachineLearning.KMeans(5);
            Accord.MachineLearning.KMeans gml = new Accord.MachineLearning.KMeans(5);
            var ans = gm.Compute(highamp);
            var lans = gml.Compute(lowamp);

            var fclus = filtercluster(ans, peaks);
            cluspos = MixerControls.ToDict(fclus);
            var flans = filtercluster(lans, trough);//ignore bot

            waveViewer1.peakclus = ans;
            waveViewer1.troughclus = lans;

            IWavePlayer play;
            
            play = new NAudio.Wave.WaveOut();
            audio = new AudioFileReader(outputFileName); 
            play.Init(audio);
            play.Play();
            Application.Idle += Application_Idle;
            timer1.Interval = 20;
            timer1.Enabled = true;
            timer1.Start();
            Rectangle workingArea = Screen.GetWorkingArea(this);
            this.Location = new Point(workingArea.Right - Size.Width-20,
                                      workingArea.Bottom - Size.Height - 20);
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.TopMost = true;
            this.Opacity = .8;
            openToolStripMenuItem.Visible = false;
            menuStrip1.Visible = false;
            this.pictureBox1.Image = Image.FromFile("D:\\AllVSProject232015\\AudioMix\\AudioMix\\Hypnoctivity-Logo.png");
        }

        void Application_Idle(object sender, EventArgs e)
        {
            audio.Position = MixerControls.Jump(cluspos, audio.Position, waveViewer1.peaks, waveViewer1.peakclus, ref nextvalue, ref currentposs);
        }
        AudioFileReader audio;
        Dictionary<int, List<int>> cluspos = new Dictionary<int, List<int>>();
        Dictionary<int, List<int>> clusposl = new Dictionary<int, List<int>>();
        int nextvalue = 0;
        int currentposs = 0;

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!(audio.Length > audio.Position))
                Application.Exit();
            waveViewer1.StartPosition = audio.Position / 2;
            waveViewer1.Refresh();
           
        }
        private List<int> detectpeak(int slidingwindowsize, List<float> data, bool peak)
        {
            List<int> peaks = new List<int>();
            for (int a = 0; a < data.Count - slidingwindowsize; a++)
            {
                var window = data.GetRange(a, slidingwindowsize);
                if(peak)
                {
                    int index = window.IndexOf(window.Max()) + a;
                    if(!peaks.Contains(index))
                        peaks.Add(index);
                }
                else
                {
                    int index = window.IndexOf(window.Min()) + a;
                    if (!peaks.Contains(index))
                        peaks.Add(index);
                }
            }
            return peaks;
        }
        private List<int> filterpeak(int distance, List<int> data)
        {
            List<int> newdata = new List<int>();
            for(int a = 0; a < data.Count - 1; a++)
            {
                if (data[a + 1] - data[a] > distance)
                    newdata.Add(data[a]);
            }
            return newdata;
        }
        private List<int[]> filtercluster(int[] dataa, List<int> peaks)
        {
            List<int[]> newdata = new List<int[]>();
            List<int> datt = new List<int>(); datt.Add(dataa[0]);
            for (int a = 1; a < dataa.Length - 1; a++)
            {
                if(!(dataa[a] != dataa[a - 1] && dataa[a] != dataa[a + 1]))
                {
                    datt.Add(dataa[a]);
                }
            }
            datt.Add(dataa[dataa.Length - 1]);
            int[] data = datt.ToArray();
            for (int a = 0; a < data.Length; a++)
            {
                int[] d = new int[2];
                if (newdata.Count == 0)
                {
                    d[0] = peaks[a];
                    d[1] = data[a];
                    newdata.Add(d);
                }
                else if (newdata[newdata.Count - 1][1] != data[a])
                {
                    d[0] = peaks[a];
                    d[1] = data[a];
                    newdata.Add(d);
                }
            }
            return newdata;
        }
    }
}
