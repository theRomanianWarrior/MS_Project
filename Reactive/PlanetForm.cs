using System;
using System.Drawing;
using System.Timers;
using System.Windows.Forms;

namespace Reactive
{
    public partial class PlanetForm : Form
    {
        private CoordinatorAgent _ownerAgent;
        private Bitmap _doubleBufferImage;
        private Graphics g;
        private Brush lastColor = Brushes.Red;
        private bool keepChristmas = false;
        public PlanetForm()
        {
            InitializeComponent();
        }

        public void SetOwner(CoordinatorAgent a)
        {
            _ownerAgent = a;
        }

        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            DrawPlanet();
        }

        public void UpdatePlanetGUI(bool beginChristmasAlert = false)
        {
            DrawPlanet(beginChristmasAlert);
        }

        private void pictureBox_Resize(object sender, EventArgs e)
        {
            DrawPlanet();
        }

        private void DrawPlanet(bool beginChristmasAlert = false)
        {
            try
            {
                int w = pictureBox.Width;
                int h = pictureBox.Height;

                if (_doubleBufferImage != null)
                {
                    _doubleBufferImage.Dispose();
                    GC.Collect(); // prevents memory leaks
                }

                _doubleBufferImage = new Bitmap(w, h);
                g = Graphics.FromImage(_doubleBufferImage);
                g.Clear(Color.White);

                int minXY = Math.Min(w, h);
                int cellSize = (minXY - 40) / Utils.Size;

                for (int i = 0; i <= Utils.Size; i++)
                {
                    g.DrawLine(Pens.DarkGray, 20, 20 + i * cellSize, 20 + Utils.Size * cellSize, 20 + i * cellSize);
                    g.DrawLine(Pens.DarkGray, 20 + i * cellSize, 20, 20 + i * cellSize, 20 + Utils.Size * cellSize);
                }

                if (beginChristmasAlert)
                {
                    lastColor = lastColor == Brushes.Blue ? Brushes.Red : Brushes.Blue;
                    keepChristmas = true;
                }
                
                if(keepChristmas)
                    lastColor = lastColor == Brushes.Blue ? Brushes.Red : Brushes.Blue;

                g.FillEllipse(lastColor, 20 + Utils.Size / 2 * cellSize + 4, 20 + Utils.Size / 2 * cellSize + 4, cellSize - 8, cellSize - 8); // the base

                if (_ownerAgent != null)
                {
                    foreach (string v in _ownerAgent.EvacuationAgentsPositions.Values)
                    {
                        string[] t = v.Split();
                        int x = Convert.ToInt32(t[0]);
                        int y = Convert.ToInt32(t[1]);

                        g.FillEllipse(Brushes.Blue, 20 + x * cellSize + 6, 20 + y * cellSize + 6, cellSize - 12, cellSize - 12);
                    }

                    foreach (string v in _ownerAgent.ExitsPositions.Values)
                    {
                        string[] t = v.Split();
                        int x = Convert.ToInt32(t[0]);
                        int y = Convert.ToInt32(t[1]);

                        g.FillRectangle(Brushes.LightGreen, 20 + x * cellSize + 10, 20 + y * cellSize + 10, cellSize - 20, cellSize - 20);
                    }
                }

                Graphics pbg = pictureBox.CreateGraphics();
                pbg.DrawImage(_doubleBufferImage, 0, 0);
            }
            catch (Exception e)
            {
                Console.WriteLine("At DrawPlanet "+ e);
            }
        }
    }
}