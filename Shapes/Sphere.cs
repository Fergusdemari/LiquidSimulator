using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template;

namespace template.Shapes
{
    class Sphere : Shape
    {

        public float Radius;

        public Sphere(Vector3 position, Vector3 velocity, float radius = 0.01f) : base(position, velocity)
        {
            Radius = radius;
        }

        public override void Update(double deltaTime)
        {
            //Gravity only
            Velocity += new Vector3(0, (float)(Game.gravity * deltaTime), 0);
            Vector3 prevVel = Velocity;
            Position += Velocity;
            
            if (Position.Y < Game.floor) {
                Position.Y = Game.floor + Math.Abs(Game.floor - Position.Y);

                //Bounce
                Velocity.Y *= -1;
            }
            if (Position.Y > 1) {
                Position.Y = 1 - Math.Abs(Position.Y - 1);
                Velocity.Y *= -1;
            }
            if (Position.X < 0) {
                Position.X *= -1;
                Velocity.X *= -1;
            } else if (Position.X > 1) {
                Position.X = 1 - Math.Abs(Position.X - 1);
                Velocity.X *= -1;
            }
            if (Position.Z < 0) {
                Position.Z *= -1;
                Velocity.Z *= -1;
            } else if (Position.Z > 1) {
                Position.Z = 1 - Math.Abs(Position.Z - 1);
                Velocity.Z *= -1;
            }
            if (prevVel != Velocity) {
                Velocity *= 0.9f;
            }
        }
    }
}
