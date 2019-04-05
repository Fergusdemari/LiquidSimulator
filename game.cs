using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Template {
    struct Emitter {
        public Vector3 position;
        public float radius;
        public int emissionRate;
        public int delay;
        public int tickCounter;
        public Vector3 velocity;
    }

    class Game {
        public enum Mode {
            PARTICLES,  //Displays them as points
            CUBES,      //Displays the voxels they're in
            SHAPES      //Displays whatever shape we decided to give particles (tilted cube atm)
        }

        #region originalGameVars
        public static int numberOfPoints = 30000;
        public static Mode displayMode = Mode.PARTICLES;
        private bool threading = true;
        public static bool Recording = false;

        public static bool running = false;
        public bool step = false;
        // Currently it's all done within 0-1. If you want it to be 0-3, set dim to 3 (In case of rounding errors maybe?)
        public static float dim = 1.0f;

        // divided by 1000 because idk
        public static float gravity = -9.81f;
        // Stepsize of each frame. Set to very tiny if you want it to look silky smooth
        public float dt = 1.0f / 25f;

        //Debug showing
        bool showGrid = false;
        bool showBorders = true;


        // Number of voxels in the grid per dimension
        static int voxels = 64;
        static int visualisationVoxels = 64;
        bool[] visualisationVoxelSwitches = new bool[visualisationVoxels * visualisationVoxels * visualisationVoxels];
        // Size of one voxele
        static float visVoxelSize = dim / visualisationVoxels;
        static float voxelSize = dim / voxels;

        public static int currentPoints = 0;

        public Emitter[] emitters;

        //public static Sphere[] particles = new Sphere[numberOfPoints];
        public static Dictionary<int, List<int>> grid = new Dictionary<int, List<int>>();


        public static int RNGSeed;

        // member variables
        public Surface screen;
        public FluidSim simulator;
        #endregion

        #region Particles
        // Amount of particles
        public static int N = numberOfPoints;

        // Material attributes of particles
        public static float Mass = 0.3f;
        public static float Radius = 0.002f;
        public static float Damping = 2;
        public static float[] Density;
        public static float[] Pressure;

        // Mechanical attributes of particles
        /// Position, never change these manually, only through methods ChangePosition and ChangeX etc
        /// Because the grid needs to be updated
        public static float[] PosX;
        public static float[] PosY;
        public static float[] PosZ;
        #region PositionMethods
        public static void ChangePosition(int i, float x, float y, float z) {
            FluidSim.isFucked(x);
            FluidSim.isFucked(y);
            FluidSim.isFucked(z);
            PosX[i] = x;
            PosY[i] = y;
            PosZ[i] = z;
            UpdateGrid(i);
        }
        public static void ChangeX(int i, float x) {
            PosX[i] = x;
            UpdateGrid(i);
        }
        public static void ChangeY(int i, float y) {
            PosY[i] = y;
            UpdateGrid(i);
        }
        public static void ChangeZ(int i, float z) {
            PosZ[i] = z;
            UpdateGrid(i);
        }

        public static void ChangePosition(int i, Vector3 v) {
            ChangePosition(i, v.X, v.Y, v.Z);
        }

        public static void AddPosition(int i, Vector3 v) {
            AddPosition(i, v.X, v.Y, v.Z);
        }

        public static void AddPosition(int i, float x, float y, float z) {
            ChangePosition(i, PosX[i] + x, PosY[i] + y, PosZ[i] + z);
        }


        public static Vector3 GetVectorPosition(int i) {
            return new Vector3(PosX[i], PosY[i], PosZ[i]);
        }

        public static void UpdateGrid(int i) {
            int newVoxelIndex = GetVoxelIndexFromParticleIndex(i);
            if (currentVoxelIndex[i] != newVoxelIndex) {
                if (newVoxelIndex != -2)
                    Game.grid[newVoxelIndex].Add(i);

                if (currentVoxelIndex[i] != -2)
                    Game.grid[currentVoxelIndex[i]].Remove(i);

                currentVoxelIndex[i] = newVoxelIndex;
            }
        }

        public static int GetVoxelIndexFromParticleIndex(int i) {
            return GetVoxelIndexFromParticlePosition(new Vector3(PosX[i], PosY[i], PosZ[i]));
        }

        public static int GetVoxelIndexFromParticlePosition(Vector3 p) {
            // Scale from [0, 1] to [0, #voxels]
            p *= Game.voxels;
            int x = (int)p.X;
            int y = (int)p.Y;
            int z = (int)p.Z;

            // If the xyz coordinates are outside the total cube, return -2
            if (x < 0 || y < 0 || z < 0 ||
                x >= Game.voxels || y >= Game.voxels || z >= Game.voxels) {
                return -2;
            } else {
                return x + y * Game.voxels + z * Game.voxels * Game.voxels;
            }
        }
        #endregion

        /// Velocity
        public static float[] VelX;
        public static float[] VelY;
        public static float[] VelZ;
        #region VelocityMethods

        public static void ChangeVelocity(int i, float x, float y, float z) {
            VelX[i] = x;
            VelY[i] = y;
            VelZ[i] = z;
        }

        public static void AddVelocity(int i, float x, float y, float z) {
            VelX[i] += x;
            VelY[i] += y;
            VelZ[i] += z;
        }

        public static void AddVelocity(int i, Vector3 v) {
            AddVelocity(i, v.X, v.Y, v.Z);
        }

        public static void ChangeVelocity(int i, Vector3 v) {
            ChangeVelocity(i, v.X, v.Y, v.Z);
        }

        public static Vector3 GetVectorVelocity(int i) {
            return new Vector3(VelX[i], VelY[i], VelZ[i]);
        }
        #endregion
        /// Acceleration
        public static float[] AccelX;
        public static float[] AccelY;
        public static float[] AccelZ;

        /// Net Force
        public static float[] NFX;
        public static float[] NFY;
        public static float[] NFZ;
        #region NetForceMethods
        public static void ChangeNetForce(int i, float x, float y, float z) {
            NFX[i] = x;
            NFY[i] = y;
            NFZ[i] = z;
        }

        public static void AddNetForce(int i, float x, float y, float z) {
            NFX[i] += x;
            NFY[i] += y;
            NFZ[i] += z;
        }


        public static void AddNetForce(int i, Vector3 v) {
            AddNetForce(i, v.X, v.Y, v.Z);
        }

        public static void ChangeNetForce(int i, Vector3 v) {
            ChangeNetForce(i, v.X, v.Y, v.Z);
        }

        public static Vector3 GetVectorNetForce(int i) {
            return new Vector3(NFX[i], NFY[i], NFZ[i]);
        }
        #endregion

        public static float[] R;
        public static float[] G;
        public static float[] B;

        public static float[] NormX;
        public static float[] NormY;
        public static float[] NormZ;
        #region NormalMethods
        public static Vector3 GetVectorNormal(int i) {
            return new Vector3(NormX[i], NormY[i], NormZ[i]);
        }
        #endregion

        // Programming related
        public static bool[] verbose;
        public static int[] currentVoxelIndex;
        #endregion

        // initialize
        public void Init() {
            InitSize();
            // Just some seed code to keep the same seed during the same run
            Random seedCreator = new Random();
            RNGSeed = seedCreator.Next(int.MaxValue);
            Console.WriteLine("Current RNG Seed is " + RNGSeed);

            printInstructions();
            //Creates random points with random velocities
            grid = new Dictionary<int, List<int>>();
            for (int i = 0; i < voxels * voxels * voxels; i++) {
                grid.Add(i, new List<int>());
            }
            emitters = new Emitter[0];
            //emitters[0] = new Emitter();
            //emitters[0].position = new Vector3(0.0f, 0.95f, 0.5f);
            //emitters[0].radius = 0.1f;
            //emitters[0].velocity = new Vector3(0.4f, -0.2f, 0.01f);
            //emitters[0].emissionRate = 2;
            //emitters[0].delay = 5;
            //emitters[0].tickCounter = 0;


            RestartScene(true);

            //start fluid simulator
            simulator = new FluidSim(numberOfPoints, dt);
            //int[] t = neighborsIndicesConcatenated();
        }
        public static void InitSize() {
            // Material attributes of particles
            Mass = 1;
            Density = new float[N];
            Pressure = new float[N];

            PosX = new float[N];
            PosY = new float[N];
            PosZ = new float[N];

            VelX = new float[N];
            VelY = new float[N];
            VelZ = new float[N];

            AccelX = new float[N];
            AccelY = new float[N];
            AccelZ = new float[N];

            NFX = new float[N];
            NFY = new float[N];
            NFZ = new float[N];

            R = new float[N];
            G = new float[N];
            B = new float[N];

            NormX = new float[N];
            NormY = new float[N];
            NormZ = new float[N];

            // Programming related
            verbose = new bool[N];
            currentVoxelIndex = new int[N];
            for (int i = 0; i < N; i++) {
                currentVoxelIndex[i] = -2;
                //Density[i] = 0.001f;
            }
        }

        /// <summary>
        /// One update, ran every like 1/120 seconds
        /// </summary>
        /// <param name="e"></param>
        public void Tick(FrameEventArgs e) {
            Random r = new Random();
            #region emitters
            if (currentPoints < numberOfPoints && (running || step)) {
                for (int i = 0; i < emitters.Length; i++) {
                    if (emitters[i].tickCounter == 0) {
                        for (int j = 0; j < emitters[i].emissionRate; j++) {
                            float rad = (float)Math.Sqrt(r.NextDouble()) * emitters[i].radius;
                            float angle = (float)r.NextDouble() * (float)Math.PI * 2;
                            Vector3 pos = new Vector3(emitters[i].position.X, emitters[i].position.Y + rad * (float)Math.Cos(angle), emitters[i].position.Z + rad * (float)Math.Sin(angle));
                            
                            ChangePosition(currentPoints, pos.X, pos.Y, pos.Z);

                            VelX[currentPoints] = emitters[i].velocity.X;
                            VelY[currentPoints] = emitters[i].velocity.Y;
                            VelZ[currentPoints] = emitters[i].velocity.Z;

                            verbose[i] = true;

                            R[currentPoints] = 1;
                            currentPoints++;
                        }
                    }
                    emitters[i].tickCounter++;
                    if (emitters[i].tickCounter > emitters[i].delay) {
                        emitters[i].tickCounter = 0;
                    }
                }
            }
            #endregion

            // If set to threading, split the taskforce up, but if the amount of points is too small then there's no point
            if (threading && !(N < 100)) {
                if (running || step) {
                    int PPT = 8;
                    var options = new ParallelOptions() {
                        MaxDegreeOfParallelism = 8
                    };
                    Parallel.For(0, currentPoints / PPT, options, i => {
                        simulator.PropertiesUpdate(i * PPT, (i + 1) * PPT);
                    });
                    Parallel.For(0, currentPoints / PPT, options, i => {
                        simulator.ForcesUpdate(i * PPT, (i + 1) * PPT);
                    });
                    simulator.MovementUpdate();
                    for (int i = 0; i < currentPoints; i++) {
                        ResolveCollisions(i);
                    }
                    if (displayMode == Mode.CUBES) {
                        Parallel.For(0, (visualisationVoxels * visualisationVoxels * visualisationVoxels) / PPT, options, i => {
                            calcVoxelDensities(i * PPT, (i + 1) * PPT);
                        });
                    }
                    step = false;
                }
            } else {
                //particles[i].Update(dt);
                if (running || step) {
                    simulator.Update();
                    step = false;
                }

            }
        }

        /// <summary>
        /// Calculates a density value for each voxel to decide whether it should be drawn
        /// </summary>
        public void calcVoxelDensities(int start, int end) {
            for (int i = start; i < end; i++) {
                Vector3 position = getVisPosition(i);
                position += new Vector3(visVoxelSize / 2.0f, visVoxelSize / 2.0f, visVoxelSize / 2.0f);
                float density = FluidSim.calcDensity(position);
                if (density > 20.0) {
                    //Console.WriteLine(position + " " + density);
                    visualisationVoxelSwitches[i] = true;
                } else {
                    visualisationVoxelSwitches[i] = false;
                }
            }
        }

        /// <summary>
        /// Does the logic of what to draw
        /// </summary>
        public void RenderGL() {
            //Different displaymodes
            switch (displayMode) {
                case Mode.PARTICLES:

                    // Drawing of all spheres
                    int vboID;
                    float[] allVerts = new float[numberOfPoints * 3];
                        for (int i = 0; i < numberOfPoints; i+=3) {
                            allVerts[i] = PosX[i];
                            allVerts[i+1] = PosY[i];
                            allVerts[i+2] = PosZ[i];
                        }
                    
                    vboID = GL.GenBuffer();
                    GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
                    GL.BufferData<float>(BufferTarget.ArrayBuffer, (IntPtr)(allVerts.Length * 4),
                        allVerts, BufferUsageHint.StaticDraw);

                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, 0);

                    GL.DrawArrays(PrimitiveType.Points, 0, allVerts.Length);

                    break;
                case Mode.CUBES:
                    GL.Begin(PrimitiveType.Triangles);
                    GL.Color4(0.1f, 0.1, 1f, 1f);
                    /// Drawing of voxels when not empty
                    for (int i = 0; i < voxels*voxels*voxels; i++) {
                        if (grid[i].Count > 0) {
                            drawVoxel(i);
                        }
                    }
                    //for (int i = 0; i < visualisationVoxels * visualisationVoxels * visualisationVoxels; i++) {
                    //    
                    //    if (visualisationVoxelSwitches[i]) {
                    //        drawVoxel(i);
                    //    }
                    //}
                    GL.End();
                    break;
                case Mode.SHAPES:
                    GL.Begin(PrimitiveType.Triangles);
                    GL.PointSize(2000);
                    // Drawing of all spheres
                    for (int i = 0; i < currentPoints; i++) {
                        Vector3[] shape = getShape(i);
                        for (int j = 0; j < shape.Length; j++) {
                            GL.Color3(R[i], G[i], B[i]);
                            GL.Vertex3(shape[j]);
                        }
                    }
                    GL.End();
                    break;

            }
            if (showGrid) {
                drawGrid();
            }
            if (showBorders) {
                drawBorders();
            }


        }

        #region Distancefunctions and their overloads
        // gets distance from indices in pointList
        public static float getSquaredDistance(int i, int j) {
            return getSquaredDistance(GetVectorPosition(i), GetVectorPosition(j));
        }

        public static float getDistance(Vector3 a, Vector3 b) {
            return (float)Math.Sqrt(getSquaredDistance(a, b));
        }

        public static float getDistance(int i, int j) {
            return (float)Math.Sqrt(getSquaredDistance(i, j));
        }
        // gets distance from Points
        public static float getSquaredDistance(Vector3 a, Vector3 b) {

            float deltaX = a.X - b.X;
            float deltaY = a.Y - b.Y;
            float deltaZ = a.Z - b.Z;
            double temp = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;
            if (double.IsNaN(temp)) {
                //Console.WriteLine();
            }
            return (float)temp;
        }
        #endregion

        #region DebugDrawMethods

        /// <summary>
        /// Draws just the borders surrounding the area, very useful
        /// </summary>
        private void drawBorders() {
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
        public void drawGrid() {
            GL.Begin(PrimitiveType.Lines);
            GL.PointSize(1);
            GL.Color3(0.5f, 0.5f, 0.5f);

            float inv = dim / voxels;
            for (float x = 0; x <= dim; x += inv) {
                for (float y = 0; y <= dim; y += inv) {
                    for (float z = 0; z <= dim; z += inv) {
                        if (x + inv <= dim) {
                            GL.Vertex3(x, y, z);
                            GL.Vertex3(x + inv, y, z);
                        }
                        if (y + inv <= dim) {
                            GL.Vertex3(x, y, z);
                            GL.Vertex3(x, y + inv, z);
                        }

                        if (z + inv <= dim) {
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
        public static int getParticleVoxelIndex(Vector3 p) {
            // Scale from [0, 1] to [0, #voxels]
            p *= voxels;
            int x = (int)p.X;
            int y = (int)p.Y;
            int z = (int)p.Z;

            // If the xyz coordinates are outside the total cube, return -2
            if (x < 0 || y < 0 || z < 0 ||
                x >= voxels || y >= voxels || z >= voxels) {
                return -2;
            } else {
                return x + y * voxels + z * voxels * voxels;
            }
        }

        /// <summary>
        /// Overload method but can be called using a particles position
        /// </summary>
        /// <param name="Position"></param>
        /// <returns></returns>
        public static int[] neighborsIndicesConcatenated(Vector3 Position) {
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
        public static int[] neighborsIndicesConcatenated(int x, int y, int z) {
            int[] voxels = getNeighborIndices(x, y, z);

            //Gets how many particles there are in total in all the neighbors
            int total = 0;
            for (int i = 0; i < voxels.Length; i++) {
                total += grid[voxels[i]].Count;
            }

            //Copy spheres into the new list
            int[] res = new int[total];
            int counter = 0;
            for (int i = 0; i < voxels.Length; i++) {
                for (int j = 0; j < grid[voxels[i]].Count; j++) {
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
        public static int[] getNeighborIndices(int x, int y, int z, int radius = 1) {
            int[] res = new int[27];

            int counter = 0;
            for (int xi = -radius; xi <= radius; xi++) {
                for (int yi = -radius; yi <= radius; yi++) {
                    for (int zi = -radius; zi <= radius; zi++) {
                        res[counter] = getVoxelIndex(x + xi, y + yi, z + zi);
                        counter++;
                    }
                }
            }

            int validOnes = 0;
            for (int i = 0; i < res.Length; i++) {
                if (res[i] >= 0 && res[i] < voxels * voxels * voxels) {
                    validOnes++;
                }
            }
            int[] res2 = new int[validOnes];
            int counter1 = 0;
            for (int i = 0; i < res.Length; i++) {
                if (res[i] >= 0 && res[i] < voxels * voxels * voxels) {
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
        public static int[] getNeighborIndices(int i) {
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
        private static int getVoxelIndex(int x, int y, int z) {
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
        public static Vector3 getPosition(int i) {
            int temp = i;
            int z = temp / (voxels * voxels);
            temp -= z * voxels * voxels;
            int y = temp / voxels;
            temp -= y * voxels;
            int x = temp;
            return new Vector3(x * voxelSize, y * voxelSize, z * voxelSize);
        }

        public static Vector3 getVisPosition(int i) {
            int temp = i;
            int z = temp / (visualisationVoxels * visualisationVoxels);
            temp -= z * visualisationVoxels * visualisationVoxels;
            int y = temp / visualisationVoxels;
            temp -= y * visualisationVoxels;
            int x = temp;
            return new Vector3(x * visVoxelSize, y * visVoxelSize, z * visVoxelSize);
        }
        #endregion

        /// <summary>
        /// Draws a voxel of the grid using it's index
        /// </summary>
        /// <param name="i"></param>
        public static void drawVoxel(int i) {
            Vector3 pos = getVisPosition(i);
            //Console.WriteLine(pos);

            // Backside
            GL.Vertex3(pos);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y, pos.Z);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y + visVoxelSize, pos.Z);

            GL.Vertex3(pos);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y + visVoxelSize, pos.Z);
            GL.Vertex3(pos.X, pos.Y + visVoxelSize, pos.Z);

            // Frontside
            GL.Vertex3(pos.X, pos.Y, pos.Z + visVoxelSize);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y, pos.Z + visVoxelSize);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y + visVoxelSize, pos.Z + visVoxelSize);

            GL.Vertex3(pos.X, pos.Y, pos.Z + visVoxelSize);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y + visVoxelSize, pos.Z + visVoxelSize);
            GL.Vertex3(pos.X, pos.Y + visVoxelSize, pos.Z + visVoxelSize);

            // Top
            GL.Vertex3(pos.X, pos.Y + visVoxelSize, pos.Z);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y + visVoxelSize, pos.Z);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y + visVoxelSize, pos.Z + visVoxelSize);

            GL.Vertex3(pos.X, pos.Y + visVoxelSize, pos.Z);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y + visVoxelSize, pos.Z + visVoxelSize);
            GL.Vertex3(pos.X, pos.Y + visVoxelSize, pos.Z + visVoxelSize);

            // Bottom
            GL.Vertex3(pos.X, pos.Y, pos.Z);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y, pos.Z);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y, pos.Z + visVoxelSize);

            GL.Vertex3(pos.X, pos.Y, pos.Z);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y, pos.Z + visVoxelSize);
            GL.Vertex3(pos.X, pos.Y, pos.Z + visVoxelSize);

            // Left
            GL.Vertex3(pos);
            GL.Vertex3(pos.X, pos.Y, pos.Z + visVoxelSize);
            GL.Vertex3(pos.X, pos.Y + visVoxelSize, pos.Z + visVoxelSize);

            GL.Vertex3(pos);
            GL.Vertex3(pos.X, pos.Y + visVoxelSize, pos.Z + visVoxelSize);
            GL.Vertex3(pos.X, pos.Y + visVoxelSize, pos.Z);

            // Right
            GL.Vertex3(pos.X + visVoxelSize, pos.Y, pos.Z);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y, pos.Z + visVoxelSize);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y + visVoxelSize, pos.Z + visVoxelSize);

            GL.Vertex3(pos.X + visVoxelSize, pos.Y, pos.Z);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y + visVoxelSize, pos.Z + visVoxelSize);
            GL.Vertex3(pos.X + visVoxelSize, pos.Y + visVoxelSize, pos.Z);
        }

        /// <summary>
        /// Prints instructions at the start
        /// </summary>
        private static void printInstructions() {
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
        public static void RestartScene(bool sameSeed) {
            grid = new Dictionary<int, List<int>>();
            InitSize();
            for (int i = 0; i < voxels * voxels * voxels; i++) {
                grid.Add(i, new List<int>());
            }

            int val = 40;
            float step = dim / val;
            int count = 0;
            for (int i = 0; i <= val; i++) {
                for (int j = 0; j < val; j++) {
                    for (int k = 1; k < val; k++) {
                        if (count < numberOfPoints) {
            
                            ChangePosition(count, step * i/2, (step * j / 2), step * k);
            
                            R[count] = 0.5f;
            
                            //if ((k == 0 && j == 0 && i == 0) || (k == 9 && j == 9 && i == 2)) {
                            //    verbose[count] = true;
                            //}
                            currentPoints++;
                        }
                        count++;
                    }
                }
            }

        }

        /// <summary>
        /// Currently a very non physically accurate collision method that keeps particles in the box
        /// </summary>
        public static void ResolveCollisions(int i) {
            #region wallCollision

            // For every wall check if the particle is hitting it and going toward the outside
            if (PosX[i] < 0) {
                ChangeX(i, 0);
                VelX[i] = -1 * VelX[i] / Damping;
            }
            if (PosY[i] < 0) {
                ChangeY(i, 0);
                VelY[i] = -1 * VelY[i] / Damping;
            }
            if (PosZ[i] < 0) {
                ChangeZ(i, 0);
                VelZ[i] = -1 * VelZ[i] / Damping;
            }

            if (PosX[i] >= dim) {
                ChangeX(i, dim - 0.001f);
                VelX[i] = -1 * VelX[i] / Damping;
            }
            if (PosY[i] >= dim) {
                ChangeY(i, dim - 0.001f);
                VelY[i] = -1 * VelY[i] / Damping;
            }
            if (PosZ[i] >= dim) {
                ChangeZ(i, dim - 0.001f);
                VelZ[i] = -1 * VelZ[i] / Damping;
            }

            int[] neighbors = Game.neighborsIndicesConcatenated(new Vector3(PosX[i], PosY[i], PosZ[i]));
            for (int j = 0; j < neighbors.Length; j++) {
                float dist = getSquaredDistance(GetVectorPosition(i), GetVectorPosition(j));
                if (dist < (Radius + Radius) * (Radius + Radius)) {
                    //solve interpenetration
                    Vector3 Pos1 = GetVectorPosition(i);
                    Vector3 Pos2 = GetVectorPosition(neighbors[j]);
                    if (i != neighbors[j] && !(Pos1.X == Pos2.X && Pos1.Y == Pos2.Y && Pos1.Z == Pos2.Z)) {
                        float depth = (float)(Math.Sqrt(dist) - Math.Sqrt((Radius + Radius) * (Radius + Radius)));
                        Vector3 collisionNormal = Pos1 - Pos2;
                        collisionNormal.Normalize();
                        float x = collisionNormal.X * depth * 0.5f;
                        float y = collisionNormal.Y * depth * 0.5f;
                        float z = collisionNormal.Z * depth * 0.5f;
                        //solve interpenetration
                        ChangePosition(i, PosX[i] - x, PosY[i] - y, PosZ[i] - z);
                        ChangePosition(j, PosX[j] + x, PosY[j] + y, PosZ[j] + z);
                        //new velocity
                        float a = (depth * -(1 + 1f) * Vector3.Dot(GetVectorVelocity(i), collisionNormal));
                        AddVelocity(i, -a * collisionNormal.X, -a * collisionNormal.Y, -a * collisionNormal.Z);
                        AddVelocity(j, a * collisionNormal.X, a * collisionNormal.Y, a * collisionNormal.Z);
                    }
                }
            }
            #endregion
        }


        /// <summary>
        /// Makes a shape to represent this particle
        /// </summary>
        /// <returns> Returns a list of vertices for a CUBE shape </returns>
        public Vector3[] getShape(int i) {
            Vector3[] v = new Vector3[6];
            v[0] = GetVectorPosition(i) + new Vector3(0, 0, -Radius);
            v[1] = GetVectorPosition(i) + new Vector3(0, 0, Radius);
            v[2] = GetVectorPosition(i) + new Vector3(0, -Radius, 0);
            v[3] = GetVectorPosition(i) + new Vector3(0, Radius, 0);
            v[4] = GetVectorPosition(i) + new Vector3(-Radius, 0, 0);
            v[5] = GetVectorPosition(i) + new Vector3(Radius, 0, 0);
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