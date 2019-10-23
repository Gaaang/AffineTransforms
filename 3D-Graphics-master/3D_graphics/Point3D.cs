using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AffineTransformations
{
    public class Point3D
    {
        public float X, Y, Z;

        public Point3D()
        {
            X = 0;
            Y = 0;
            Z = 0;

        }
        public Point3D(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public Point3D(Point3D p)
        {
            if (p == null)
                return;
            this.X = p.X;
            this.Y = p.Y;
            this.Z = p.Z;
        }

        public override string ToString()
        {
            return String.Format("X:{0:f1} Y:{1:f1} Z:{2:f1}", X, Y, Z);
        }

        public static float Scalar(Point3D p1, Point3D p2)//скалярное произведение
        {
            return p1.X * p2.X + p1.Y * p2.Y + p1.Z * p2.Z;
        }

        public static Point3D Norm(Point3D p)//нормирование вектора
        {
            float l = (float)Math.Sqrt((float)(p.X * p.X + p.Y * p.Y + p.Z * p.Z));//длина вектора
            return new Point3D(p.X / l, p.Y / l, p.Z / l);
        }

        public static Point3D operator -(Point3D p1, Point3D p2)//координаты вектора P1P2
        {
            return new Point3D(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);

        }

        public static Point3D operator *(Point3D p1, Point3D p2)//вектор нормали к полигонам
        {
            return new Point3D(p1.Y * p2.Z - p1.Z * p2.Y, p1.Z * p2.X - p1.X * p2.Z, p1.X * p2.Y - p1.Y * p2.X);
        }
    }
}
