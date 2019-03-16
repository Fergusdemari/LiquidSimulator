using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template;

namespace template
{
    class Util
    {

        //Clampless RGB conversion
        public static int CreateColorInt(int red, int green, int blue) {
            return (red << 16) + (green << 8) + blue;
        }

        //Clamped RGB conversion
        public static int CreateColorIntSafe(int red, int green, int blue)
        {
            red = MathHelper.Clamp(red, 0, 255);
            green = MathHelper.Clamp(green, 0, 255);
            blue = MathHelper.Clamp(blue, 0, 255);
            return (red << 16) + (green << 8) + blue;
        }

        public static Matrix4 RotateCamera(Matrix4 transformation)
        {
            Matrix4 m = OpenTKApp.Camera;
            Matrix4 res = new Matrix4();
            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    float SumElements = 0.0f;
                    for (int k = 0; k < 4; ++k)
                    {
                        SumElements += transformation[i, k] * m[k, j];
                    }
                    res[i, j] = SumElements;
                }
            }
            OpenTKApp.Camera = res;
            OpenTKApp.ViewDirection = MatrixMultiplication4D(OpenTKApp.ViewDirectionOriginal, OpenTKApp.Camera);
            OpenTKApp.UpDirection = MatrixMultiplication4D(OpenTKApp.UpDirectionOriginal, OpenTKApp.Camera);

            return res;
        }

        public static Vector3 MatrixMultiplication4D(Vector3 vector, Matrix4 m)
        {
            Vector4 vector4 = new Vector4(vector, 1);
            Vector4 result = Vector4.Zero;
            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    result[j] += m[j, i] * vector4[i];
                }
            }
            return new Vector3(result.X, result.Y, result.Z);
        }
    }
}
