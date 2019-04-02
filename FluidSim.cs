using OpenTK;
using System;
using template.Shapes;
using System.Collections.Generic;

namespace Template {

    public class FluidSim {
        int particleCount;
        //k is a coefficient basically for how dense the fluid is in general. Increasing k will make the particles act as if they represent a larger amount of fluid (box will appear more full)
        float k = 0.8f;
        //how much the liquid stays together
        float viscosity = 1.0f;
        //a preference pressure value
        float p0 = 1.0f;
        //radius which is the cutoff for the kernels. Particle is only affected by other particles within this radius
        float d = 0.1f;

        float sigma = 0.1f;

        float timeStep;

        Vector3[] spikyLookup = new Vector3[101];
        float[] poly6Lookup = new float[101];
        float[] laplacianLookup = new float[101];

        public FluidSim(int particleCount_, float timeStep_, Sphere[] points, float _d) {
            //set up sim constants
            particleCount = particleCount_;
            timeStep = timeStep_;
            //d = _d*20.0f;
            d=0.2f;
            Console.WriteLine(d);

            Vector3 v1 = new Vector3(d, 0, 0);
            Vector3 v2 = v1;
            int i = 0;
            while(v2.X > 0){
                if( i == 100){
                    v2.X = 0;
                }
                spikyLookup[i] = spikyPressureKernel(v2, v1);
                poly6Lookup[i] = Poly6WeightKernel(v2, v1);
                laplacianLookup[i] = laplacianKernel(v2, v1);
                v2.X -= d/100;
                i++;
            }
        }

        public void Update(int startIndex=-1, int stopIndex=-1) {

            // So you can call the Update function without parameters
            if (startIndex == -1 && stopIndex == -1)
            {
                startIndex = 0;
                stopIndex = Game.currentPoints;
            }

            // Updates density and pressure
            PropertiesUpdate(startIndex, stopIndex);

            //calculate total force for every particle
            ForcesUpdate(startIndex, stopIndex);

            //updates the movement
            MovementUpdate(startIndex, stopIndex);

            for (int i = startIndex; i < stopIndex; i++)
            {
                Game.particles[i].Update(timeStep);
            }
        }

        public void PropertiesUpdate(int startIndex = -1, int stopIndex = -1)
        {
            // So you can call the Update function without parameters
            if (startIndex == -1 && stopIndex == -1)
            {
                startIndex = 0;
                stopIndex = Game.currentPoints;
            }

            //loop through each particle and find it's density and pressure
            for (int i = startIndex; i < stopIndex; i++)
            {
                Game.particles[i].NetForce = new Vector3(0, 0, 0);
                calcDensity(Game.particles[i]);
                calcPressure(Game.particles[i]);
                calcNormal(Game.particles[i]);
            }
        }

        public void ForcesUpdate(int startIndex = -1, int stopIndex = -1)
        {
            // So you can call the Update function without parameters
            if (startIndex == -1 && stopIndex == -1)
            {
                startIndex = 0;
                stopIndex = Game.currentPoints;
            }

            for (int i = startIndex; i < stopIndex; i++)
            {
            //calculate total force for every particle
                if (Game.particles[i].verbose)
                {
                    Console.WriteLine("Particle: " + Game.particles[i].Position);
                }
                calcPresssureForce(Game.particles[i]);
                calcViscosityForce(Game.particles[i]);
                calcSurfaceTension(Game.particles[i]);
                calcBodyForce(Game.particles[i]);
                if (Game.particles[i].verbose)
                {
                    Console.WriteLine();
                }
                //Get the acceleration resulted from the force and integrate for position
               
            }
        }

        public void MovementUpdate(int startIndex=-1, int stopIndex=-1)
        {
            // So you can call the Update function without parameters
            if (startIndex == -1 && stopIndex == -1)
            {
                startIndex = 0;
                stopIndex = Game.currentPoints;
            }

            for (int i = startIndex; i < stopIndex; i++)
            {
                Vector3 acceleration = Game.particles[i].NetForce / Game.particles[i].Density;
                //calling update for a Sphere object now only checks for boundary collision
                Game.particles[i].Velocity += acceleration * timeStep;
                Game.particles[i].Position += Game.particles[i].Velocity * timeStep;

                Game.particles[i].Update(timeStep);
            }

        }

        /**
         * The following functions are implementations of the langrangian fluid equations provided here:
         * https://www.cs.ubc.ca/~rbridson/fluidsimulation/fluids_notes.pdf
         * 
         * This source was linked by Amir on the game physics course page
         **/

        public void calcDensity(Sphere p) {
            float dense = 1f;

            int[] closePointInds = Game.neighborsIndicesConcatenated(p.Position);
            for (int i = 0; i < closePointInds.Length; i++) {
                int index = (int)((Game.getDistance(p.Position, Game.particles[closePointInds[i]].Position)*100)/d);
                if(index > 100){
                    index = 100;
                }
                dense += Game.particles[closePointInds[i]].Mass * poly6Lookup[index];
            }

            p.Density = dense;
        }

        public void calcPressure(Sphere p) {
            p.Pressure = k * p.Density - k * p0;
        }

        public void calcNormal(Sphere p){
            Vector3 n = new Vector3(0,0,0);
            int[] closePointInds = Game.neighborsIndicesConcatenated(p.Position);
            for (int i = 0; i < closePointInds.Length; i++) {
                int index = (int)((Game.getDistance(p.Position, Game.particles[closePointInds[i]].Position)*100)/d);
                if(index > 100){
                    index = 100;
                }
                n += (Game.particles[closePointInds[i]].Mass / Game.particles[closePointInds[i]].Density) * Poly6GradientKernel(p.Position, Game.particles[closePointInds[i]].Position);
            }
            n.Normalize();
            p.normal = n;
        }

        public void calcPresssureForce(Sphere p) {
            Vector3 f = new Vector3(0, 0, 0);
            int[] closePointInds = Game.neighborsIndicesConcatenated(p.Position);
            for (int i = 0; i < closePointInds.Length; i++) {
                float fScalar = -1.0f * Game.particles[closePointInds[i]].Mass * ((p.Pressure + Game.particles[closePointInds[i]].Pressure) / (2 * Game.particles[closePointInds[i]].Density));
                f += fScalar * spikyPressureKernel(p.Position, Game.particles[closePointInds[i]].Position);
            }
            float withSelf = -1.0f * p.Mass * ((p.Pressure + p.Pressure) / (2 * p.Density));
            f -= withSelf * spikyPressureKernel(p.Position, p.Position);
            if(p.verbose)
            {
                Console.WriteLine("pressure: " + f);
            }
            p.NetForce += f;
        }

        public void calcViscosityForce(Sphere p) {
            Vector3 f = new Vector3(0, 0, 0);
            int[] closePointInds = Game.neighborsIndicesConcatenated(p.Position);
            for (int i = 0; i < closePointInds.Length; i++) {
                int index = (int)((Game.getDistance(p.Position, Game.particles[closePointInds[i]].Position)*100)/d);
                if(index > 100){
                    index = 100;
                }

                f += viscosity * Game.particles[closePointInds[i]].Mass * ((Game.particles[closePointInds[i]].Velocity - p.Velocity) / Game.particles[closePointInds[i]].Density) * laplacianLookup[index];
            }
            // Console.WriteLine("viscosity force: " + f);
            if(p.verbose)
            {
                Console.WriteLine("Viscocity: " + f);
            }
            p.NetForce += f;
        }

        public void calcSurfaceTension(Sphere p)
        {
            Vector3 f = new Vector3(0,0,0);
            Vector3 cNormal = new Vector3(0, 0, 0);
            float cLaplacian = 0;
            int[] closePointInds = Game.neighborsIndicesConcatenated(p.Position);
            for (int i = 0; i < closePointInds.Length; i++) {
                int index = (int)((Game.getDistance(p.Position, Game.particles[closePointInds[i]].Position)*100)/d);
                if(index > 100){
                    index = 100;
                }
                Vector3 r = p.Position - Game.particles[closePointInds[i]].Position;
                if(Game.getDistance(p.Position, Game.particles[closePointInds[i]].Position) > 0){
                    cNormal += Poly6GradientKernel(p.Position, Game.particles[closePointInds[i]].Position) / Game.particles[closePointInds[i]].Density;
                    cLaplacian += Poly6LaplacianKernel(Game.getDistance(p.Position, Game.particles[closePointInds[i]].Position)) / Game.particles[closePointInds[i]].Density;
               }

            }

            cNormal *= p.Mass;
            cLaplacian *= p.Mass;

            if(cNormal.Length > 7){
                f = -sigma * cNormal / cNormal.Length * cLaplacian;
                //Console.WriteLine("IN: " + f + "-" + cNormal + " -- " + cNormal.Length + " -- " + cLaplacian);
            }

            if (p.verbose){
                Console.WriteLine("Surface: " + f);
            }

            p.NetForce += f;
        }

        public void calcBodyForce(Sphere p) {
            Vector3 f = new Vector3(0, Game.gravity * p.Mass, 0);
            if(p.verbose)
            {
                Console.WriteLine("gravity: " + f);
            }
            // Maybe add force for walls here
            p.NetForce += f;
            
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

        public float CohesionKernel(Vector3 x1, Vector3 x2){
            float r = Game.getDistance(x1, x2);

            float constant = (float)(32/(Math.PI * Math.Pow(d, 9)));
            if(2*r > d && r <= d){
                //Console.WriteLine(1);
                return constant * (float)Math.Pow((d-r), 3) * (float)Math.Pow(r, 3);
            }else if(r > 0.0001 && 2*r <= d){
                //Console.WriteLine(2);
                return constant * 2 * (float)Math.Pow((d-r), 3) * (float)Math.Pow(r, 3) - (float)Math.Pow(d, 6)/64;
            }
            //Console.WriteLine(3);
            return 0.0f;
        }

        public Vector3 Poly6GradientKernel(Vector3 x1, Vector3 x2){
            float r = Game.getDistance(x1, x2);
            if(r < 1.0f){ 
                r = 0.1f;
            }

            if (r > d) {
                return new Vector3(0, 0, 0);
            }

            return (-945 / (32 * (float)Math.PI * (float)Math.Pow(d, 9))) * ((x1-x2)*(float)Math.Pow(d * d - r * r, 2));
        }

        public float Poly6LaplacianKernel(float r){
            if (r > d) {
                return 0;
            }

            return (-945 / (32 * (float)Math.PI * ((float)Math.Pow(d, 9))) * (float)((d * d - r * r)*(3 * d * d - 7 * r * r)));
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
    }

}
