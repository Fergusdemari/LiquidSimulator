using OpenTK;
using System;

namespace Template {

    public class FluidSim {
        int particleCount;
        //k is a coefficient basically for how dense the fluid is in general. Increasing k will make the particles act as if they represent a larger amount of fluid (box will appear more full)
        float k = 0.05f;
        //how much the liquid stays together
        float viscosity = 0.1f;
        //a preference pressure value
        float p0 = 0.1f;
        //radius which is the cutoff for the kernels. Particle is only affected by other particles within this radius
        static float d = 0.1f;
        float sigma = 40000.0f;
        float timeStep;
        float gradientFieldThreshold = 1.0f;
        Vector3[] spikyLookup = new Vector3[101];
        public static float[] poly6Lookup = new float[101];
        float[] laplacianLookup = new float[101];

        public FluidSim(int particleCount_, float timeStep_) {
            //set up sim constants
            particleCount = particleCount_;
            timeStep = timeStep_;
            //d = _d*20.0f;
            d = 0.2f;
            Console.WriteLine(d);

            Vector3 v1 = new Vector3(d, 0, 0);
            Vector3 v2 = v1;
            int i = 0;
            while (v2.X > 0) {
                if (i == 100) {
                    v2.X = 0;
                }
                spikyLookup[i] = spikyPressureKernel(v2, v1);
                poly6Lookup[i] = Poly6WeightKernel(v2, v1);
                laplacianLookup[i] = laplacianKernel(v2, v1);
                v2.X -= d / 100;
                i++;
            }
        }

        public void Update(int startIndex = -1, int stopIndex = -1) {
            // So you can call the Update function without parameters
            if (startIndex == -1 && stopIndex == -1) {
                startIndex = 0;
                stopIndex = Game.currentPoints;
            }

            // Updates density and pressure
            PropertiesUpdate(startIndex, stopIndex);

            //calculate total force for every particle
            ForcesUpdate(startIndex, stopIndex);

            //updates the movement
            MovementUpdate(startIndex, stopIndex);

            for (int i = startIndex; i < stopIndex; i++) {
                Game.ResolveCollisions(i);
            }
        }

        public void PropertiesUpdate(int startIndex = -1, int stopIndex = -1) {
            // So you can call the Update function without parameters
            if (startIndex == -1 && stopIndex == -1) {
                startIndex = 0;
                stopIndex = Game.currentPoints;
            }

            //loop through each particle and find it's density and pressure
            for (int i = startIndex; i < stopIndex; i++) {
                Game.NFX[i] = 0;
                Game.NFY[i] = 0;
                Game.NFZ[i] = 0;

                calcDensity(i);
                calcPressure(i);
            }
            for (int i = startIndex; i < stopIndex; i++) {
                calcColorGradient(i);
            }
        }

        public void ForcesUpdate(int startIndex = -1, int stopIndex = -1) {
            // So you can call the Update function without parameters
            if (startIndex == -1 && stopIndex == -1) {
                startIndex = 0;
                stopIndex = Game.currentPoints;
            }

            for (int i = startIndex; i < stopIndex; i++) {
                //calculate total force for every particle
                isFucked(Game.GetVectorPosition(i));
                if (Game.verbose[i]) {
                    Console.WriteLine("Particle: " + Game.GetVectorPosition(i));
                }
                calcPresssureForce(i);
                calcViscosityForce(i);
                calcSurfaceTension(i);
                calcBodyForce(i);
                //Get the acceleration resulted from the force and integrate for position

            }
        }

        public void MovementUpdate(int startIndex = -1, int stopIndex = -1) {
            // So you can call the Update function without parameters
            if (startIndex == -1 && stopIndex == -1) {
                startIndex = 0;
                stopIndex = Game.currentPoints;
            }

            for (int i = startIndex; i < stopIndex; i++) {

                Vector3 acceleration = Game.GetVectorNetForce(i) / Game.Density[i];
                //calling update for a Sphere object now only checks for boundary collision
                Game.AddVelocity(i, acceleration * timeStep);
                Vector3 temp = Game.GetVectorVelocity(i);
                //if (temp.X > 1 || temp.Y > 1 || temp.Z > 1) {
                //    Console.WriteLine();
                //}
                Game.AddPosition(i, Game.GetVectorVelocity(i) * timeStep);
                Game.ResolveCollisions(i);
            }

        }

        /**
         * The following functions are implementations of the langrangian fluid equations provided here:
         * https://www.cs.ubc.ca/~rbridson/fluidsimulation/fluids_notes.pdf
         * 
         * This source was linked by Amir on the game physics course page
         **/
        public static void calcDensity(int i) {
            Game.Density[i] = calcDensity(Game.GetVectorPosition(i));
        }

        public static float calcDensity(Vector3 position) {
            float dense = 0.0f;

            int[] closePointInds = Game.neighborsIndicesConcatenated(position);
            for (int i = 0; i < closePointInds.Length; i++) {
                int index = (int)((Game.getDistance(position, Game.GetVectorPosition(closePointInds[i])) * 100) / d);
                if (index > 100) {
                    index = 100;
                }
                if (index < 0) {
                    index = 0;
                }
                dense += Game.Mass * poly6Lookup[index];
            }
            if (dense == 0) {
                dense += 0.0001f;
            }
            isFucked(dense);
            return dense;
        }

        public void calcPressure(int i) {
            Game.Pressure[i] = k * Game.Density[i] - k * p0;
        }

        public void calcColorGradient(int i) {
            Vector3 n = new Vector3(0, 0, 0);
            int[] closePointInds = Game.neighborsIndicesConcatenated(Game.GetVectorPosition(i));
            for (int j = 0; j < closePointInds.Length; j++) {
                int index = (int)((Game.getDistance(Game.GetVectorPosition(i), Game.GetVectorPosition(closePointInds[j])) * 100) / d);
                if (index > 100) {
                    index = 100;
                }
                isFucked(n);
                n += ((Game.Mass / Game.Density[closePointInds[j]])) * Poly6GradientKernel(Game.GetVectorPosition(i), Game.GetVectorPosition(closePointInds[j]));
                isFucked(n);
            }
            if (n.Length > 0) {
                n.Normalize();
            }
            Game.NormX[i] = n.X;
            Game.NormY[i] = n.Y;
            Game.NormZ[i] = n.Z;
        }

        public void calcPresssureForce(int j) {
            Vector3 f = new Vector3(0, 0, 0);
            int[] closePointInds = Game.neighborsIndicesConcatenated(Game.GetVectorPosition(j));
            for (int i = 0; i < closePointInds.Length; i++) {
                
                float fScalar = -1.0f * Game.Mass * ((Game.Pressure[j] + Game.Pressure[closePointInds[i]]) / (2 * Game.Density[closePointInds[i]]));
                f += fScalar * spikyPressureKernel(Game.GetVectorPosition(j), Game.GetVectorPosition(closePointInds[i]));
            }
            if (Game.verbose[j]) {
                Console.WriteLine("pressure: " + f);
                Console.WriteLine(closePointInds.Length);
            }
            isFucked(f);
            Game.AddNetForce(j, f);
        }

        public void calcViscosityForce(int j) {
            Vector3 f = new Vector3(0, 0, 0);
            int[] closePointInds = Game.neighborsIndicesConcatenated(Game.GetVectorPosition(j));
            for (int i = 0; i < closePointInds.Length; i++) {
                int index = (int)((Game.getDistance(Game.GetVectorPosition(j), Game.GetVectorPosition(closePointInds[i])) * 100) / d);
                if (index > 100) {
                    index = 100;
                }
                isFucked(f);
                Vector3 temp1 = (Game.GetVectorVelocity(closePointInds[i]));
                Vector3 temp2 = Game.GetVectorVelocity(j);
                f += viscosity * Game.Mass * ((temp1-temp2) / Game.Density[closePointInds[i]]) * laplacianLookup[index];
                isFucked(f);
            }
            // Console.WriteLine("viscosity force: " + f);
            if (Game.verbose[j]) {
                Console.WriteLine("Viscocity: " + f);
            }
            isFucked(f);
            Game.AddNetForce(j, f);
        }

        //Surface area model from here: https://cg.informatik.uni-freiburg.de/publications/2013_SIGGRAPHASIA_surfaceTensionAdhesion.pdf
        public void calcSurfaceTension(int j) {
            if (Game.GetVectorNormal(j).Length < gradientFieldThreshold) {
                return;
            }

            Vector3 f = new Vector3(0, 0, 0);
            Vector3 cNormal = new Vector3(0, 0, 0);
            Vector3 cCurvature = new Vector3(0, 0, 0);
            float K = Game.Density[j];
            int[] closePointInds = Game.neighborsIndicesConcatenated(Game.GetVectorPosition(j));
            for (int i = 0; i < closePointInds.Length; i++) {
                Vector3 r = Game.GetVectorPosition(j) - Game.GetVectorPosition(closePointInds[i]);
                if (Game.getDistance(Game.GetVectorPosition(j), Game.GetVectorPosition(closePointInds[i])) > 0) {
                    cNormal += Game.Mass * CohesionKernel(Game.GetVectorPosition(j), Game.GetVectorPosition(closePointInds[i])) * (r / r.Length);
                    cCurvature += Game.GetVectorNormal(j) - Game.GetVectorNormal(closePointInds[i]);
                    K += Game.Density[closePointInds[i]];
                }

            }
            cNormal *= Game.Mass * -sigma;
            cCurvature *= Game.Mass * -sigma;
            if (K > 0.0001) {
                K = 2 * p0 / K;
            } else {
                K = 0;
            }
            f = K * (cNormal + cCurvature);
            if (Game.verbose[j]) {
                Console.WriteLine("Surface: " + f);
            }
            Game.AddNetForce(j, f);
            isFucked(f);
        }

        public void calcBodyForce(int i) {
            Vector3 f = new Vector3(0, Game.gravity * Game.Mass, 0);
            if (Game.verbose[i]) {
                Console.WriteLine("gravity: " + f);
            }
            // Maybe add force for walls here
            Game.AddNetForce(i, f);
            isFucked(f);
        }

        /**
         * Kernels: the following functions are the kernels used to calculate the distance weighting of particles
         *          as well as the effect that graident of the vector field has on the particles for each force.
         * 
         * Sources used to find these kernels: 
         * https://www8.cs.umu.se/kurser/TDBD24/VT06/lectures/sphsurvivalkit.pdf
         * https://nccastaff.bournemouth.ac.uk/jmacey/MastersProjects/MSc15/06Burak/BurakErtekinMScThesis.pdf
         **/


        //Poly6 kernel for distance weighting. used in calculating a particle's density
        public float Poly6WeightKernel(Vector3 x1, Vector3 x2) {
            float r = Game.getDistance(x1, x2);
            if (r > d) {
                return 0.0f;
            }

            return (315 / (64 * (float)Math.PI * (float)Math.Pow(d, 9))) * (float)Math.Pow(d * d - r * r, 3);
        }

        public float CohesionKernel(Vector3 x1, Vector3 x2) {
            float r = Game.getDistance(x1, x2);

            float constant = (float)(32 / (Math.PI * Math.Pow(d, 9)));
            if (2 * r > d && r <= d) {
                //Console.WriteLine(1);
                return constant * (float)Math.Pow((d - r), 3) * (float)Math.Pow(r, 3);
            } else if (r > 0.0001 && 2 * r <= d) {
                //Console.WriteLine(2);
                return constant * 2 * (float)Math.Pow((d - r), 3) * (float)Math.Pow(r, 3) - (float)Math.Pow(d, 6) / 64;
            }
            //Console.WriteLine(3);
            return 0.0f;
        }

        public Vector3 Poly6GradientKernel(Vector3 x1, Vector3 x2) {
            float r = Game.getDistance(x1, x2);
            if (r < 1.0f) {
                r = 0.1f;
            }

            if (r > d) {
                return new Vector3(0, 0, 0);
            }

            Vector3 temp = (-945 / (32 * (float)Math.PI * (float)Math.Pow(d, 9))) * ((x1 - x2) * (float)Math.Pow(d * d - r * r, 2));
            return temp;
        }

        public float Poly6LaplacianKernel(float r) {
            if (r > d) {
                return 0;
            }

            return (-945 / (32 * (float)Math.PI * ((float)Math.Pow(d, 9))) * ((d * d - r * r) * (3 * d * d - 7 * r * r)));
        }

        //Spiky kernel for distance weighitng and vector gradient. used for calculating pressure force
        public Vector3 spikyPressureKernel(Vector3 x1, Vector3 x2) {
            float r = Game.getDistance(x1, x2);
            if (r > d) {
                return new Vector3(0, 0, 0);
            }

            return -1.0f * (45 / ((float)Math.PI * (float)Math.Pow(d, 6))) * (float)Math.Pow(d - r, 2) * (x1 - x2);
        }

        //Laplacian kernel for distance weighitng and vector gradient. used forcalculating viscosit force
        public float laplacianKernel(Vector3 x1, Vector3 x2) {
            float r = Game.getDistance(x1, x2);
            if (r > d) {
                return 0.0f;
            }

            return (45 / ((float)Math.PI * (float)Math.Pow(d, 6))) * (d - r);
        }
        
        public static bool isFucked(int x) {
            return x < -2100000000;
        }

        public static bool isFucked(float x) {
            bool ret = double.IsNaN(x) || double.IsInfinity(x);
            float[] density = Game.Density;
            if (ret) {
                Console.WriteLine("Something is fucked");
            }
            return ret;
        }

        public static bool isFucked(Vector3 v) {
            return isFucked(v.X) || isFucked(v.Y) || isFucked(v.Z);
        }
    }

}