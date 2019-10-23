using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AffineTransformations
{
    public partial class Form1 : Form
    {

        public List<Polygon> scene = new List<Polygon>();
        public OrbitCamera OrbitCam = new OrbitCamera(200, 0, (float)Math.PI / 2, 0, new Point3D(0, 0, 0), (float)(65 * Math.PI / 180), (float)(65 * Math.PI / 180), 100, 300);

        public Form1()
        {
            InitializeComponent();
            Bitmap bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = bmp;
            comboBox2.SelectedIndex = 0;
            ControlType.SelectedIndex = 0;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.TranslateTransform(0, pictureBox1.Height);
            e.Graphics.ScaleTransform(1, -1);
            e.Graphics.DrawImageUnscaled(OrbitCam.CameraRender(pictureBox1, scene), new Point(0, 0));
            debuglabel.Text = String.Format("Camerea pos:\n AngleX:{0}\n AngleY:{1}\n Tilt:{2}\n Distance:{3} ", OrbitCam.AngleX * 180 / (float)Math.PI, OrbitCam.AngleY * 180 / (float)Math.PI, OrbitCam.AngleTilt * 180 / (float)Math.PI, OrbitCam.Distance);
        }

        private void RotatePolygon(Polygon f, float ang, string type)
        {
            switch (type)
            {
                case "CenterX":
                    f.RotateAround(ang, "CX");
                    break;
                case "CenterY":
                    f.RotateAround(ang, "CY");
                    break;
                case "CenterZ":
                    f.RotateAround(ang, "CZ");
                    break;
                case "X axis":
                    f.RotateAround(ang, "X");
                    break;
                case "Y axis":
                    f.RotateAround(ang, "Y");
                    break;
                case "Z asix":
                    f.RotateAround(ang, "Z");
                    break;
                case "Custom Line":
                    f.line_rotate(ang, new Point3D((float)ControlCustom1X.Value, (float)ControlCustom1Y.Value, (float)ControlCustom1Z.Value),
                                        new Point3D((float)ControlCustom2X.Value, (float)ControlCustom2Y.Value, (float)ControlCustom2Z.Value));
                    break;
                default:
                    break;
            }

        }

        private void ApplyTransfroms()
        {
            float ox = (float)ControlOffsetX.Value;
            float oy = (float)ControlOffsetY.Value;
            float oz = (float)ControlOffsetZ.Value;
            float sx = (float)ControlScaleX.Value / 10;
            float sy = (float)ControlScaleY.Value / 10;
            float sz = (float)ControlScaleZ.Value / 10;
            float an = (float)ControlAngle.Value;
            Polygon f = scene[2];


            RotatePolygon(f, an, ControlType.Text);
            ScalePolygon(f, sx, sy, sz, ControlType.Text);
            f.offset(ox, oy, oz);
        }

        private void ScalePolygon(Polygon f, float sx, float sy, float sz, string type)
        {
            switch (type)
            {
                case "CenterX":
                case "CenterY":
                case "CenterZ":
                    f.scale_around_center(sx, sy, sz);
                    break;
                case "X axis":
                case "Y axis":
                case "Z asix":
                    f.scale_axis(sx, sy, sz);
                    break;
                default:
                    break;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            pictureBox1.Invalidate();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            groupBox1.Enabled = false;
            ApplyTransfroms();
            groupBox1.Enabled = true;
            pictureBox1.Invalidate();
        }


        //Выбрали другую фигуру
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            scene.Clear();
            scene.Add(Polygon.getCoordinates());
            scene.Add(GenerateLine());
            switch (comboBox2.Text)
            {
                case "Tetrahedron":
                    scene.Add(Polygon.get_Tetrahedron(100));//тетраэдр
                    break;
                case "Octahedron":
                    scene.Add(Polygon.Octahedron(100));//октаэдр
                    break;
                case "Icosahedron":
                    scene.Add(Polygon.get_Icosahedron(50));//икосаэдр
                    break;
                default:
                    break;

            }
            ResetControls();
            pictureBox1.Invalidate();
        }

        /// Reset controls for Aphine transforms
        private void ResetControls()
        {
            ControlOffsetX.Value = 0;
            ControlOffsetY.Value = 0;
            ControlOffsetZ.Value = 0;
            ControlScaleX.Value = 10;
            ControlScaleY.Value = 10;
            ControlScaleZ.Value = 10;
            ControlAngle.Value = 0;
        }

        private void AphineResetButton_Click(object sender, EventArgs e)
        {
            ResetControls();
        }

        ///  Generates line figure, reads params from controls
        private Polygon GenerateLine()
        {
            Polygon res = new Polygon();
            res.points.Add(new Point3D((float)ControlCustom1X.Value, (float)ControlCustom1Y.Value, (float)ControlCustom1Z.Value));
            res.points.Add(new Point3D((float)ControlCustom2X.Value, (float)ControlCustom2Y.Value, (float)ControlCustom2Z.Value));
            res.sides.Add(new Edge(res));
            res.sides.First().points = new List<int> { 0, 1 };
            return res;
        }

        ///  handles changes of custom line
        private void ControlCustom1X_ValueChanged(object sender, EventArgs e)
        {
            scene[1] = GenerateLine();
            pictureBox1.Invalidate();
        }

        /// Handles keyboard controls for camera
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // camera movemetns
            switch (e.KeyCode)
            {
                case Keys.A: // move left by 1d
                    OrbitCam.MoveLeftRight((float)(Math.PI / 180));
                    break;
                case Keys.D: // move right by 1d
                    OrbitCam.MoveLeftRight(-(float)(Math.PI / 180));
                    break;
                case Keys.W: //move up by 1d
                    OrbitCam.MoveUpDown((float)(Math.PI / 180));
                    break;
                case Keys.S: // move down by 1d
                    OrbitCam.MoveUpDown(-(float)(Math.PI / 180));
                    break;
                case Keys.Q: // rotate left
                    OrbitCam.TiltLeftRight((float)(Math.PI / 180));
                    break;
                case Keys.E: // rotate right
                    OrbitCam.TiltLeftRight(-(float)(Math.PI / 180));
                    break;
                case Keys.R: //move closer 
                    OrbitCam.MoveFarNear(-1);
                    break;
                case Keys.F: // move further
                    OrbitCam.MoveFarNear(1); ;
                    break;
                default:
                    break;
            }
            pictureBox1.Invalidate();
        }

        //Reset camera
        private void CamReset_Click(object sender, EventArgs e)
        {
            OrbitCam.Reset();
            debuglabel.Text = String.Format("Camerea pos:\n AngleX:{0}\n AngleY:{1}\n Tilt:{2}\n Distance:{3} ", OrbitCam.AngleX * 180 / (float)Math.PI, OrbitCam.AngleY * 180 / (float)Math.PI, OrbitCam.AngleTilt * 180 / (float)Math.PI, OrbitCam.Distance);
            pictureBox1.Invalidate();
        }
    }


}
