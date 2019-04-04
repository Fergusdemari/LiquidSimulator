using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using template.Shapes;

namespace Template
{
    struct Emitter
    {
        public Vector3 position;
        public float radius;
        public int emissionRate;
        public int delay;
        public int tickCounter;
        public Vector3 velocity;
    }

    class Game
    {
        //OPENGL program variables
        private int program;


        public enum Mode
        {
            PARTICLES,  //Displays them as points
            CUBES,      //Displays the voxels they're in
            SHAPES      //Displays whatever shape we decided to give particles (tilted cube atm)
        }

        public Mode displayMode = Mode.SHAPES;
        
        public static bool Recording = false;
        public static bool random = false;
        public bool running = false;
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
        // Keep threading false atm, issues with locking
        private bool threading = true;

        // Number of voxels in the grid per dimension
        static int voxels = 32;
        static int visualisationVoxels = 32;
        bool[] visualisationVoxelSwitches = new bool[visualisationVoxels * visualisationVoxels * visualisationVoxels];
        // Size of one voxele
        static float visVoxelSize = (float)dim/visualisationVoxels;
        static float voxelSize = (float)dim / voxels;

        public static int numberOfPoints = 3000;
        public static int currentPoints = 0;

        public Emitter[] emitters;

        public static Sphere[] particles = new Sphere[numberOfPoints];
        public static Dictionary<int, List<int>> grid = new Dictionary<int, List<int>>();

        //OPENGL
        public Vector3[] vertices;
        public int[] indices;

        public Vector3[] boundsVertices;
        public int[] boundsIndices;

        public Vector3[] vertexBuffer;
        public int[] indexBuffer;

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
            grid = new Dictionary<int, List<int>>();
            for (int i = 0; i < voxels * voxels * voxels; i++) {
                    grid.Add(i, new List<int>());
            }
            emitters = new Emitter[0];
            //emitters[0] = new Emitter();
            //emitters[0].position = new Vector3(0.0f, 0.95f, 0.5f);
            //emitters[0].radius = 0.1f;
            //emitters[0].velocity = new Vector3(0.4f, -0.2f, 0.01f);
            //emitters[0].emissionRate = 5;
            //emitters[0].delay = 5;
            //emitters[0].tickCounter = 0;

            
            RestartScene(true);

            //start fluid simulator
            simulator = new FluidSim(numberOfPoints, dt, particles, voxelSize);
                        //int[] t = neighborsIndicesConcatenated();
        }

        /// <summary>
        /// One update, ran every like 1/120 seconds
        /// </summary>
        /// <param name="e"></param>
        public void Tick(FrameEventArgs e)
        {
            Random r = new Random();
            if(currentPoints < numberOfPoints && (running || step)){
                for(int i = 0; i < emitters.Length; i++){
                    if(emitters[i].tickCounter == 0){
                        for(int j = 0; j < emitters[i].emissionRate; j++){
                            float rad = (float)Math.Sqrt(r.NextDouble()) * emitters[i].radius;
                            float angle = (float)r.NextDouble() * (float)Math.PI * 2;
                            Vector3 pos = new Vector3(emitters[i].position.X, emitters[i].position.Y + rad * (float)Math.Cos(angle), emitters[i].position.Z + rad * (float)Math.Sin(angle));
                          
                            particles[currentPoints] = (new Sphere(currentPoints, pos, emitters[i].velocity, 0.01f));
                            particles[currentPoints].color = new Vector3(1.0f, 0, 0);
                            particles[currentPoints].Mass = 1.0f;
                            particles[currentPoints].NetForce = new Vector3(0, 0, 0);
                            currentPoints++;
                        }
                    }
                    emitters[i].tickCounter++;
                    if(emitters[i].tickCounter > emitters[i].delay){
                        emitters[i].tickCounter = 0;
                    }
                }
            }
            // If set to threading, split the taskforce up, but if the amount of points is too small then there's no point
            if (threading && !(particles.Length < 100))
            {
                if(running || step){
                    int PPT = 8;
                    var options = new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = 8
                    };
                     Parallel.For(0, currentPoints / PPT, options, i =>
                      {
                          simulator.PropertiesUpdate(i * PPT, (i + 1) * PPT);
                      });
                    Parallel.For(0, currentPoints / PPT, options, i =>
                    {
                        simulator.ForcesUpdate(i * PPT, (i + 1) * PPT);
                    });
                    simulator.MovementUpdate();
                    for (int i = 0; i < currentPoints; i++)
                    {
                        particles[i].Update(dt);
                    }
                    if(displayMode == Mode.CUBES){
                    Parallel.For(0, (visualisationVoxels * visualisationVoxels * visualisationVoxels) / PPT, options, i =>
                    {
                        calcVoxelDensities(i * PPT, (i + 1) * PPT);
                    });
                    }
                    step = false;
                }
            }
            else
            {
                //particles[i].Update(dt);
                if(running || step){
                    simulator.Update();
                    step = false;
                }

            } 
        }

        /// <summary>
        /// Calculates a density value for each voxel to decide whether it should be drawn
        /// </summary>
        public void calcVoxelDensities(int start, int end){
            for (int i = start; i < end; i++){
                Vector3 position = getVisPosition(i);
                position += new Vector3(visVoxelSize/2.0f, visVoxelSize/2.0f, visVoxelSize/2.0f);
                float density = FluidSim.calcDensity(position);
                if(density > 20.0){
                    //Console.WriteLine(position + " " + density);
                    visualisationVoxelSwitches[i] = true;
                }else{
                    visualisationVoxelSwitches[i] = false;
                }
            }
        }

        /// <summary>
        /// Does the logic of what to draw
        /// </summary>
        public void RenderGL()
        {
            if (showGrid)
            {
                drawGrid();
            }
            drawBorders();

            //Different displaymodes
            switch (displayMode)
            {
                case Mode.PARTICLES:
                    // Drawing of all spheres
                    vertices = new Vector3[currentPoints];
                    indices = new int[currentPoints];
                    for (int i = 0; i < currentPoints; i++)
                    {
                        vertices[i] = particles[i].Position;
                        indices[i] = i+boundsVertices.Length;
                    }
                    break;
                case Mode.CUBES:
                    GL.Begin(PrimitiveType.Triangles);
                    GL.Color4(0.1f, 0.1, 1f, 1f);
                    /// Drawing of voxels when not empty
                    for (int i = 0; i < visualisationVoxels * visualisationVoxels * visualisationVoxels; i++)
                    {
                        if(visualisationVoxelSwitches[i]){
                            drawVoxel(i);
                        }
                    }
                    GL.End();
                    break;
                case Mode.SHAPES:
                    // Drawing of all spheres
                    vertices = new Vector3[numberOfPoints * 6];
                    indices = new int[numberOfPoints*8*3];
                    for (int i = 0; i < currentPoints; i++)
                    {
                        Vector3[] shape = particles[i].getShape();
                        shape.CopyTo(vertices, i*6);

                        int[] shapeIndex = new int[]{
                            i*6+boundsVertices.Length, i*6+5+boundsVertices.Length, i*6+2+boundsVertices.Length,
                            i*6+boundsVertices.Length, i*6+4+boundsVertices.Length, i*6+3+boundsVertices.Length,
                            i*6+boundsVertices.Length, i*6+4+boundsVertices.Length, i*6+2+boundsVertices.Length,
                            i*6+boundsVertices.Length, i*6+5+boundsVertices.Length, i*6+3+boundsVertices.Length,
                            i*6+1+boundsVertices.Length, i*6+5+boundsVertices.Length, i*6+2+boundsVertices.Length,
                            i*6+1+boundsVertices.Length, i*6+4+boundsVertices.Length, i*6+3+boundsVertices.Length,
                            i*6+1+boundsVertices.Length, i*6+4+boundsVertices.Length, i*6+2+boundsVertices.Length,
                            i*6+1+boundsVertices.Length, i*6+5+boundsVertices.Length, i*6+3+boundsVertices.Length,
                        };

                        shapeIndex.CopyTo(indices, i*8*3);
                    }
                    break;

            }
            vertexBuffer = new Vector3[vertices.Length + boundsVertices.Length];
            boundsVertices.CopyTo(vertexBuffer, 0);
            vertices.CopyTo(vertexBuffer, boundsVertices.Length);

            indexBuffer = new int[indices.Length + boundsIndices.Length];
            boundsIndices.CopyTo(indexBuffer, 0);
            indices.CopyTo(indexBuffer, boundsIndices.Length);
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
            boundsVertices = new Vector3[]{
                new Vector3(0, 0, 0),       //0
                new Vector3(0, 0, dim),     //1
                new Vector3(0, dim, 0),     //2
                new Vector3(dim, 0, 0),     //3
                new Vector3(0, dim, dim),   //4
                new Vector3(dim, 0, dim),   //5
                new Vector3(dim, dim, 0),   //6
                new Vector3(dim, dim, dim), //7

            };

            boundsIndices = new int[]{
                0, 1,
                0, 2,
                0, 3,
                5, 1,
                5, 3,
                5, 7,
                3, 6,
                1, 4,
                2, 6,
                6, 7,
                2, 4,
                4, 7,
            };
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

        public static Vector3 getVisPosition(int i)
        {
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
        public void drawVoxel(int i)
        {
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
        public static void RestartScene(bool sameSeed) {
            if (random) { 
                if (!sameSeed) {
                    Random seedCreator = new Random();
                    RNGSeed = seedCreator.Next(int.MaxValue);
                    Console.WriteLine("RNGSeed has been rerolled to " + RNGSeed);
                }

                Console.Write("Setting up Grid...");
                grid = new Dictionary<int, List<int>>();
                particles = new Sphere[numberOfPoints];
                // Creates the grid list empty
                for (int i = 0; i < voxels * voxels * voxels; i++) {
                    grid.Add(i, new List<int>());
                }
                Console.WriteLine(" Done.");

                Console.Write("Generating particles...");
                //Creates random points with random velocities
                Random r = new Random(123123);
                for (int i = 0; i < particles.Length; i++) {
                    particles[i] =  new Sphere(i, new Vector3(((float)r.NextDouble() / 8 + 0.4375f * dim), ((float)r.NextDouble() / 4 * dim), ((float)r.NextDouble() / 8 + 0.4375f * dim)),
                                           Vector3.Zero, 0.01f);
                    particles[i].color = particles[i].Position / dim;
                    particles[i].Mass = 0.3f;
                    particles[i].NetForce = new Vector3(0, 0, 0);
                }
                Console.WriteLine(" Done.");
            }
            else{
                grid = new Dictionary<int, List<int>>();
                particles = new Sphere[numberOfPoints];
                for (int i = 0; i < voxels * voxels * voxels; i++) {
                    grid.Add(i, new List<int>());
                }
                float step = dim/20;
                int count = 0;
                for(int i = 0; i <= numberOfPoints/2; i++){
                    for(int j = 0; j < 20; j++){
                        for(int k = 0; k < 20; k++) {
                            if(count < numberOfPoints){
                                particles[count] = new Sphere(count, new Vector3(step*i, step/3*j, step*k+step/2), Vector3.Zero, 0.01f);
                                particles[count].color = new Vector3(0.5f, 0, 0);
                                particles[count].Mass = 0.3f;
                                particles[count].NetForce = new Vector3(0, 0, 0);
                                if((k == 0 && j == 0 && i == 0) || (k == 9 && j == 9 && i == 2))
                                {
                                    particles[count].verbose = false;
                                }
                                currentPoints++;
                            }
                            count++;
                        }
                    }
                }
            }
        }

        private int CompileShaders() {
            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, 
            File.ReadAllText(@"Components\Shaders\vertexShader.vert"));
            GL.CompileShader(vertexShader);

            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, 
            File.ReadAllText(@"Components\Shaders\fragmentShader.frag"));
            GL.CompileShader(fragmentShader);

            var program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);

            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            return program;
        }
    }
}