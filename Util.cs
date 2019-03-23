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
            // Up direction is fucked up
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
