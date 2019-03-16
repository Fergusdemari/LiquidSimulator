using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.IO;
using template;
using template.Shapes;

namespace Template
{

    class Game
    {
        public Shape[] points = new Shape[1000000];
        public static float gravity = -0.003f;
        public static float floor = -0.5f;
        // member variables
        public Surface screen;
        // initialize
        public void Init()
        {
            Random r = new Random();
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Sphere(new Vector3((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble()), Vector3.Zero);
            }
        }
        // tick: renders one frame
        public void Tick(FrameEventArgs e)
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i].Update(e.Time);
            }
        }

        public void RenderGL()
        {
            GL.Color3(1.0f, 0.0f, 0.0f);
            GL.Begin(PrimitiveType.Points);
            GL.PointSize(200);
            for (int i = 0; i < points.Length; i++)
            {
                GL.Vertex3(points[i].Position);
            }
            GL.End();
        }
    }

} // namespace Template