﻿using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Template;

namespace template.Shapes
{
    public class Sphere
    {
        public float Radius;

        // Water like variables
        public float Mass;
        public Vector3 Velocity;
        public Vector3 NetForce;
        public float Density;
        public float Pressure;
        public bool verbose = false;
        public float damping = 2; 
        public Vector3 normal;
        // Meta info
        public int ListIndex;
        public int currentVoxelIndex = -2;

        // Basic object values
        public Vector3 color;

        // Position of this Sphere, and a property below it, so that whenever you change a particle's position, it automatically updates the grid position.
        private Vector3 position;
        public Vector3 Position
        {
            get { return position; }
            set
            {
                position = value;
                updateGrid();
            }
        }

        Vector3 prevPos;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listIndex"> It's position in the list of points. This remains the same always atm</param>
        public Sphere(int listIndex, Vector3 position, Vector3 velocity, float radius = 0.01f, float mass = 1, float density = 1, float pressure = 1)
        {
            ListIndex = listIndex;
            Position = position;
            Velocity = velocity;
            Radius = radius;
            Mass = mass;
            Density = density;
            Pressure = pressure;
        }

        public void Update(double timeStep)
        {
            ResolveCollisions();
        }

        // Updates this sphere's position in the grid
        private void updateGrid()
        {
            #region Update location in grid
            int newIndex = Game.getParticleVoxelIndex(Position);
            if (currentVoxelIndex != newIndex)
            {
                // If the new position is within a voxel, add it to it
                if (newIndex != -2)
                {
                    Game.grid[newIndex].Add(ListIndex);
                }
                // If the particle was in another voxel before, remove it
                if (currentVoxelIndex != -2)
                {
                    Game.grid[currentVoxelIndex].Remove(ListIndex);
                }
                currentVoxelIndex = newIndex;
            }
            #endregion
        }

        /// <summary>
        /// Currently a very non physically accurate collision method that keeps particles in the box
        /// </summary>
        private void ResolveCollisions()
        {
            #region wallCollision

            // For every wall check if the particle is hitting it and going toward the outside
            if (Position.X < 0)
            {
                Position = new Vector3(0, Position.Y, Position.Z);
                Velocity = new Vector3(-1 * Velocity.X, Velocity.Y, Velocity.Z);
            }
            if (Position.Y < 0)
            {
                Position = new Vector3(Position.X, 0, Position.Z);
                Velocity = new Vector3(Velocity.X, -1*Velocity.Y/damping, Velocity.Z);
            }
            if (Position.Z < 0)
            {
                Position = new Vector3(Position.X, Position.Y, 0);
                Velocity = new Vector3(Velocity.X, Velocity.Y, -1*Velocity.Z);
            }

            if (Position.X >= Game.dim) {
                Position = new Vector3(Game.dim-0.001f, Position.Y, Position.Z);
                Velocity = new Vector3(-1 * Velocity.X, Velocity.Y, Velocity.Z);
            }
            if (Position.Y >= Game.dim) {
                Position = new Vector3(Position.X, Game.dim-0.001f, Position.Z);
                Velocity = new Vector3(Velocity.X, -1*Velocity.Y, Velocity.Z);
            }
            if (Position.Z >= Game.dim) {
                Position = new Vector3(Position.X, Position.Y, Game.dim-0.001f);
                Velocity = new Vector3(Velocity.X, Velocity.Y, -1 * Velocity.Z);
            }

            #endregion

            //// Interparticle Collision TODO
            //int[] neighbors = Game.neighborsIndicesConcatenated(Position);
            //for (int i = 0; i < neighbors.Length; i++) {
            //    float dist = Game.getSquaredDistance(Position, Game.particles[i].Position);
            //    if (dist < (Radius + Radius) * (Radius + Radius)) {
            //        // Resolve the collision
            //    }
            //}
            // TODO Collision with fellow particles
        }

        /// <summary>
        /// Updates particle positions, and updates their position
        /// </summary>
        /// <param name="timeStep"></param>
        private void AdvanceParticles(double timeStep)
        {
            prevPos = Position;
            Position += Velocity * (float)timeStep;
        }

        /// <summary>
        /// Applies general forces, currently only gravity
        /// </summary>
        public void ApplyExternalForces()
        {
            Velocity.Y += (float)Game.gravity;
        }

        /// <summary>
        /// Makes a shape to represent this particle
        /// </summary>
        /// <returns> Returns a list of vertices for a CUBE shape </returns>
        public Vector3[] getShape()
        {
            Vector3[] v = new Vector3[6];
            v[0] = Position + new Vector3(0, 0, -Radius);
            v[1] = Position + new Vector3(0, 0, Radius);
            v[2] = Position + new Vector3(0, -Radius, 0);
            v[3] = Position + new Vector3(0, Radius, 0);
            v[4] = Position + new Vector3(-Radius, 0, 0);
            v[5] = Position + new Vector3(Radius, 0, 0);
            //return vertices;
            Vector3[] t = new Vector3[24];
            t[0] = v[0];
            t[1] = v[2];
            t[2] = v[4];

            t[3] = v[0];
            t[4] = v[4];
            t[5] = v[3];

            t[6] = v[0];
            t[7] = v[5];
            t[8] = v[3];

            t[9] = v[0];
            t[10] = v[5];
            t[11] = v[2];

            t[12] = v[1];
            t[13] = v[2];
            t[14] = v[4];

            t[15] = v[1];
            t[16] = v[2];
            t[17] = v[5];

            t[18] = v[1];
            t[19] = v[3];
            t[20] = v[4];

            t[21] = v[1];
            t[22] = v[3];
            t[23] = v[5];
            return t;
        }
    }
}
