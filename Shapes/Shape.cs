using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shapes
{
    abstract class Shape
    {
        public Vector3 Position;
        public Vector3 Velocity;

        public Shape(Vector3 position, Vector3 velocity)
        {
            Position = position;
            Velocity = velocity;
        }

        public abstract void Update(double deltaTime);
    }
}
