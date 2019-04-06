using System;
using System.IO;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using template;
using System.Drawing.Imaging;

namespace Template
{
    public class OpenTKApp : GameWindow
    {
        public OpenTKApp()
            : base(640, 480,
            new GraphicsMode(), "OpenGL 3 Example", 0,
            DisplayDevice.Default, 3, 0,
            GraphicsContextFlags.ForwardCompatible | GraphicsContextFlags.Debug)
        { }
        static int screenID;
        static Game game;
        static bool terminated = false;
        public static Matrix4 Camera;
        public static Vector3 ViewDirectionOriginal;
        public static Vector3 UpDirectionOriginal;


        int vertexShaderHandle,
            fragmentShaderHandle,
            shaderProgramHandle,
            modelviewMatrixLocation,
            projectionMatrixLocation,
            vaoHandle, vaoHandle2, lighitngHandle, lighting;
        Matrix4 projectionMatrix, modelviewMatrix;


        void CreateShaders()
        {
            vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);
            StreamReader Vs = new StreamReader( "../../vertex.glsl" );
            StreamReader Fs = new StreamReader( "../../fragment.glsl" );
            GL.ShaderSource(vertexShaderHandle, Vs.ReadToEnd());
            GL.ShaderSource(fragmentShaderHandle, Fs.ReadToEnd());

            GL.CompileShader(vertexShaderHandle);
            GL.CompileShader(fragmentShaderHandle);

            Console.WriteLine(GL.GetShaderInfoLog(vertexShaderHandle));
            Console.WriteLine(GL.GetShaderInfoLog(fragmentShaderHandle));

            // Create program
            shaderProgramHandle = GL.CreateProgram();

            GL.AttachShader(shaderProgramHandle, vertexShaderHandle);
            GL.AttachShader(shaderProgramHandle, fragmentShaderHandle);

            GL.LinkProgram(shaderProgramHandle);

            Console.WriteLine(GL.GetProgramInfoLog(shaderProgramHandle));
            Console.WriteLine(GL.GetString(StringName.Version));
            Console.WriteLine(GL.GetString(StringName.ShadingLanguageVersion));


            GL.UseProgram(shaderProgramHandle);

            // Set uniforms
            projectionMatrixLocation = GL.GetUniformLocation(shaderProgramHandle, "projection_matrix");
            modelviewMatrixLocation = GL.GetUniformLocation(shaderProgramHandle, "modelview_matrix");
            lighitngHandle = GL.GetUniformLocation(shaderProgramHandle, "lighting");
            float aspectRatio = ClientSize.Width / (float)(ClientSize.Height);
            Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, aspectRatio, 0.01f, 100, out projectionMatrix);
            modelviewMatrix = Matrix4.LookAt(new Vector3(0.5f, 0.5f, 2.3f), new Vector3(0.5f, 0.5f, 0), new Vector3(0, 1, 0));
            position = new Vector3(0.5f, 0.5f, 2.3f);
        }

        void CreateVBOs()
        {


            //GL.GenBuffers(1, out eboHandle);
            //GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
            //GL.BufferData(BufferTarget.ElementArrayBuffer,
            //    new IntPtr(sizeof(uint) * game.indexBuffer.Length),
            //    game.indexBuffer, BufferUsageHint.StaticDraw);
                
            //GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        void CreateVAOs()
        {

        }

        private static Vector3 viewDirection;
        public static Vector3 ViewDirection {
            get { return viewDirection; }
            set { viewDirection = value.Normalized(); }
        }

        private static Vector3 upDirection;
        public static Vector3 UpDirection
        {
            get { return upDirection; }
            set { upDirection = value.Normalized(); }
        }

        public static Vector3 position;
        public static float angle = 0.0f;
        protected override void OnLoad(EventArgs e)
        {
            VSync = VSyncMode.On;

            CreateShaders();

            // Other state
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(System.Drawing.Color.Black);

            ResetCamera();
            // called upon app init
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            ClientSize = new Size(1024, 1024);
            Location = new Point(300, 0); 
            game = new Game(); 
            game.screen = new Surface(Width, Height);
            Sprite.target = game.screen;
            screenID = game.screen.GenTexture();
            game.Init();
        }
        protected override void OnUnload(EventArgs e)
        {
            // called upon app close
            GL.DeleteTextures(1, ref screenID);
            Environment.Exit(0); // bypass wait for key on CTRL-F5
        }
        protected override void OnResize(EventArgs e)
        {

        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);

            // called once per frame; app logic
            var keyboard = OpenTK.Input.Keyboard.GetState();
            HandleInput(keyboard);
            game.Tick(e);

        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            game.RenderGL();
            if (Game.Recording) {
                SaveImage();
            }

            int positionVboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(game.vertexBuffer.Length * Vector3.SizeInBytes),
                game.vertexBuffer, BufferUsageHint.StaticDraw);

            int normVboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, normVboHandle);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(game.normalsBuffer.Length * Vector3.SizeInBytes),
                game.normalsBuffer, BufferUsageHint.StaticDraw);

            int attribute_vpos = GL.GetAttribLocation( shaderProgramHandle, "in_position" );
            int attribute_norm = GL.GetAttribLocation( shaderProgramHandle, "in_normal" );

            GL.GenVertexArrays(2, out vaoHandle2);
            GL.BindVertexArray(vaoHandle2);
            GL.EnableVertexAttribArray(attribute_norm);
            GL.BindBuffer(BufferTarget.ArrayBuffer, normVboHandle);
            GL.VertexAttribPointer(attribute_norm, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

            GL.EnableVertexAttribArray(attribute_vpos);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.VertexAttribPointer(attribute_vpos, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

            GL.Viewport(0, 0, Width, Height);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.BindVertexArray(vaoHandle2);

            GL.UniformMatrix4(projectionMatrixLocation, false, ref projectionMatrix);
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);

            GL.DrawArrays(PrimitiveType.Lines, 0, game.boundsVertices.Length);
            if(game.displayMode == Game.Mode.PARTICLES){
                GL.DrawElements(BeginMode.Points, game.indices.Length, DrawElementsType.UnsignedInt, game.boundsIndices.Length);
            }else if (game.displayMode == Game.Mode.SHAPES){
                GL.DrawArrays(PrimitiveType.Triangles, game.boundsVertices.Length, game.vertices.Length);
            }
            SwapBuffers();

        }

        private void SaveImage()
        {
            int width = 1024;
            int height = 1024;

            var snapShotBmp = new Bitmap(width, height);
            BitmapData bmpData = snapShotBmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly,
                                                      System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte,
                          bmpData.Scan0);
            snapShotBmp.UnlockBits(bmpData);
            snapShotBmp.RotateFlip(RotateFlipType.RotateNoneFlipXY);
            VideoMaker.writeImage(snapShotBmp);
        }

        public static void Main(string[] args)
        {
            // entry point -> Framerate stuff set?
            using (OpenTKApp app = new OpenTKApp()) { app.Run(60.0, 60.0); }
        }

        /// <summary>
        /// Handles the input from the keyboard
        /// </summary>
        /// <param name="keyboard"></param>
        private void HandleInput(KeyboardState keyboard)
        {
            float rotationSpeed = 0.03f;
            float translationSpeed = 0.03f;
            if (keyboard[OpenTK.Input.Key.Escape]) this.Exit();

            if (keyboard[OpenTK.Input.Key.Left])
            {
                Matrix4 rot = Matrix4.CreateRotationY(-rotationSpeed);
                angle -= rotationSpeed;
                projectionMatrix = rot* projectionMatrix;
            }
            if (keyboard[OpenTK.Input.Key.Right])
            {
                Matrix4 rot = Matrix4.CreateRotationY(+rotationSpeed);
                angle += rotationSpeed;
                projectionMatrix = rot* projectionMatrix;
            }
            if (keyboard[OpenTK.Input.Key.R])
            {
                ResetCamera();
            }
            if (keyboard[OpenTK.Input.Key.Number1])
            {
                Game.RestartScene(true);
            }
            if (keyboard[OpenTK.Input.Key.Number2])
            {
                Game.RestartScene(false);
            }

            if (keyboard[OpenTK.Input.Key.W])
            {
                Matrix4 trans = Matrix4.CreateTranslation(-translationSpeed*(float)Math.Sin(angle), 0, translationSpeed*(float)Math.Cos(angle));
                modelviewMatrix = trans * modelviewMatrix;
            }

            if (keyboard[OpenTK.Input.Key.S])
            {
                Matrix4 trans = Matrix4.CreateTranslation(translationSpeed*(float)Math.Sin(angle), 0, -translationSpeed*(float)Math.Cos(angle));
                modelviewMatrix = trans * modelviewMatrix;

            }

            if (keyboard[OpenTK.Input.Key.A])
            {
                Matrix4 trans = Matrix4.CreateTranslation(translationSpeed*(float)Math.Cos(angle), 0, translationSpeed*(float)Math.Sin(angle));
                modelviewMatrix = trans * modelviewMatrix;

            }
            if (keyboard[OpenTK.Input.Key.D])
            {
                Matrix4 trans = Matrix4.CreateTranslation(-translationSpeed*(float)Math.Cos(angle), 0, -translationSpeed*(float)Math.Sin(angle));
                modelviewMatrix = trans * modelviewMatrix;
            }

            if (keyboard[OpenTK.Input.Key.Q])
            {
                Matrix4 trans = Matrix4.CreateTranslation(0, translationSpeed, 0);
                modelviewMatrix = trans * modelviewMatrix;
            }
            
            if (keyboard[OpenTK.Input.Key.E])
            {
                Matrix4 trans = Matrix4.CreateTranslation(0, -translationSpeed, 0);
                modelviewMatrix = trans * modelviewMatrix;
                position.Y += translationSpeed;
            }

            if (keyboard[OpenTK.Input.Key.L])
            {
                game.step = true;
            }

            if (keyboard[OpenTK.Input.Key.Space])
            {
                if (game.running) { 
                    game.running = false;
                }else { 
                    game.running = true;
                }

            }

            //if (keyboard[OpenTK.Input.Key.I])
            //{
            //    VideoMaker.Start();
            //}
            //
            //if (keyboard[OpenTK.Input.Key.O])
            //{
            //    VideoMaker.Close();
            //}


        }

        /// <summary>
        /// Sets / reset camera
        /// </summary>
        public void ResetCamera()
        {
            Camera = Matrix4.CreatePerspectiveFieldOfView(1f, 1f, .1f, 1000);
            position = new Vector3(-0.5f*Game.dim, -0.5f*Game.dim, -2 * Game.dim);
            ViewDirectionOriginal = new Vector3(0, 0, 1);

            ViewDirection = new Vector3(0, 0, 1);

            // For some reason movementdirections are inverted until you call RotateCamera
            Util.RotateCamera(new Matrix4(1, 0, 0, 0,
                                          0, 1, 0, 0,
                                          0, 0, 1, 0,
                                          0, 0, 0, 1));
        }
    }
}