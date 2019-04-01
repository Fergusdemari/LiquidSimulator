using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using template.Shapes;

namespace Template
{

    class Game
    {
        public enum Mode
        {
            PARTICLES,  //Displays them as points
            CUBES,      //Displays the voxels they're in
            SHAPES      //Displays whatever shape we decided to give particles (tilted cube atm)
        }
        public Mode displayMode = Mode.PARTICLES;

        public static bool Recording = false;

        // Currently it's all done within 0-1. If you want it to be 0-3, set dim to 3 (In case of rounding errors maybe?)
        public static int dim = 1;

        // divided by 1000 because idk
        public static float gravity = -9.81f;
        // Stepsize of each frame. Set to very tiny if you want it to look silky smooth
        public float dt = 1.0f / 240f;
        
        //Debug showing
        bool showGrid = false;
        bool showBorders = true;
        // Keep threading false atm, issues with locking
        private bool threading = false;

        // Number of voxels in the grid per dimension
        static int voxels = 128;
        // Size of one voxel
        static float voxelSize = (float)dim / voxels;

        public static int numberOfPoints = 1000;


        public static Sphere[] particles = new Sphere[numberOfPoints];
        public static Dictionary<int, List<int>> grid = new Dictionary<int, List<int>>();


        public static int RNGSeed;

        // member variables
        public Surface screen;
        public FluidSim simulator;

        // initialize
        public void Init()
        {
            // Just some seed code to keep the same seed during the same run
            Random seedCreator = new Random();
            RNGSeed = seedCreator.Next(int.MaxValue);
            Console.WriteLine("Current RNG Seed is " + RNGSeed);

            printInstructions();
            //Creates random points with random velocities
            RestartScene(true);

            //start fluid simulator
            simulator = new FluidSim(numberOfPoints, dt, particles);
        }

        /// <summary>
        /// One update, ran every like 1/120 seconds
        /// </summary>
        /// <param name="e"></param>
        public void Tick(FrameEventArgs e)
        {
            // If set to threading, split the taskforce up, but if the amount of points is too small then there's no point
            if (threading && !(particles.Length < 100))
            {
                int PPT = 10;
                var options = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 8
                };
                Parallel.For(0, particles.Length / PPT, options, i =>
                  {
                      simulator.PropertiesUpdate(i * PPT, (i + 1) * PPT);
                  });
                Parallel.For(0, particles.Length / PPT, options, i =>
                {
                    simulator.ForcesUpdate(i * PPT, (i + 1) * PPT);
                });

                simulator.MovementUpdate();

                for (int i = 0; i < particles.Length; i++)
                {
                    particles[i].Update(dt);
                }

            }
            else
            {
                //particles[i].Update(dt);
                simulator.Update();

            }
        }

        /// <summary>
        /// Does the logic of what to draw
        /// </summary>
        public void RenderGL()
        {
            //Different displaymodes
            switch (displayMode)
            {
                case Mode.PARTICLES:
                    GL.Begin(PrimitiveType.Points);
                    GL.Color3(1.0f, 1.0f, 1f);
                    // Drawing of all spheres
                    for (int i = 0; i < particles.Length; i++)
                    {
                        GL.Vertex3(particles[i].Position);
                    }
                    GL.End();
                    break;
                case Mode.CUBES:
                    GL.Begin(PrimitiveType.Triangles);
                    GL.Color4(0.1f, 0.1, 1f, 1f);
                    /// Drawing of voxels when not empty
                    for (int i = 0; i < voxels * voxels * voxels; i++)
                    {
                        if (grid[i].Count > 0)
                        {
                            drawVoxel(i);
                        }
                    }
                    GL.End();
                    break;
                case Mode.SHAPES:
                    GL.Begin(PrimitiveType.Triangles);
                    GL.PointSize(2000);
                    GL.Color3(1.0f, 1.0f, 1.0f);
                    // Drawing of all spheres
                    for (int i = 0; i < particles.Length; i++)
                    {
                        Vector3[] shape = particles[i].getShape();
                        for (int j = 0; j < shape.Length; j++)
                        {
                            GL.Vertex3(shape[j]);
                        }
                    }
                    GL.End();
                    break;

            }
            if (showGrid)
            {
                drawGrid();
            }
            if (showBorders)
            {
                drawBorders();
            }


        }

        #region Distancefunctions and their overloads
        // gets distance from indices in pointList
        public static float getSquaredDistance(int i, int j)
        {
            return getSquaredDistance(particles[i].Position, particles[j].Position);
        }

        public static float getDistance(Vector3 a, Vector3 b)
        {
            return (float)Math.Sqrt(getSquaredDistance(a, b));
        }

        public static float getDistance(int i, int j)
        {
            return (float)Math.Sqrt(getSquaredDistance(i, j));
        }
        // gets distance from Points
        public static float getSquaredDistance(Vector3 a, Vector3 b)
        {
            float deltaX = a.X - b.X;
            float deltaY = a.Y - b.Y;
            float deltaZ = a.Z - b.Z;
            return deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;
        }
        #endregion

        #region DebugDrawMethods

        /// <summary>
        /// Draws just the borders surrounding the area, very useful
        /// </summary>
        private void drawBorders()
        {
            GL.Begin(PrimitiveType.Lines);
            GL.PointSize(1);
            GL.Color3(0.5f, 0, 0.5f);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 0, dim);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, dim, 0);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(dim, 0, 0);

            GL.Vertex3(0, 0, dim);
            GL.Vertex3(0, dim, dim);
            GL.Vertex3(0, 0, dim);
            GL.Vertex3(dim, 0, dim);

            GL.Vertex3(0, dim, 0);
            GL.Vertex3(dim, dim, 0);
            GL.Vertex3(0, dim, 0);
            GL.Vertex3(0, dim, dim);

            GL.Vertex3(dim, 0, 0);
            GL.Vertex3(dim, dim, 0);
            GL.Vertex3(dim, 0, 0);
            GL.Vertex3(dim, 0, dim);

            GL.Vertex3(dim, dim, dim);
            GL.Vertex3(0, dim, dim);
            GL.Vertex3(dim, dim, dim);
            GL.Vertex3(dim, dim, 0);
            GL.Vertex3(dim, dim, dim);
            GL.Vertex3(dim, 0, dim);


            GL.End();
        }


        /// <summary>
        /// Draws outline of every voxel
        /// </summary>
        public void drawGrid()
        {
            GL.Begin(PrimitiveType.Lines);
            GL.PointSize(1);
            GL.Color3(0.5f, 0.5f, 0.5f);

            float inv = (float)dim / voxels;
            for (float x = 0; x <= dim; x += inv)
            {
                for (float y = 0; y <= dim; y += inv)
                {
                    for (float z = 0; z <= dim; z += inv)
                    {
                        if (x + inv <= dim)
                        {
                            GL.Vertex3(x, y, z);
                            GL.Vertex3(x + inv, y, z);
                        }
                        if (y + inv <= dim)
                        {
                            GL.Vertex3(x, y, z);
                            GL.Vertex3(x, y + inv, z);
                        }

                        if (z + inv <= dim)
                        {
                            GL.Vertex3(x, y, z);
                            GL.Vertex3(x, y, z + inv);
                        }


                    }
                }
            }

            GL.End();
        }

        #endregion

        #region grid and indexing related stuff
        /// <summary>
        /// Given a Vector with values between [0, 1], return the index of the voxel that point is in.
        /// </summary>
        /// <returns>The correct index, otherwise -2</returns>
        public static int getParticleVoxelIndex(Vector3 p)
        {
            // Scale from [0, 1] to [0, #voxels]
            p *= voxels;
            int x = (int)p.X;
            int y = (int)p.Y;
            int z = (int)p.Z;

            // If the xyz coordinates are outside the total cube, return -2
            if (x < 0 || y < 0 || z < 0 ||
                x >= voxels || y >= voxels || z >= voxels)
            {
                return -2;
            }
            else
            {
                return x + y * voxels + z * voxels * voxels;
            }
        }

        /// <summary>
        /// Overload method but can be called using a particles position
        /// </summary>
        /// <param name="Position"></param>
        /// <returns></returns>
        public static int[] neighborsIndicesConcatenated(Vector3 Position)
        {
            int x = (int)(Position.X * voxels);
            int y = (int)(Position.Y * voxels);
            int z = (int)(Position.Z * voxels);
            return neighborsIndicesConcatenated(x, y, z);
        }



        /// <summary>
        /// Given one position, return all the particles in the neighboring boxes, INCLUDING YOURSELF
        /// </summary>
        /// <param name="voxels"></param>
        /// <returns></returns>
        public static int[] neighborsIndicesConcatenated(int x, int y, int z)
        {
            int[] voxels = getNeighborIndices(x, y, z);

            //Gets how many particles there are in total in all the neighbors
            int total = 0;
            for (int i = 0; i < voxels.Length; i++)
            {
                total += grid[voxels[i]].Count;
            }

            //Copy spheres into the new list
            int[] res = new int[total];
            int counter = 0;
            for (int i = 0; i < voxels.Length; i++)
            {
                for (int j = 0; j < grid[voxels[i]].Count; j++)
                {
                    res[counter] = grid[voxels[i]][j];
                    counter++;
                }
            }
            return res;
        }

        /// <summary>
        /// Returns all neighbors indices, return length may vary based on if it doesn't have neighbors;
        /// </summary>
        /// <param name="x">Value between 0 and #voxels</param>
        /// <param name="y">Value between 0 and #voxels</param>
        /// <param name="z">Value between 0 and #voxels</param>
        /// <returns>Returns a list of the indices of all neighbor voxels. The indices are the ones used in Grid</returns>
        public static int[] getNeighborIndices(int x, int y, int z, int radius = 1)
        {
            int[] res = new int[27];
            int counter = 0;
            for (int xi = -radius; xi <= radius; xi++)
            {
                for (int yi = -radius; yi <= radius; yi++)
                {
                    for (int zi = -radius; zi <= radius; zi++)
                    {
                        res[counter] = getVoxelIndex(x + xi, y + yi, z + zi);
                        counter++;
                    }
                }
            }
            int validOnes = 0;
            for (int i = 0; i < res.Length; i++)
            {
                if (res[i] >= 0 && res[i] < voxels * voxels * voxels)
                {
                    validOnes++;
                }
            }
            int[] res2 = new int[validOnes];
            int counter1 = 0;
            for (int i = 0; i < res.Length; i++)
            {
                if (res[i] >= 0 && res[i] < voxels * voxels * voxels)
                {
                    res2[counter1] = res[i];
                    counter1++;
                }
            }
            return res2;
        }

        /// <summary>
        /// Overload for (x, y, z) version.
        /// </summary>
        /// <param name="i"> Index in voxel grid</param>
        /// <returns></returns>
        public static int[] getNeighborIndices(int i)
        {
            Vector3 position = getPosition(i);
            int x = (int)position.X;
            int y = (int)position.Y;
            int z = (int)position.Z;
            return getNeighborIndices(x, y, z);
        }

        /// <summary>
        /// Based on XYZ values between 0 and #voxels, gets the index that position has in the voxel grid.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>Returns the idnex of a voxel, and -2 if it's not in the grid </returns>
        private static int getVoxelIndex(int x, int y, int z)
        {
            // If one is below 0, it's not in the grid
            if (x < 0 || y < 0 || z < 0)
                return -2;

            int size = voxels * voxels * voxels;
            // If one is above the allowed gridsize, it's not in the grid
            if (x > size || y > size || z > size)
                return -2;


            return x + y * voxels + z * voxels * voxels;
        }

        /// <summary>
        /// Gets the grid's position vector based on it's index in Grid
        /// </summary>
        public static Vector3 getPosition(int i)
        {
            int temp = i;
            int z = temp / (voxels * voxels);
            temp -= z * voxels * voxels;
            int y = temp / voxels;
            temp -= y * voxels;
            int x = temp;
            return new Vector3(x * voxelSize, y * voxelSize, z * voxelSize);
        }
        #endregion

        /// <summary>
        /// Draws a voxel of the grid using it's index
        /// </summary>
        /// <param name="i"></param>
        public void drawVoxel(int i)
        {
            Vector3 pos = getPosition(i);


            // Backside
            GL.Vertex3(pos);
            GL.Vertex3(pos.X + voxelSize, pos.Y, pos.Z);
            GL.Vertex3(pos.X + voxelSize, pos.Y + voxelSize, pos.Z);

            GL.Vertex3(pos);
            GL.Vertex3(pos.X + voxelSize, pos.Y + voxelSize, pos.Z);
            GL.Vertex3(pos.X, pos.Y + voxelSize, pos.Z);

            // Frontside
            GL.Vertex3(pos.X, pos.Y, pos.Z + voxelSize);
            GL.Vertex3(pos.X + voxelSize, pos.Y, pos.Z + voxelSize);
            GL.Vertex3(pos.X + voxelSize, pos.Y + voxelSize, pos.Z + voxelSize);

            GL.Vertex3(pos.X, pos.Y, pos.Z + voxelSize);
            GL.Vertex3(pos.X + voxelSize, pos.Y + voxelSize, pos.Z + voxelSize);
            GL.Vertex3(pos.X, pos.Y + voxelSize, pos.Z + voxelSize);

            // Top
            GL.Vertex3(pos.X, pos.Y + voxelSize, pos.Z);
            GL.Vertex3(pos.X + voxelSize, pos.Y + voxelSize, pos.Z);
            GL.Vertex3(pos.X + voxelSize, pos.Y + voxelSize, pos.Z + voxelSize);

            GL.Vertex3(pos.X, pos.Y + voxelSize, pos.Z);
            GL.Vertex3(pos.X + voxelSize, pos.Y + voxelSize, pos.Z + voxelSize);
            GL.Vertex3(pos.X, pos.Y + voxelSize, pos.Z + voxelSize);

            // Bottom
            GL.Vertex3(pos.X, pos.Y, pos.Z);
            GL.Vertex3(pos.X + voxelSize, pos.Y, pos.Z);
            GL.Vertex3(pos.X + voxelSize, pos.Y, pos.Z + voxelSize);

            GL.Vertex3(pos.X, pos.Y, pos.Z);
            GL.Vertex3(pos.X + voxelSize, pos.Y, pos.Z + voxelSize);
            GL.Vertex3(pos.X, pos.Y, pos.Z + voxelSize);

            // Left
            GL.Vertex3(pos);
            GL.Vertex3(pos.X, pos.Y, pos.Z + voxelSize);
            GL.Vertex3(pos.X, pos.Y + voxelSize, pos.Z + voxelSize);

            GL.Vertex3(pos);
            GL.Vertex3(pos.X, pos.Y + voxelSize, pos.Z + voxelSize);
            GL.Vertex3(pos.X, pos.Y + voxelSize, pos.Z);

            // Right
            GL.Vertex3(pos.X + voxelSize, pos.Y, pos.Z);
            GL.Vertex3(pos.X + voxelSize, pos.Y, pos.Z + voxelSize);
            GL.Vertex3(pos.X + voxelSize, pos.Y + voxelSize, pos.Z + voxelSize);

            GL.Vertex3(pos.X + voxelSize, pos.Y, pos.Z);
            GL.Vertex3(pos.X + voxelSize, pos.Y + voxelSize, pos.Z + voxelSize);
            GL.Vertex3(pos.X + voxelSize, pos.Y + voxelSize, pos.Z);
        }

        /// <summary>
        /// Prints instructions at the start
        /// </summary>
        private void printInstructions()
        {
            Console.WriteLine("[R]         - Reset the Camera");
            Console.WriteLine("[1]         - Respawn the particles with the same seed as last time");
            Console.WriteLine("[2]         - Respawn the particles with a new seed");
            Console.WriteLine("[WASD]      - Walk around");
            Console.WriteLine("[Arrowkeys] - Look around");

        }

        /// <summary>
        /// Respawns the balls and empties the grid lists
        /// </summary>
        /// <param name="sameSeed"> If set to true, the same seed as previous spawn will be used </param>
        public static void RestartScene(bool sameSeed)
        {
            if (!sameSeed)
            {
                Random seedCreator = new Random();
                RNGSeed = seedCreator.Next(int.MaxValue);
                Console.WriteLine("RNGSeed has been rerolled to " + RNGSeed);
            }

            Console.Write("Setting up Grid...");
            grid = new Dictionary<int, List<int>>();
            particles = new Sphere[numberOfPoints];
            // Creates the grid list empty
            for (int i = 0; i < voxels * voxels * voxels; i++)
            {
                grid.Add(i, new List<int>());
            }
            Console.WriteLine(" Done.");

            Console.Write("Generating particles...");
            //Creates random points with random velocities
            Random r = new Random();
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i] = new Sphere(i, new Vector3(((float)r.NextDouble() / 8 + 0.4375f * dim), ((float)r.NextDouble() / 4 * dim), ((float)r.NextDouble() / 8 + 0.4375f * dim)),
                                       Vector3.Zero, 0.01f);
                particles[i].color = particles[i].Position / dim;
                particles[i].Mass = 0.3f;
                particles[i].NetForce = new Vector3(0, 0, 0);
            }
            Console.WriteLine(" Done.");
        }
    }
}