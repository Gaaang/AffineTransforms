using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AffineTransformations
{
    public class CameraView
    {
        private Point3D view_target;
        private Point3D eye_postion;
        private Point3D up;
        private float fovx;
        private float fovy;
        private float max_distance;
        private float min_distance;
        private float cam_width;
        private float cam_height;
        private float[,] view_matrix;
        private float[,] perspective_projection_matrix;
        private float[,] orthoganal_projection_matrix;
        private float[,] complete_matrix_perspective;
        private float[,] complete_matrix_orthoganal;
        private bool isorthg = false;
        public bool istexture = false;

        /// Basic Camera object
        public CameraView(Point3D p, Point3D t, Point3D u, float fvx, float fvy, float mind, float maxd)
        {
            view_target = new Point3D(t);
            eye_postion = new Point3D(p);
            up = new Point3D(u);
            fovx = fvx;
            fovy = fvy;
            max_distance = maxd;
            min_distance = mind;
            cam_width = 100;
            cam_height = 100;
            update_view_matrix();
            update_proj_matrix();
            update_full_matrix();
        }

        ///  Set all params at once and recount matrixes only ones
        protected void set_params_at_once(Point3D p, Point3D t, Point3D u, float fvx, float fvy, float mind, float maxd)
        {
            view_target = new Point3D(t);
            eye_postion = new Point3D(p);
            up = new Point3D(u);
            fovx = fvx;
            fovy = fvy;
            max_distance = maxd;
            min_distance = mind;
            cam_width = 100;
            cam_height = 100;
            update_view_matrix();
            update_proj_matrix();
            update_full_matrix();
        }

        public Bitmap CameraRender(PictureBox rend_obj, List<Polygon> scene)
        {
            point3 ViewPortTranform(Point3D p, float l)
            {
                return new point3((int)((1 + p.X) * rend_obj.Width / 2),
                                  (int)((1 + p.Y) * rend_obj.Height / 2),
                                  (int)(1 / p.Z * 100000000), l);
            }

            List<Polygon> view = scene.Select(f => new Polygon(f)).ToList();
            int h = rend_obj.Height;
            int w = rend_obj.Width;

            int[,] zbuffer = new int[h, w];
            Color[,] cbuffer = new Color[h, w];
            for (int i = 0; i < h; i++)
                for (int j = 0; j < w; j++)
                {
                    zbuffer[i, j] = 0;
                    cbuffer[i, j] = Color.LightSkyBlue;
                }


            foreach (Polygon f in view)
            {

                f.CalculateVisibiltyAndLighting(Position);

                if (isorthg)
                {

                    // f.apply_matrix(multiply_matrix(f.get_matrix(), complete_matrix_orthoganal));
                }
                else
                {

                    f.apply_matrix(multiply_matrix(multiply_matrix(f.get_matrix(), view_matrix), perspective_projection_matrix));
                }


                foreach (Edge s in f.sides.Where(s => s.IsVisible))
                {
                    Color[,] ObjectTexture;
                    ObjectTexture = new Color[1, 1] { { s.penEdge.Color } };


                    point3[] pl;
                    switch (s.points.Count)
                    {
                        case 1://рисуем
                            point3 p0 = ViewPortTranform(s.getPoint(0), f.lighting[s.points[0]]);
                            if (p0.z > zbuffer[p0.y, p0.x])
                            {
                                zbuffer[p0.y, p0.x] = p0.z;
                                cbuffer[p0.y, p0.x] = ObjectTexture[0, 0];
                            }
                            break;
                        case 2:
                            pl = s.points.Select(i => ViewPortTranform(s.polygon.points[i], s.polygon.lighting[i])).OrderBy(p => p.y).ToArray();
                            FillTrinagle(pl[0], pl[1], pl[1], w, h, zbuffer, cbuffer, ObjectTexture);
                            break;

                        case 3:
                            pl = s.points.Select(i => ViewPortTranform(s.polygon.points[i], s.polygon.lighting[i])).OrderBy(p => p.y).ToArray();
                            FillTrinagle(pl[0], pl[1], pl[2], w, h, zbuffer, cbuffer, ObjectTexture);
                            break;
                        default:
                        case 4:
                            pl = s.points.Select(i => ViewPortTranform(s.polygon.points[i], s.polygon.lighting[i])).ToArray();
                            FillQuad(pl, w, h, zbuffer, cbuffer, ObjectTexture);
                            break;
                    }

                }

            }

            Bitmap bmp = new Bitmap(rend_obj.Width, rend_obj.Height);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);

            IntPtr ptr = bmpData.Scan0;
            int strd = Math.Abs(bmpData.Stride);
            int bytes = strd * bmp.Height;
            byte[] rgbValues = new byte[bytes];

            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    int ind = strd * i + j * 4;

                    rgbValues[ind] = cbuffer[i, j].B; //B
                    rgbValues[ind + 1] = cbuffer[i, j].G;//G
                    rgbValues[ind + 2] = cbuffer[i, j].R; // R
                    rgbValues[ind + 3] = cbuffer[i, j].A; //A
                }


            }

            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            bmp.UnlockBits(bmpData);


            return bmp;

        }

        ///  Camera params setters and getters invoking recounting of matrixes 
        private void update_view_matrix()
        {
            Point3D f = Point3D.Norm(eye_postion - view_target);
            Point3D s = Point3D.Norm(f * up);
            Point3D v = s * f;
            view_matrix = new float[,] { { s.X, v.X, f.X, 0 }, { s.Y, v.Y, f.Y, 0 }, { s.Z, v.Z, f.Z, 0 }, { -Point3D.Scalar(s, eye_postion), -Point3D.Scalar(v, eye_postion), -Point3D.Scalar(f, eye_postion), 1 } };
        }
        private void update_proj_matrix()
        {
            float w = (float)(1 / Math.Tan(fovx / 2));
            float h = (float)(1 / Math.Tan(fovy / 2));
            perspective_projection_matrix = new float[,] { { w, 0, 0, 0 },
                                                           { 0, h, 0, 0 },
                                                           { 0, 0,max_distance / (min_distance-max_distance), -1 },
                                                           { 0, 0,max_distance*min_distance/(min_distance - max_distance), 0 } };
            orthoganal_projection_matrix = new float[,] { { 2/cam_width,0,0,0},
                                                          {0,2/cam_height,0,0},
                                                          {0,0,1 / (min_distance-max_distance),-1 },
                                                          {0,0,min_distance/(min_distance - max_distance),0} };


        }
        private void update_full_matrix()
        {
            complete_matrix_perspective = multiply_matrix(view_matrix, perspective_projection_matrix);
            complete_matrix_orthoganal = multiply_matrix(view_matrix, orthoganal_projection_matrix);

        }
        public Point3D Up
        {
            get { return new Point3D(up); }
            set { up = value; update_view_matrix(); update_full_matrix(); }
        }
        public Point3D Position
        {
            get { return new Point3D(eye_postion); }
            set { eye_postion = value; update_view_matrix(); update_full_matrix(); }
        }
        public Point3D Target
        {
            get { return new Point3D(view_target); }
            set { view_target = value; update_view_matrix(); update_full_matrix(); }
        }
        public float FovX
        {
            get { return fovx; }
            set { fovx = value; update_proj_matrix(); update_full_matrix(); }
        }
        public float FovY
        {
            get { return fovy; }
            set { fovy = value; update_proj_matrix(); update_full_matrix(); }
        }
        public float MaxDistance
        {
            get { return max_distance; }
            set { max_distance = value; update_proj_matrix(); update_full_matrix(); }
        }
        public float MinDistance
        {
            get { return min_distance; }
            set { min_distance = value; update_proj_matrix(); update_full_matrix(); }
        }
        public float CamWidth
        {
            get { return cam_width; }
            set { cam_width = value; update_proj_matrix(); update_full_matrix(); }
        }
        public float CamHeight
        {
            get { return cam_height; }
            set { cam_height = value; update_proj_matrix(); update_full_matrix(); }
        }

        public bool IsOrthogonal
        {
            get { return isorthg; }
            set { isorthg = value; }

        }

        public void SetMimMaxPlane(float mn, float mx)
        {
            min_distance = mn;
            max_distance = mx;
            update_proj_matrix();
            update_full_matrix();
        }

        public void SetFov(float fx, float vy)
        {
            fovx = fx;
            fovy = vy;
            update_proj_matrix();
            update_full_matrix();
        }
        /// 
        /// ----------------------------------------------
        ///

        private static float[,] multiply_matrix(float[,] m1, float[,] m2)
        {
            float[,] res = new float[m1.GetLength(0), m2.GetLength(1)];
            for (int i = 0; i < m1.GetLength(0); i++)
            {
                for (int j = 0; j < m2.GetLength(1); j++)
                {
                    for (int k = 0; k < m2.GetLength(0); k++)
                    {
                        res[i, j] += m1[i, k] * m2[k, j];
                    }
                }
            }
            return res;

        }

        private static int[] Interpolate(int i0, int d0, int i1, int d1)
        {
            if (i0 == i1)
            {
                return new int[] { d0 };
            }
            int[] res;
            float a = (float)(d1 - d0) / (i1 - i0);
            float val = d0;
            res = new int[i1 - i0 + 1];
            d1 = 0;
            for (int i = i0; i <= i1; i++)
            {
                res[d1] = d0;
                val += a;
                d0 = (int)val;
                ++d1;
            }


            return res;

        }

        private static float[] FInterpolate(int i0, float d0, int i1, float d1)
        {
            if (i0 == i1)
            {
                return new float[] { d0 };
            }
            float[] res;
            float a = (d1 - d0) / (i1 - i0);
            float val = d0;
            res = new float[i1 - i0 + 1];
            int ind = 0;
            for (int i = i0; i <= i1; i++)
            {
                res[ind] = val;
                val += a;
                ++ind;
            }

            return res;
        }


        private static void FillQuad(point3[] pl, int w, int h, int[,] zbuffer, Color[,] cbuffer, Color[,] texture)
        {
            int cur = 0;
            int[] indexes = pl.Select(p => cur++).OrderBy(_i => pl[_i].y).ToArray();
            List<int> wayCW = new List<int> { indexes.First() };
            List<int> wayCCW = new List<int> { indexes.First() };


            int minx = pl.Min(p => p.x), maxx = pl.Max(p => p.x);
            int ly = Math.Max(pl[indexes.First()].y, 0);
            int i = ly - pl[indexes.First()].y;
            int uy = Math.Min(pl[indexes.Last()].y, h - 1);
            if (ly > uy || Math.Max(0, minx) > Math.Min(w - 1, maxx))
                return;


            float TextureScaleX, TextureScaleY;
            if (maxx != minx)
                TextureScaleX = (float)(texture.GetLength(1) - 1) / (maxx - minx);
            else
                TextureScaleX = 0;

            if (pl[indexes.First()].y != pl[indexes.Last()].y)
                TextureScaleY = (float)(texture.GetLength(0) - 1) / (pl[indexes.Last()].y - pl[indexes.First()].y);
            else
                TextureScaleY = 0;


            Point ToTextureCoords(int x, int y)
            {
                if (x < minx)
                    x = minx;
                return new Point((int)(TextureScaleX * (x - minx)), (int)(TextureScaleY * (y - pl[indexes.First()].y)));
            }

            cur = indexes.First();
            while (cur != indexes.Last())
            {
                cur = (cur + 1) % pl.Count();
                wayCW.Add(cur);
            }

            cur = indexes.First();
            while (cur != indexes.Last())
            {
                cur = (cur + pl.Count() - 1) % pl.Count();
                wayCCW.Add(cur);
            }




            int[] xWayCW = Interpolate(pl[wayCW[0]].y, pl[wayCW[0]].x, pl[wayCW[1]].y, pl[wayCW[1]].x);
            int[] hWayCW = Interpolate(pl[wayCW[0]].y, pl[wayCW[0]].z, pl[wayCW[1]].y, pl[wayCW[1]].z);
            float[] fWayCW = FInterpolate(pl[wayCW[0]].y, pl[wayCW[0]].l, pl[wayCW[1]].y, pl[wayCW[1]].l);
            for (int k = 1; k < wayCW.Count - 1; k++)
            {

                xWayCW = xWayCW.Take(xWayCW.Count() - 1).Concat(Interpolate(pl[wayCW[k]].y, pl[wayCW[k]].x, pl[wayCW[k + 1]].y, pl[wayCW[k + 1]].x)).ToArray();
                hWayCW = hWayCW.Take(hWayCW.Count() - 1).Concat(Interpolate(pl[wayCW[k]].y, pl[wayCW[k]].z, pl[wayCW[k + 1]].y, pl[wayCW[k + 1]].z)).ToArray();
                fWayCW = fWayCW.Take(fWayCW.Count() - 1).Concat(FInterpolate(pl[wayCW[k]].y, pl[wayCW[k]].l, pl[wayCW[k + 1]].y, pl[wayCW[k + 1]].l)).ToArray();

            }






            int[] xWayCCW = Interpolate(pl[wayCCW[0]].y, pl[wayCCW[0]].x, pl[wayCCW[1]].y, pl[wayCCW[1]].x);
            int[] hWayCCW = Interpolate(pl[wayCCW[0]].y, pl[wayCCW[0]].z, pl[wayCCW[1]].y, pl[wayCCW[1]].z);
            float[] fWayCCW = FInterpolate(pl[wayCCW[0]].y, pl[wayCCW[0]].l, pl[wayCCW[1]].y, pl[wayCCW[1]].l);
            for (int k = 1; k < wayCCW.Count - 1; k++)
            {
                xWayCCW = xWayCCW.Take(xWayCCW.Count() - 1).Concat(Interpolate(pl[wayCCW[k]].y, pl[wayCCW[k]].x, pl[wayCCW[k + 1]].y, pl[wayCCW[k + 1]].x)).ToArray();
                hWayCCW = hWayCCW.Take(hWayCCW.Count() - 1).Concat(Interpolate(pl[wayCCW[k]].y, pl[wayCCW[k]].z, pl[wayCCW[k + 1]].y, pl[wayCCW[k + 1]].z)).ToArray();
                fWayCCW = fWayCCW.Take(fWayCCW.Count() - 1).Concat(FInterpolate(pl[wayCCW[k]].y, pl[wayCCW[k]].l, pl[wayCCW[k + 1]].y, pl[wayCCW[k + 1]].l)).ToArray();
            }


            int[] xleft, xright, hleft, hright;
            float[] fleft, fright;

            int m = xWayCW.Length / 2;
            if (xWayCW[m] < xWayCCW[m])
            {
                // CW way is left
                // CCW way is right
                xleft = xWayCW;
                xright = xWayCCW;
                hleft = hWayCW;
                hright = hWayCCW;
                fleft = fWayCW;
                fright = fWayCCW;

            }
            else
            {
                // CCW way is left
                // CW way is right
                xleft = xWayCCW;
                xright = xWayCW;
                hleft = hWayCCW;
                hright = hWayCW;
                fleft = fWayCCW;
                fright = fWayCW;
            }


            for (int y = ly; y <= uy; y++)
            {
                int x_l = xleft[i];
                int x_r = xright[i];

                int lx = Math.Max(x_l, 0);
                int j = lx - x_l;
                int ux = Math.Min(x_r, w - 1);

                if (x_l > x_r || lx > ux)
                {
                    ++i;
                    continue;
                }

                int[] h_segment = Interpolate(x_l, hleft[i], x_r, hright[i]);
                float[] f_segment = FInterpolate(x_l, fleft[i], x_r, fright[i]);

                for (int x = lx; x <= ux; x++)
                {
                    int z = h_segment[j];
                    if (z > zbuffer[y, x])
                    {
                        zbuffer[y, x] = z;
                        Point t = ToTextureCoords(x, y);
                        cbuffer[y, x] = CompileColor(texture[t.Y, t.X], f_segment[j]);
                    }
                    j++;
                }
                i++;
            }


        }


        private static Color CompileColor(Color tex, float fval)
        {
            return Color.FromArgb(tex.A, tex.R, tex.G, tex.B);
        }


        private static void FillTrinagle(point3 p0, point3 p1, point3 p2, int w, int h, int[,] zbuffer, Color[,] cbuffer, Color[,] texture)
        {
            // p0.y <=p1.y <= p2.y

            var pl = new point3[] { p0, p1, p2 };
            int minx = pl.Min(p => p.x), maxx = pl.Max(p => p.x);

            int ly = Math.Max(p0.y, 0);
            int i = ly - p0.y;
            int uy = Math.Min(p2.y, h - 1);
            if (ly > uy || Math.Max(0, minx) > Math.Min(w - 1, maxx))
                return;



            float TextureScaleX, TextureScaleY;
            if (maxx != minx)
                TextureScaleX = (float)(texture.GetLength(1) - 1) / (maxx - minx);
            else
                TextureScaleX = 0;

            if (p0.y != p2.y)
                TextureScaleY = (float)(texture.GetLength(0) - 1) / (p2.y - p0.y);
            else
                TextureScaleY = 0;

            Point ToTextureCoords(int x, int y)
            {
                if (x < minx)
                    x = minx;
                return new Point((int)(TextureScaleX * (x - minx)), (int)(TextureScaleY * (y - p0.y)));
            }



            int[] x012 = Interpolate(p0.y, p0.x, p1.y, p1.x);
            x012 = x012.Take(x012.Length - 1).Concat(Interpolate(p1.y, p1.x, p2.y, p2.x)).ToArray();

            int[] h012 = Interpolate(p0.y, p0.z, p1.y, p1.z);
            h012 = h012.Take(h012.Length - 1).Concat(Interpolate(p1.y, p1.z, p2.y, p2.z)).ToArray();

            float[] f012 = FInterpolate(p0.y, p0.l, p1.y, p1.l);
            f012 = f012.Take(f012.Length - 1).Concat(FInterpolate(p1.y, p1.l, p2.y, p2.l)).ToArray();

            int[] x02 = Interpolate(p0.y, p0.x, p2.y, p2.x);
            int[] h02 = Interpolate(p0.y, p0.z, p2.y, p2.z);
            float[] f02 = FInterpolate(p0.y, p0.l, p2.y, p2.l);



            int[] x_left, x_right, h_left, h_right;
            float[] f_left, f_right;

            int m = x012.Length / 2;
            if (x02[m] < x012[m])
            {
                x_left = x02;
                x_right = x012;

                h_left = h02;
                h_right = h012;

                f_left = f02;
                f_right = f012;
            }
            else
            {
                x_left = x012;
                x_right = x02;

                h_left = h012;
                h_right = h02;

                f_left = f012;
                f_right = f02;
            }

            for (int y = ly; y <= uy; y++)
            {

                int x_l = x_left[i];
                int x_r = x_right[i];

                int lx = Math.Max(x_l, 0);
                int j = lx - x_l;
                int ux = Math.Min(x_r, w - 1);

                if (lx > ux)
                {
                    i++;
                    continue;
                }

                int[] h_segment;
                float[] f_segment;
                if (x_l > x_r)
                    break;
                h_segment = Interpolate(x_l, h_left[i], x_r, h_right[i]);
                f_segment = FInterpolate(x_l, f_left[i], x_r, f_right[i]);


                for (int x = lx; x <= ux; x++)
                {
                    int z = h_segment[j];
                    if (z > zbuffer[y, x])
                    {
                        zbuffer[y, x] = z;
                        Point t = ToTextureCoords(x, y);
                        cbuffer[y, x] = CompileColor(texture[t.Y, t.X], f_segment[j]);
                    }
                    j++;
                }
                i++;
            }

        }


        public static Color[,] TextureToColors(string filepath)
        {
            Bitmap tex = new Bitmap(filepath);
            Color[,] res = new Color[tex.Height, tex.Width];


            Rectangle rect = new Rectangle(0, 0, tex.Width, tex.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                tex.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                tex.PixelFormat);


            IntPtr ptr = bmpData.Scan0;
            int strd = Math.Abs(bmpData.Stride);
            int bytes = strd * tex.Height;
            byte[] rgbValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            tex.UnlockBits(bmpData);

            for (int i = 0; i < tex.Height; i++)
            {
                for (int j = 0; j < tex.Width; j++)
                {
                    int ind = strd * i + j * 4;

                    res[i, j] = Color.FromArgb(rgbValues[ind + 3], rgbValues[ind + 2], rgbValues[ind + 1], rgbValues[ind]);
                }
            }
            return res;
        }
    }

    public class OrbitCamera : CameraView
    {
        private float cam_angx;
        private float cam_angy;
        private float cam_distance;
        private float cam_tilt;
        private Polygon cam;
        private float angX;
        private float angY;
        private float angT;
        private float dist;

        public OrbitCamera(float distance, float ini_anglx, float init_angly, float init_tiltang, Point3D t, float fvx, float fvy, float mind, float maxd)
            : base(new Point3D(0, 0, 0), t, new Point3D(0, 0, 0), fvx, fvy, mind, maxd)
        {

            cam_distance = distance;
            cam_angy = init_angly;
            cam_angx = ini_anglx;
            cam_tilt = init_tiltang;

            dist = distance;
            angX = ini_anglx;
            angY = init_angly;
            angT = init_tiltang;

            cam = Polygon.getCoordinates();
            for (int i = 1; i < 4; i++)
                cam.points[i] = Point3D.Norm(cam.points[i]);

            cam.offset(-cam_distance, 0, 0);
            cam.line_rotate_rad(cam_angx, new Point3D(0, 0, 0), cam.points[3] - cam.points[0]);
            cam.line_rotate_rad(cam_angy, new Point3D(0, 0, 0), cam.points[1] - cam.points[0]);
            cam.line_rotate_rad(cam_tilt, new Point3D(0, 0, 0), cam.points[2] - cam.points[0]);

            set_cam();
        }

        private void set_cam()
        {
            set_params_at_once(cam.points[0], Target, cam.points[3] - cam.points[0], FovX, FovY, MinDistance, MaxDistance);

        }

        public void MoveUpDown(float rad_ang)
        {
            cam.line_rotate_rad(rad_ang, new Point3D(0, 0, 0), cam.points[1] - cam.points[0]);
            angY += rad_ang;
            set_cam();
            if (angY >= Math.PI * 2) angY -= (float)Math.PI * 2;
            else if (angY <= -Math.PI * 2) angY += (float)Math.PI * 2;
        }

        public void MoveLeftRight(float rad_ang)
        {
            cam.line_rotate_rad(rad_ang, new Point3D(0, 0, 0), cam.points[3] - cam.points[0]);
            angX += rad_ang;
            set_cam();
            if (angX >= Math.PI * 2) angX -= (float)Math.PI * 2;
            else if (angX <= -Math.PI * 2) angX += (float)Math.PI * 2;
        }

        public void TiltLeftRight(float rad_ang)
        {
            cam.line_rotate_rad(rad_ang, new Point3D(0, 0, 0), cam.points[2] - cam.points[0]);
            angT += rad_ang;
            set_cam();
            if (angT >= Math.PI * 2) angT -= (float)Math.PI * 2;
            else if (angT <= -Math.PI * 2) angT += (float)Math.PI * 2;
        }

        public void MoveFarNear(float d)
        {
            Point3D ofst = cam.points[0] - cam.points[2];
            cam.offset(d * ofst.X, d * ofst.Y, d * ofst.Z);
            dist += d;
            set_cam();
        }

        public float AngleX
        {
            get { return angX; }
            set { MoveLeftRight(value - angX); }
        }

        public float AngleY
        {
            get { return angY; }
            set { MoveUpDown(value - angY); }
        }

        public float AngleTilt
        {
            get { return angT; }
            set { TiltLeftRight(value - angT); }
        }

        public float Distance
        {
            get { return dist; }
            set { MoveFarNear(value - dist); }
        }

        ///something weird with resetting to angles, went the hard way
        public void Reset()
        {
            dist = cam_distance;
            angX = cam_angx;
            angY = cam_angy;
            angT = cam_tilt;

            cam = Polygon.getCoordinates();
            for (int i = 1; i < 4; i++)
                cam.points[i] = Point3D.Norm(cam.points[i]);

            cam.offset(-cam_distance, 0, 0);
            cam.line_rotate_rad(cam_angx, new Point3D(0, 0, 0), cam.points[3] - cam.points[0]);
            cam.line_rotate_rad(cam_angy, new Point3D(0, 0, 0), cam.points[1] - cam.points[0]);
            cam.line_rotate_rad(cam_tilt, new Point3D(0, 0, 0), cam.points[2] - cam.points[0]);

            set_cam();
        }
    }

    public struct point3
    {
        public int x;
        public int y;
        public int z;
        public float l;


        public point3(int _x, int _y, int _z, float _l)
        {
            x = _x;
            y = _y;
            z = _z;
            l = _l;
        }
    }
}
