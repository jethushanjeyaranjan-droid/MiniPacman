using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

namespace MiniPacman
{
    public class PacmanPlayer
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Radius { get; private set; }
        public Vector Direction { get; private set; }
        public double Speed { get; set; }

        public Path PacmanShape { get; private set; }

        public PacmanPlayer(double startX, double startY, double radius, double speed)
        {
            X = startX;
            Y = startY;
            Radius = radius;
            Speed = speed;
            Direction = new Vector(1, 0);

            PacmanShape = new Path { Fill = Brushes.Gold, Stroke = Brushes.Orange, StrokeThickness = 1 };
        }

        public void Move(Vector inputDir, double dt)
        {
            if (inputDir.Length > 0.001)
            {
                Direction = inputDir;
                Direction.Normalize();
            }

            X += Direction.X * Speed * dt;
            Y += Direction.Y * Speed * dt;
        }

        public void KeepInsideCanvas(double canvasWidth, double canvasHeight)
        {
            if (X < -Radius) X = canvasWidth + Radius;
            if (X > canvasWidth + Radius) X = -Radius;
            if (Y < -Radius) Y = canvasHeight + Radius;
            if (Y > canvasHeight + Radius) Y = -Radius;
        }

        public void UpdateShape(double timeSeconds)
        {
            double mouthBase = 15.0;
            double mouthAmplitude = 30.0;
            double mouth = mouthBase + Math.Abs(System.Math.Sin(timeSeconds * 8.0)) * mouthAmplitude;

            double angle = System.Math.Atan2(Direction.Y, Direction.X);

            Point center = new Point(X, Y);
            double startAngle = angle + DegreeToRadian(mouth);
            double endAngle = angle - DegreeToRadian(mouth);

            Point startPoint = PointOnCircle(center, Radius, startAngle);
            Point endPoint = PointOnCircle(center, Radius, endAngle);

            PathFigure fig = new PathFigure { StartPoint = startPoint, IsClosed = true };
            fig.Segments.Add(new ArcSegment
            {
                Point = endPoint,
                Size = new Size(Radius, Radius),
                IsLargeArc = true,
                SweepDirection = SweepDirection.Clockwise
            });
            fig.Segments.Add(new LineSegment(center, true));

            PathGeometry geo = new PathGeometry();
            geo.Figures.Add(fig);
            PacmanShape.Data = geo;
        }

        private static Point PointOnCircle(Point center, double r, double angleRadians)
        {
            return new Point(center.X + r * System.Math.Cos(angleRadians),
                             center.Y + r * System.Math.Sin(angleRadians));
        }

        private static double DegreeToRadian(double deg) => deg * System.Math.PI / 180.0;
    }
}
