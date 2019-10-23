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

        public Polygon() { }

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
        }

        ///  Calculate visibility of each side and lighting intensifyer of every visible vertex
        public void CalculateVisibiltyAndLighting(Point3D eye_pos)
        {
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

        ///
        /// ----------------------------- TRANSFORMS  SUPPORT METHODS --------------------------------
        ///


        public float[,] GetMatrix()
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

        public void ApplyMatrix(float[,] matrix)
        {
            for (int i = 0; i < points.Count; i++)
            {
                points[i].X = matrix[i, 0] / matrix[i, 3];
                points[i].Y = matrix[i, 1] / matrix[i, 3];
                points[i].Z = matrix[i, 2] / matrix[i, 3];

            }
        }

        //определяет центр
        private Point3D GetCenter()
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

        //
        public void RotateAround(float angle, string type)
        {
            RotateAroundRad(angle * (float)Math.PI / 180, type);
        }

        public void RotateAroundRad(float rangle, string type)
        {
            float[,] mt = GetMatrix();
            Point3D center = GetCenter();
            switch (type)
            {
                case "CX":
                    mt = ApplyOffset(mt, -center.X, -center.Y, -center.Z);
                    mt = ApplyRotationX(mt, rangle);
                    mt = ApplyOffset(mt, center.X, -center.Y, -center.Z);
                    break;
                case "CY":
                    mt = ApplyOffset(mt, -center.X, -center.Y, -center.Z);
                    mt = ApplyRotationY(mt, rangle);
                    mt = ApplyOffset(mt, center.X, -center.Y, -center.Z);
                    break;
                case "CZ":
                    mt = ApplyOffset(mt, -center.X, -center.Y, -center.Z);
                    mt = ApplyRotationZ(mt, rangle);
                    mt = ApplyOffset(mt, center.X, -center.Y, -center.Z);
                    break;
                case "X":
                    mt = ApplyRotationX(mt, rangle);
                    break;
                case "Y":
                    mt = ApplyRotationY(mt, rangle);
                    break;
                case "Z":
                    mt = ApplyRotationZ(mt, rangle);
                    break;
                default:
                    break;
            }
            ApplyMatrix(mt);
        }

        public void ScaleAxis(float xs, float ys, float zs)
        {
            float[,] pnts = GetMatrix();
            pnts = apply_scale(pnts, xs, ys, zs);
            ApplyMatrix(pnts);
        }

        public void Offset(float xs, float ys, float zs)
        {
            ApplyMatrix(ApplyOffset(GetMatrix(), xs, ys, zs));
        }

        //определяет цвет граней
        public void SetRandColor()
        {
            Random r = new Random();
            foreach (Edge s in sides)
            {
                Color c = Color.FromArgb((byte)r.Next(0, 255), (byte)r.Next(0, 255), (byte)r.Next(0, 255));
                s.penEdge = new Pen(c);
            }
        }

        public void ScaleAroundCenter(float xs, float ys, float zs)
        {
            float[,] pnts = GetMatrix();
            Point3D p = GetCenter();
            pnts = ApplyOffset(pnts, -p.X, -p.Y, -p.Z);
            pnts = apply_scale(pnts, xs, ys, zs);
            pnts = ApplyOffset(pnts, p.X, p.Y, p.Z);
            ApplyMatrix(pnts);
        }

        /// rotate figure line
        public void LineRotation(float ang, Point3D p1, Point3D p2)
        {
            ang = ang * (float)Math.PI / 180;
            LineRotarionRad(ang, p1, p2);
        }

        public void LineRotarionRad(float rang, Point3D p1, Point3D p2)
        {

            p2 = new Point3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            p2 = Point3D.Norm(p2);

            float[,] mt = GetMatrix();
            ApplyMatrix(rotate_around_line(mt, p1, p2, rang));
        }


        ///
        /// ----------------------------- PROJECTIONS METHODS --------------------------------
        ///

        public void project_orthogX()
        {
            ApplyMatrix(orthographic_projection_X(GetMatrix()));
        }
        public void project_orthogY()
        {
            ApplyMatrix(orthographic_projection_Y(GetMatrix()));
        }
        public void project_orthogZ()
        {
            ApplyMatrix(orthographic_projection_Z(GetMatrix()));
        }
        public void project_isometric()
        {
            ApplyMatrix(isometric_projection(GetMatrix()));
        }
        public void project_cental()
        {
            ApplyMatrix(perspective_projection(GetMatrix()));
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
            return ApplyOffset(multiply_matrix(ApplyOffset(transform_matrix, -start.X, -start.Y, -start.Z), rotateMatrix), start.X, start.Y, start.Z);
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

        private static float[,] ApplyOffset(float[,] transform_matrix, float offset_x, float offset_y, float offset_z)
        {
            float[,] translationMatrix = new float[,] { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 1, 0 }, { offset_x, offset_y, offset_z, 1 } };
            return multiply_matrix(transform_matrix, translationMatrix);
        }

        private static float[,] ApplyRotationX(float[,] transform_matrix, float angle)
        {
            float[,] rotationMatrix = new float[,] { { 1, 0, 0, 0 }, { 0, (float)Math.Cos(angle), (float)Math.Sin(angle), 0 },
                { 0, -(float)Math.Sin(angle), (float)Math.Cos(angle), 0}, { 0, 0, 0, 1} };
            return multiply_matrix(transform_matrix, rotationMatrix);
        }

        private static float[,] ApplyRotationY(float[,] transform_matrix, float angle)
        {
            float[,] rotationMatrix = new float[,] { { (float)Math.Cos(angle), 0, -(float)Math.Sin(angle), 0 }, { 0, 1, 0, 0 },
                { (float)Math.Sin(angle), 0, (float)Math.Cos(angle), 0}, { 0, 0, 0, 1} };
            return multiply_matrix(transform_matrix, rotationMatrix);
        }

        private static float[,] ApplyRotationZ(float[,] transform_matrix, float angle)
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
        /// ------------------------STATIC READY FIGURES-----------------------------
        ///

        //for camera
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

        static public Polygon Tetrahedron(float sz)
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

        static public Polygon Icosahedron(float sz)
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
                }
                else
                {
                    s.points.AddRange(new int[] { (i + 2) % ind, (i + 1) % ind, i });
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

            res.ScaleAroundCenter(sz, sz, sz);

            res.SetRandColor();
            return res;
        }
    }
}
