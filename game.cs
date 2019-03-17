using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Threading.Tasks;
using template.Shapes;

namespace Template {

    class Game {
        public Shape[] points = new Shape[200000];
        public static float gravity = -0.03f;
        public static float floor = -0.5f;
        private bool threading = true;
        // member variables
        public Surface screen;
        // initialize
        public void Init() {
            Random r = new Random();
            for (int i = 0; i < points.Length; i++) {
                points[i] = new Sphere(new Vector3((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble()),
                                       new Vector3((float)r.NextDouble()/60, (float)r.NextDouble()/60, (float)r.NextDouble()/60));
                points[i].color = points[i].Position;
            }
        }
        // tick: Does one frame worth of work
        public void Tick(FrameEventArgs e) {
            if (threading) {
                int workPerTask = 1000;
                Parallel.For(0, points.Length/workPerTask, new ParallelOptions { MaxDegreeOfParallelism = 8 }, j => {
                    for (int i = j*workPerTask; i < (j+1)*workPerTask; i++) {
                        points[i].Update(e.Time);
                    }
                });
            } else {
                for (int i = 0; i < points.Length; i++) {
                    points[i].Update(e.Time);
                }
            }

        }

        public void RenderGL() {
            GL.Begin(PrimitiveType.Points);
            GL.PointSize(200);
            for (int i = 0; i < points.Length; i++) {
                GL.Color3(points[i].color);
                GL.Vertex3(points[i].Position);
            }
            GL.End();
        }
    }

} // namespace Template