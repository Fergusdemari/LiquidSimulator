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
            Position += Velocity;

            // If it's below the floor, correct how deep it went in and bounce
            if (Position.Y < Game.floor)
            {
                float collisionDepth = Position.Y-Game.floor;
                Position.Y = Game.floor + -collisionDepth;

                //Bounce
                Velocity = -0.9f*Velocity;

            }

            
        }
    }
}
