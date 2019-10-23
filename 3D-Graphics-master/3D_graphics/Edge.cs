using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AffineTransformations
{
    public class Edge
    {
        public Polygon polygon = null;//многоугольник,к которому относится ребро
        public List<int> points = new List<int>();//индексы
        public Pen penEdge = new Pen(Color.Black);
        public Point3D Normal;
        public bool IsVisible = false;

        public Edge(Polygon polyg = null)
        {
            polygon = polyg;
        }

        public Edge(Edge edge)
        {
            points = new List<int>(edge.points);
            polygon = edge.polygon;
            penEdge = edge.penEdge.Clone() as Pen;
            Normal = new Point3D(edge.Normal);
            IsVisible = edge.IsVisible;
        }

        public Point3D getPoint(int ind)
        {
            if (polygon != null)
                return polygon.points[points[ind]];
            return null;
        }

        public static Point3D Norm(Edge edge)
        {
            if (edge.points.Count() < 3)
                return new Point3D(0, 0, 0);
            Point3D U = edge.getPoint(1) - edge.getPoint(0);
            Point3D V = edge.getPoint(edge.points.Count - 1) - edge.getPoint(0);
            Point3D normal = V * U;
            return Point3D.Norm(normal);
        }

        public void CalculateSideNormal()
        {
            Normal = Norm(this);
        }

        public void CalculateVisibilty(Point3D cntr)
        {
            if (Normal == null)
                IsVisible = true;
            else
                IsVisible = Point3D.Scalar(cntr - getPoint(0), Normal) < 0;

        }
    }
}
