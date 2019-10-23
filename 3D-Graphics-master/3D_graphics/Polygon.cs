using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AffineTransformations
{
    public class Polygon
    {

        public List<Point3D> points = new List<Point3D>(); // точки 
        public List<Edge> sides = new List<Edge>(); // стороны
        public float[] lighting;
        private Point3D[] point_normals;
        public Polygon()
        {
        }

        // redo for new members
        public Polygon(Polygon f)
        {
            foreach (Point3D p in f.points)
            {
                points.Add(new Point3D(p));
            }
            foreach (Edge s in f.sides)
            {
                sides.Add(new Edge(s));
                sides.Last().polygon = this;
            }
            if (f.lighting != null)
                lighting = f.lighting.ToArray();
        }

        ///  Calculate visibility of each side and lighting intensifyer of every visible vertex
        public void CalculateVisibiltyAndLighting(Point3D eye_pos)
        {
            lighting = new float[points.Count];
            List<Edge>[] point_sides = new List<Edge>[points.Count];
            bool[] point_visible = new bool[points.Count];
            point_normals = new Point3D[points.Count];
            foreach (Edge s in sides)
            {
                s.CalculateSideNormal();
                s.CalculateVisibilty(eye_pos);
                foreach (int ind in s.points)
                    if (point_sides[ind] == null)
                        point_sides[ind] = new List<Edge>() { s };
                    else
                        point_sides[ind].Add(s);

            }

            for (int i = 0; i < points.Count; i++)
            {
                point_visible[i] = point_sides[i].Any(s => s.IsVisible);
                if (point_visible[i])
                {
                    Point3D t = point_sides[i].Aggregate(new Point3D(0, 0, 0), (Point3D n, Edge s) => { n.X += s.Normal.X; n.Y += s.Normal.Y; n.Z += s.Normal.Z; return n; });
                    t.X /= point_sides[i].Count;
                    t.Y /= point_sides[i].Count;
                    t.Z /= point_sides[i].Count;
                    point_normals[i] = t;
                }
            }
        }

        private void CalculateVertexNormals()
        {
            point_normals = new Point3D[points.Count];
            int[] count_sidesPerPoint = new int[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                point_normals[i] = new Point3D(0, 0, 0);
                count_sidesPerPoint[i] = 0;

            }

            foreach (Edge s in sides)
            {
                s.CalculateSideNormal();
                // sorry in this project normals are calculated inverted pls fix
                Point3D Normal = new Point3D(s.Normal.X * -1, s.Normal.Y * -1, s.Normal.Z * -1);
                for (int i = 0; i < s.points.Count; i++)
                {
                    int ind = s.points[i];
                    point_normals[ind].X += Normal.X;
                    point_normals[ind].Y += Normal.Y;
                    point_normals[ind].Z += Normal.Z;
                    count_sidesPerPoint[ind] += 1;

                }


            }
            for (int i = 0; i < points.Count; i++)
            {
                if (count_sidesPerPoint[i] != 0)
                {
                    point_normals[i].X /= count_sidesPerPoint[i];
                    point_normals[i].Y /= count_sidesPerPoint[i];
                    point_normals[i].Z /= count_sidesPerPoint[i];
                    point_normals[i] = Point3D.Norm(point_normals[i]);
                }
            }


        }


        ///
        /// ----------------------------- TRANSFORMS  SUPPORT METHODS --------------------------------
        ///


        public float[,] get_matrix()
        {
            var res = new float[points.Count, 4];
            for (int i = 0; i < points.Count; i++)
            {
                res[i, 0] = points[i].X;
                res[i, 1] = points[i].Y;
                res[i, 2] = points[i].Z;
                res[i, 3] = 1;
            }
            return res;
        }
        public void apply_matrix(float[,] matrix)
        {
            for (int i = 0; i < points.Count; i++)
            {
                points[i].X = matrix[i, 0] / matrix[i, 3];
                points[i].Y = matrix[i, 1] / matrix[i, 3];
                points[i].Z = matrix[i, 2] / matrix[i, 3];

            }
        }
        private Point3D get_center()
        {
            Point3D res = new Point3D(0, 0, 0);
            foreach (Point3D p in points)
            {
                res.X += p.Z;
                res.Y += p.Y;
                res.Z += p.Z;

            }
            res.X /= points.Count();
            res.Y /= points.Count();
            res.Z /= points.Count();
            return res;
        }


        ///
        /// ----------------------------- APHINE TRANSFORMS METHODS --------------------------------
        ///

        public void rotate_around_rad(float rangle, string type)
        {
            float[,] mt = get_matrix();
            Point3D center = get_center();
            switch (type)
            {
                case "CX":
                    mt = apply_offset(mt, -center.X, -center.Y, -center.Z);
                    mt = apply_rotation_X(mt, rangle);
                    mt = apply_offset(mt, center.X, -center.Y, -center.Z);
                    break;
                case "CY":
                    mt = apply_offset(mt, -center.X, -center.Y, -center.Z);
                    mt = apply_rotation_Y(mt, rangle);
                    mt = apply_offset(mt, center.X, -center.Y, -center.Z);
                    break;
                case "CZ":
                    mt = apply_offset(mt, -center.X, -center.Y, -center.Z);
                    mt = apply_rotation_Z(mt, rangle);
                    mt = apply_offset(mt, center.X, -center.Y, -center.Z);
                    break;
                case "X":
                    mt = apply_rotation_X(mt, rangle);
                    break;
                case "Y":
                    mt = apply_rotation_Y(mt, rangle);
                    break;
                case "Z":
                    mt = apply_rotation_Z(mt, rangle);
                    break;
                default:
                    break;
            }
            apply_matrix(mt);
        }
        public void rotate_around(float angle, string type)
        {
            rotate_around_rad(angle * (float)Math.PI / 180, type);
        }
        public void scale_axis(float xs, float ys, float zs)
        {
            float[,] pnts = get_matrix();
            pnts = apply_scale(pnts, xs, ys, zs);
            apply_matrix(pnts);
        }
        public void offset(float xs, float ys, float zs)
        {
            apply_matrix(apply_offset(get_matrix(), xs, ys, zs));
        }

        public void SetPen(Pen dw)
        {
            foreach (Edge s in sides)
                s.penEdge = dw;

        }
        public void SetRandColor()
        {
            Random r = new Random();
            foreach (Edge s in sides)
            {
                Color c = Color.FromArgb((byte)r.Next(0, 255), (byte)r.Next(0, 255), (byte)r.Next(0, 255));
                s.penEdge = new Pen(c);
            }
        }


        public void scale_around_center(float xs, float ys, float zs)
        {
            float[,] pnts = get_matrix();
            Point3D p = get_center();
            pnts = apply_offset(pnts, -p.X, -p.Y, -p.Z);
            pnts = apply_scale(pnts, xs, ys, zs);
            pnts = apply_offset(pnts, p.X, p.Y, p.Z);
            apply_matrix(pnts);
        }
        public void line_rotate_rad(float rang, Point3D p1, Point3D p2)
        {

            p2 = new Point3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            p2 = Point3D.Norm(p2);

            float[,] mt = get_matrix();
            apply_matrix(rotate_around_line(mt, p1, p2, rang));
        }

        /// rotate figure line
        public void line_rotate(float ang, Point3D p1, Point3D p2)
        {
            ang = ang * (float)Math.PI / 180;
            line_rotate_rad(ang, p1, p2);
        }

        ///
        /// ----------------------------- PROJECTIONS METHODS --------------------------------
        ///

        public void project_orthogX()
        {
            apply_matrix(orthographic_projection_X(get_matrix()));
        }
        public void project_orthogY()
        {
            apply_matrix(orthographic_projection_Y(get_matrix()));
        }
        public void project_orthogZ()
        {
            apply_matrix(orthographic_projection_Z(get_matrix()));
        }
        public void project_isometric()
        {
            apply_matrix(isometric_projection(get_matrix()));
        }
        public void project_cental()
        {
            apply_matrix(perspective_projection(get_matrix()));
        }


        ///
        /// ----------------------------- STATIC BACKEND FOR TRANSFROMS --------------------------------
        ///

        private static float[,] rotate_around_line(float[,] transform_matrix, Point3D start, Point3D dir, float angle)
        {
            float cos_angle = (float)Math.Cos(angle);
            float sin_angle = (float)Math.Sin(angle);
            float val00 = dir.X * dir.X + cos_angle * (1 - dir.X * dir.X);
            float val01 = dir.X * (1 - cos_angle) * dir.Y + dir.Z * sin_angle;
            float val02 = dir.X * (1 - cos_angle) * dir.Z - dir.Y * sin_angle;
            float val10 = dir.X * (1 - cos_angle) * dir.Y - dir.Z * sin_angle;
            float val11 = dir.Y * dir.Y + cos_angle * (1 - dir.Y * dir.Y);
            float val12 = dir.Y * (1 - cos_angle) * dir.Z + dir.X * sin_angle;
            float val20 = dir.X * (1 - cos_angle) * dir.Z + dir.Y * sin_angle;
            float val21 = dir.Y * (1 - cos_angle) * dir.Z - dir.X * sin_angle;
            float val22 = dir.Z * dir.Z + cos_angle * (1 - dir.Z * dir.Z);
            float[,] rotateMatrix = new float[,] { { val00, val01, val02, 0 }, { val10, val11, val12, 0 }, { val20, val21, val22, 0 }, { 0, 0, 0, 1 } };
            return apply_offset(multiply_matrix(apply_offset(transform_matrix, -start.X, -start.Y, -start.Z), rotateMatrix), start.X, start.Y, start.Z);
        }
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
        private static float[,] apply_offset(float[,] transform_matrix, float offset_x, float offset_y, float offset_z)
        {
            float[,] translationMatrix = new float[,] { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 1, 0 }, { offset_x, offset_y, offset_z, 1 } };
            return multiply_matrix(transform_matrix, translationMatrix);
        }
        private static float[,] apply_rotation_X(float[,] transform_matrix, float angle)
        {
            float[,] rotationMatrix = new float[,] { { 1, 0, 0, 0 }, { 0, (float)Math.Cos(angle), (float)Math.Sin(angle), 0 },
                { 0, -(float)Math.Sin(angle), (float)Math.Cos(angle), 0}, { 0, 0, 0, 1} };
            return multiply_matrix(transform_matrix, rotationMatrix);
        }
        private static float[,] apply_rotation_Y(float[,] transform_matrix, float angle)
        {
            float[,] rotationMatrix = new float[,] { { (float)Math.Cos(angle), 0, -(float)Math.Sin(angle), 0 }, { 0, 1, 0, 0 },
                { (float)Math.Sin(angle), 0, (float)Math.Cos(angle), 0}, { 0, 0, 0, 1} };
            return multiply_matrix(transform_matrix, rotationMatrix);
        }
        private static float[,] apply_rotation_Z(float[,] transform_matrix, float angle)
        {
            float[,] rotationMatrix = new float[,] { { (float)Math.Cos(angle), (float)Math.Sin(angle), 0, 0 }, { -(float)Math.Sin(angle), (float)Math.Cos(angle), 0, 0 },
                { 0, 0, 1, 0 }, { 0, 0, 0, 1} };
            return multiply_matrix(transform_matrix, rotationMatrix);
        }
        private static float[,] apply_scale(float[,] transform_matrix, float scale_x, float scale_y, float scale_z)
        {
            float[,] scaleMatrix = new float[,] { { scale_x, 0, 0, 0 }, { 0, scale_y, 0, 0 }, { 0, 0, scale_z, 0 }, { 0, 0, 0, 1 } };
            return multiply_matrix(transform_matrix, scaleMatrix);
        }
        private static float[,] perspective_projection(float[,] transform_matrix)
        {
            float center = 200;
            float[,] projMatrix = new float[,] { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 0, -1 / center }, { 0, 0, 0, 1 } };
            float[,] res_mt = multiply_matrix(transform_matrix, projMatrix);
            return res_mt;
        }
        private static float[,] orthographic_projection_X(float[,] transform_matrix)
        {
            float[,] projMatrix = new float[,] { { 0, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 1, 0 }, { 0, 0, 0, 1 } };
            float[,] res_mt = multiply_matrix(transform_matrix, projMatrix);
            for (int i = 0; i < res_mt.GetLength(0); ++i)
            {

                res_mt[i, 0] = res_mt[i, 1];
                res_mt[i, 1] = res_mt[i, 2];
                res_mt[i, 2] = 0;
            }
            return res_mt;
        }
        private static float[,] orthographic_projection_Y(float[,] transform_matrix)
        {
            float[,] projMatrix = new float[,] { { 1, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 1, 0 }, { 0, 0, 0, 1 } };
            float[,] res_mt = multiply_matrix(transform_matrix, projMatrix);
            for (int i = 0; i < res_mt.GetLength(0); ++i)
            {
                res_mt[i, 1] = res_mt[i, 2];
                res_mt[i, 2] = 0;
            }
            return res_mt;
        }
        private static float[,] orthographic_projection_Z(float[,] transform_matrix)
        {
            float[,] projMatrix = new float[,] { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 1 } };
            return multiply_matrix(transform_matrix, projMatrix);
        }
        private static float[,] isometric_projection(float[,] transform_matrix)
        {
            float a = (float)Math.Asin(Math.Tan(30 * Math.PI / 180));
            float b = 45 * (float)Math.PI / 180;
            float[,] transposeRotationMatrixY = new float[,] { { (float)Math.Cos(b), 0, (float)Math.Sin(b), 0 }, { 0, 1, 0, 0 }, { -(float)Math.Sin(b), 0, (float)Math.Cos(b), 0 }, { 0, 0, 0, 1 } };
            float[,] transposeRotationMatrixX = new float[,] { { 1, 0, 0, 0 }, { 0, (float)Math.Cos(a), -(float)Math.Sin(a), 0 }, { 0, (float)Math.Sin(a), (float)Math.Cos(a), 0 }, { 0, 0, 0, 1 } };
            float[,] ortMatrix = new float[,] { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 1 } };

            float[,] mt1 = multiply_matrix(transform_matrix, transposeRotationMatrixY);
            float[,] mt2 = multiply_matrix(mt1, transposeRotationMatrixX);
            return multiply_matrix(mt2, ortMatrix);
        }

        ///
        /// --------------------SAVE/LOAD METHODS------------------------------------------
        ///

        public static Polygon parse_figure(string filename)
        {
            Polygon res = new Polygon();
            List<string> lines = System.IO.File.ReadLines(filename).ToList();
            var st = lines[0].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (st[0] == "rotation")
                return parse_rotation(lines);
            else
            {
                int count_points = Int32.Parse(st[0]);
                Dictionary<string, int> pnts = new Dictionary<string, int>();

                for (int i = 0; i < count_points; ++i)
                {
                    string[] str = lines[i + 1].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    res.points.Add(new Point3D(float.Parse(str[1]), float.Parse(str[2]), float.Parse(str[3])));
                    pnts.Add(str[0], i);
                }

                int count_sides = Int32.Parse(lines[count_points + 1]);
                for (int i = count_points + 2; i < lines.Count(); ++i)
                {
                    Edge s = new Edge(res);
                    List<string> str = lines[i].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    foreach (var id in str)
                        s.points.Add(pnts[id]);
                    res.sides.Add(s);
                }

                res.SetPen(new Pen(Color.Red));
                return res;
            }
        }

        public static Polygon parse_rotation(List<string> lines)
        {

            string[] cnt = lines[1].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int count_points = Int32.Parse(cnt[0]);
            int count_divs = Int32.Parse(cnt[1]);

            if (count_points < 1 || count_divs < 1)
                return new Polygon();

            List<Point3D> pnts = new List<Point3D>();
            for (int i = 2; i < count_points + 2; ++i)
            {
                string[] s = lines[i].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                pnts.Add(new Point3D(float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3])));
            }

            string[] str = lines[count_points + 2].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            Point3D axis1 = new Point3D(float.Parse(str[0]), float.Parse(str[1]), float.Parse(str[2]));
            str = lines[count_points + 3].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            Point3D axis2 = new Point3D(float.Parse(str[0]), float.Parse(str[1]), float.Parse(str[2]));

            return get_Rotation(pnts, axis1, axis2, count_divs);
        }

        ///
        /// ------------------------STATIC READY FIGURES-----------------------------
        ///

        static public Polygon getCoordinates()
        {
            Polygon res = new Polygon();
            res.points.Add(new Point3D(0, 0, 0));

            res.points.Add(new Point3D(0, 100, 0));
            res.points.Add(new Point3D(100, 0, 0));
            res.points.Add(new Point3D(0, 0, 100));

            res.sides.Add(new Edge(res));
            res.sides.Last().points = new List<int> { 0, 1 };
            res.sides.Last().penEdge.Color = Color.Green;
            res.sides.Add(new Edge(res));
            res.sides.Last().points = new List<int> { 0, 2 };
            res.sides.Last().penEdge.Color = Color.Red;
            res.sides.Add(new Edge(res));
            res.sides.Last().points = new List<int> { 0, 3 };
            res.sides.Last().penEdge.Color = Color.Blue;

            return res;
        }


        static public Polygon Octahedron(float sz)
        {
            Polygon res = new Polygon();
            res.points.Add(new Point3D(sz / 2, 0, 0)); //0
            res.points.Add(new Point3D(-sz / 2, 0, 0)); //1
            res.points.Add(new Point3D(0, sz / 2, 0)); //2
            res.points.Add(new Point3D(0, -sz / 2, 0));//3
            res.points.Add(new Point3D(0, 0, sz / 2));//4
            res.points.Add(new Point3D(0, 0, -sz / 2));//5

            Edge s = new Edge(res);
            s.points.AddRange(new int[] { 0, 4, 3 });
            res.sides.Add(s);

            s = new Edge(res);
            s.points.AddRange(new int[] { 0, 2, 4 });
            res.sides.Add(s);

            s = new Edge(res);
            s.points.AddRange(new int[] { 1, 4, 2 });
            res.sides.Add(s);

            s = new Edge(res);
            s.points.AddRange(new int[] { 1, 3, 4 });
            res.sides.Add(s);

            s = new Edge(res);
            s.points.AddRange(new int[] { 0, 5, 2 });
            res.sides.Add(s);

            s = new Edge(res);
            s.points.AddRange(new int[] { 1, 2, 5 });
            res.sides.Add(s);

            s = new Edge(res);
            s.points.AddRange(new int[] { 0, 3, 5 });
            res.sides.Add(s);

            s = new Edge(res);
            s.points.AddRange(new int[] { 1, 5, 3 });
            res.sides.Add(s);

            res.SetRandColor();

            return res;
        }

        static public Polygon get_Tetrahedron(float sz)
        {
            Polygon res = new Polygon();
            sz = sz / 2;
            res.points.Add(new Point3D(sz, sz, sz));
            res.points.Add(new Point3D(-sz, -sz, sz));
            res.points.Add(new Point3D(sz, -sz, -sz));
            res.points.Add(new Point3D(-sz, sz, -sz));
            res.sides.Add(new Edge(res));
            res.sides.Last().points.AddRange(new List<int> { 0, 1, 2 });
            res.sides.Add(new Edge(res));
            res.sides.Last().points.AddRange(new List<int> { 1, 3, 2 });
            res.sides.Add(new Edge(res));
            res.sides.Last().points.AddRange(new List<int> { 0, 2, 3 });
            res.sides.Add(new Edge(res));
            res.sides.Last().points.AddRange(new List<int> { 0, 3, 1 });
            res.SetRandColor();
            return res;
        }

        static public Polygon get_Icosahedron(float sz)
        {
            Polygon res = new Polygon();
            float ang = (float)(Math.PI / 5);

            bool is_upper = true;
            int ind = 0;
            float a = 0;
            for (int i = 0; i < 10; ++i)
            {
                res.points.Add(new Point3D((float)Math.Cos((float)a), (float)Math.Sin((float)a), is_upper ? (float)0.5 : (float)-0.5));
                is_upper = !is_upper;
                ind++;
                a += ang;
            }
            Edge s;
            for (int i = 0; i < ind; i++)
            {
                s = new Edge(res);
                if (i % 2 == 0)
                {
                    s.points.AddRange(new int[] { i, (i + 1) % ind, (i + 2) % ind });
                    //  s.drawing_pen = new Pen(Color.Green);
                }
                else
                {
                    s.points.AddRange(new int[] { (i + 2) % ind, (i + 1) % ind, i });
                    //   s.drawing_pen = new Pen(Color.Red);
                }

                res.sides.Add(s);
            }




            res.points.Add(new Point3D(0, 0, (float)Math.Sqrt(5) / 2)); // ind
            res.points.Add(new Point3D(0, 0, -(float)Math.Sqrt(5) / 2)); // ind+1
            for (int i = 0; i < ind; i += 2)
            {
                s = new Edge(res);
                s.points.AddRange(new int[] { i, ind, (i + 2) % ind });
                s.points.Reverse();

                res.sides.Add(s);
            }

            for (int i = 1; i < ind; i += 2)
            {
                s = new Edge(res);
                s.points.AddRange(new int[] { i, (i + 2) % ind, ind + 1 });
                s.points.Reverse();
                res.sides.Add(s);
            }

            res.scale_around_center(sz, sz, sz);

            res.SetRandColor();
            return res;
        }

        public static Polygon get_curve(float x0, float x1, float y0, float y1, int n_x, int n_y, Func<float, float, float> f)
        {
            float step_x = (x1 - x0) / n_x;
            float step_y = (y1 - y0) / n_y;
            Polygon res = new Polygon();

            float x = x0;
            float y = y0;

            for (int i = 0; i <= n_x; ++i)
            {
                y = y0;
                for (int j = 0; j <= n_y; ++j)
                {
                    res.points.Add(new Point3D(x, y, f(x, y)));
                    y += step_y;
                }
                x += step_x;
            }

            for (int i = 0; i < res.points.Count; ++i)
            {
                if ((i + 1) % (n_y + 1) == 0)
                    continue;
                if (i / (n_y + 1) == n_x)
                    break;

                Edge s = new Edge(res);
                s.points.AddRange(new int[] { i, i + 1, i + n_y + 2, i + n_y + 1 });
                s.points.Reverse();
                res.sides.Add(s);
            }
            res.SetRandColor();
            return res;
        }


        public static Polygon get_Rotation(List<Point3D> pnts, Point3D axis1, Point3D axis2, int divs)
        {
            Polygon res = new Polygon();
            Polygon edge = new Polygon();
            int cnt_pnt = pnts.Count;
            edge.points = pnts.Select(x => new Point3D(x)).ToList();
            res.points = pnts.Select(x => new Point3D(x)).ToList();
            int cur_ind = res.points.Count;
            float ang = (float)360 / divs;
            for (int i = 0; i < divs; i++)
            {
                edge.line_rotate(ang, axis1, axis2);
                cur_ind = res.points.Count;
                for (int j = 0; j < cnt_pnt; j++)
                {
                    res.points.Add(new Point3D(edge.points[j]));

                }

                for (int j = cur_ind; j < res.points.Count - 1; j++)
                {
                    Edge s = new Edge(res);
                    s.points.AddRange(new int[] { j, j + 1, j + 1 - cnt_pnt, j - cnt_pnt });
                    res.sides.Add(s);

                }


            }

            res.SetPen(new Pen(Color.Black));
            return res;
        }
    }
}
