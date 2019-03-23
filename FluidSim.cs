using System;
using OpenTK;
using template.Shapes;

namespace Template
{

    public class FluidSim{
        int particleCount;
        Sphere[] particles;
        //k is a coefficient basically for how dense the fluid is in general. Increasing k will make the particles act as if they represent a larger amount of fluid (box will appear more full)
        float k = 0.018f;
        //how much the liquid stays together
        float viscosity = 1.0f;
        //a preference pressure value
        float p0 = 1.0f;
        //radius which is the cutoff for the kernels. Particle is only affected by other particles within this radius
        float d = 0.5f;

        float timeStep;

        public FluidSim(int particleCount_, float timeStep_, Sphere[] points) {
            //set up sim constants
            particleCount = particleCount_;
            timeStep = timeStep_;

            //set up particles
            particles = points;
            for(int i = 0; i < particleCount; i++) {
                particles[i].Mass = 0.3f;
                particles[i].NetForce = new Vector3(0, 0, 0);
            }
        }

        public void update() {
            //loop through each particle and find it's density and pressure
            for(int i = 0; i < particleCount; i++) {
                particles[i].NetForce = new Vector3(0,0,0);
                calcDensity(particles[i]);
                calcPressure(particles[i]);
            }

            //calculate total force for every particle
            for (int i = 0; i < particleCount; i++) {
                calcPresssureForce(particles[i]);
                calcViscosityForce(particles[i]);
                calcBodyForce(particles[i]);

                //Get the acceleration resulted from the force and integrate for position
                Vector3 acceleration = particles[i].NetForce / particles[i].Density;
                particles[i].Velocity += acceleration * timeStep;
                particles[i].Position += particles[i].Velocity * timeStep;

                //calling update for a Sphere object now only checks for boundary collision
                particles[i].Update(timeStep);
            }
        }

        /**
         * The following functions are implementations of the langrangian fluid equations provided here:
         * https://www.cs.ubc.ca/~rbridson/fluidsimulation/fluids_notes.pdf
         * 
         * This source was linked by Amir on the game physics course page
         **/

        public void calcDensity(Sphere p) {
            float dense = 0;
            for(int i = 0; i < particleCount; i++) { 
                dense += particles[i].Mass * Poly6WeightKernel(p.Position, particles[i].Position);
            }

            p.Density = dense;
        }

        public void calcPressure(Sphere p) {
            p.Pressure = k * p.Density - k * p0;
        }

        public void calcPresssureForce(Sphere p) {
            Vector3 f = new Vector3(0, 0, 0);
            for(int i = 0; i < particleCount; i++) {
                if (p.ListIndex != i) {
                    float fScalar = -1.0f * particles[i].Mass * ((p.Pressure + particles[i].Pressure) / (2 * particles[i].Density));
                    f += fScalar * spikyPressureKernel(p.Position, particles[i].Position);
                }
            }
            p.NetForce += f;
        }

        public void calcViscosityForce(Sphere p) {
            Vector3 f = new Vector3(0, 0, 0);
            for (int i = 0; i < particleCount; i++) {
                if (p.ListIndex != i) {
                    f += viscosity * particles[i].Mass * ((particles[i].Velocity - p.Velocity) / particles[i].Density) * laplacianKernel(p.Position, particles[i].Position);
                }
            }
           // Console.WriteLine("viscosity force: " + f);
            p.NetForce += f;
        }

        public void calcBodyForce(Sphere p) {
            Vector3 f = new Vector3(0, -9.81f * p.Mass, 0);
           // Console.WriteLine("gravity force: " + f);
            p.NetForce += f;
        }

        /**
         * Kernels: the following functions are the kernels used to calculate the distance weighting of particles
         *          as well as the effect that graident of the vector field has on the particles for each force.
         * 
         * Source for these: https://www8.cs.umu.se/kurser/TDBD24/VT06/lectures/sphsurvivalkit.pdf
         **/


        //Poly6 kernel for distance weighting. used in calculating a particle's density
        public float Poly6WeightKernel(Vector3 x1, Vector3 x2) {
            Vector3 x = x1 - x2;
            float r = (x.X) * (x.X) + (x.Y) * (x.Y) + (x.Z) * (x.Z);
            if (r < 0 || r > d) {
                return 0.0f;
            }

            return (315 / (64 * (float)Math.PI * (float)Math.Pow(d, 9))) * (float)Math.Pow(d*d - r*r, 3);
        }

        //Spiky kernel for distance weighitng and vector gradient. used for calculating pressure force
        public Vector3 spikyPressureKernel(Vector3 x1, Vector3 x2) {
            Vector3 x = x1 - x2;
            float r = (x.X) * (x.X) + (x.Y) * (x.Y) + (x.Z) * (x.Z);
            if (r < 0 || r > d) {
                return new Vector3(0, 0, 0);
            }

            return -1.0f * (45 / ((float)Math.PI * (float)Math.Pow(d, 6))) * (float)Math.Pow(d - r, 2) * (x1 - x2);
        }

        //Laplacian kernel for distance weighitng and vector gradient. used forcalculating viscosit force
        public float laplacianKernel(Vector3 x1, Vector3 x2) {
            Vector3 x = x1 - x2;
            float r = (x.X) * (x.X) + (x.Y) * (x.Y) + (x.Z) * (x.Z);
            if (r < 0 || r > d) {
                return 0.0f;
            }

            return (45 / ((float)Math.PI * (float)Math.Pow(d, 6))) * (d - r);
        }
    }

}
