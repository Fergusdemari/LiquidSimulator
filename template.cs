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
            vaoHandle,
            positionVboHandle,
            normalVboHandle,
            boundsVboHandle,
            boundsNormalVboHandle,
            boundsIndicesVboHandle,
            eboHandle;

        Vector3[] positionVboData = new Vector3[]{
            new Vector3(-0.5f, -0.5f,  0.5f),  //0
            new Vector3( 0.5f, -0.5f,  0.5f),  //1
            new Vector3( 0.5f,  0.5f,  0.5f),  //2
            new Vector3(-0.5f,  0.5f,  0.5f),  //3
            new Vector3(-0.5f, -0.5f, -0.5f),  //4
            new Vector3( 0.5f, -0.5f, -0.5f),  //5
            new Vector3( 0.5f,  0.5f, -0.5f),  //6
            new Vector3(-0.5f,  0.5f, -0.5f),  //7
            new Vector3(-0.5f,  0.8f,  0.5f),  //8
            new Vector3( 0.5f,  0.8f,  0.5f),  //9
            new Vector3( 0.5f,  1.3f,  0.5f),  //10
            new Vector3(-0.5f,  1.3f,  0.5f),  //11
            new Vector3(-0.5f,  0.8f, -0.5f),  //12
            new Vector3( 0.5f,  0.8f, -0.5f),  //13
            new Vector3( 0.5f,  1.3f, -0.5f),  //14
            new Vector3(-0.5f,  1.3f, -0.5f),  //15

            new Vector3(-2.0f, 0, 0),       //16
            new Vector3(-2.0f, 0, 1.0f),     //17
            new Vector3(-2.0f, 1.0f, 0),     //18
            new Vector3(-1.0f, 0, 0),     //19
            new Vector3(-2.0f, 1.0f, 1.0f),   //20
            new Vector3(-1.0f, 0, 1.0f),   //21
            new Vector3(-1.0f, 1.0f, 0),   //22
            new Vector3(-1.0f, 1.0f, 1.0f)}; //23



        int[] indicesVboData = new int[]{
             // front face
                0, 1, 2, 2, 3, 0,
                // top face
                3, 2, 6, 6, 7, 3,
                // back face
                7, 6, 5, 5, 4, 7,
                // left face
                4, 0, 3, 3, 7, 4,
                // bottom face
                0, 1, 5, 5, 4, 0,
                // right face
                1, 5, 6, 6, 2, 1,

                8, 9, 10, 10, 11, 8,
                11, 10, 14, 14, 15, 11,
                15, 14, 13, 13, 12, 15,
                12, 8, 11, 11, 15, 12,
                8, 9, 13, 13, 12, 8,
                9, 13, 14, 14, 10, 9,


                16, 17,
                16, 18,
                16, 19,
                21, 17,
                21, 19,
                21, 23,
                19, 22,
                17, 20,
                18, 22,
                22, 23,
                18, 20,
                20, 23,
                };

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

            float aspectRatio = ClientSize.Width / (float)(ClientSize.Height);
            Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, aspectRatio, 1, 100, out projectionMatrix);
            modelviewMatrix = Matrix4.LookAt(new Vector3(0.5f, 0.5f, 2.3f), new Vector3(0.5f, 0.5f, 0), new Vector3(0, 1, 0));

            GL.UniformMatrix4(projectionMatrixLocation, false, ref projectionMatrix);
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelviewMatrix);
        }

        void CreateVBOs()
        {
            GL.GenBuffers(1, out positionVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(game.vertexBuffer.Length * Vector3.SizeInBytes),
                game.vertexBuffer, BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out normalVboHandle);
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(game.vertexBuffer.Length * Vector3.SizeInBytes),
                game.vertexBuffer, BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out eboHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                new IntPtr(sizeof(uint) * game.indexBuffer.Length),
                game.indexBuffer, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        void CreateVAOs()
        {
            GL.GenVertexArrays(1, out vaoHandle);
            GL.BindVertexArray(vaoHandle);

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);
            GL.BindAttribLocation(shaderProgramHandle, 0, "in_position");

            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);
            GL.BindAttribLocation(shaderProgramHandle, 1, "in_normal");

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

            GL.BindVertexArray(0);
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
        protected override void OnLoad(EventArgs e)
        {
            VSync = VSyncMode.On;

            CreateShaders();

            // Other state
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(System.Drawing.Color.MidnightBlue);

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
            CreateVBOs();

            GL.GenVertexArrays(1, out vaoHandle);
            GL.BindVertexArray(vaoHandle);

            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);
            GL.BindAttribLocation(shaderProgramHandle, 0, "in_position");

            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);
            GL.BindAttribLocation(shaderProgramHandle, 1, "in_normal");

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

            GL.BindVertexArray(0);

            GL.Viewport(0, 0, Width, Height);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.BindVertexArray(vaoHandle);
            GL.DrawElements(BeginMode.Lines, game.boundsIndices.Length,
                DrawElementsType.UnsignedInt, 0);

            if(game.displayMode == Game.Mode.PARTICLES){
                GL.DrawElements(BeginMode.Points, game.indices.Length, DrawElementsType.UnsignedInt, game.boundsIndices.Length);
            }else if (game.displayMode == Game.Mode.SHAPES){
                GL.DrawElements(BeginMode.Triangles, game.indices.Length, DrawElementsType.UnsignedInt, game.boundsIndices.Length+60);
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
                Util.RotateCamera(
                         new Matrix4((float)Math.Cos(-rotationSpeed), 0, (float)-Math.Sin(-rotationSpeed), 0,
                                      0, 1, 0, 0,
                                      (float)Math.Sin(-rotationSpeed), 0, (float)Math.Cos(-rotationSpeed), 0,
                                      0, 0, 0, 1));
            }
            if (keyboard[OpenTK.Input.Key.Right])
            {
                Util.RotateCamera(
                         new Matrix4((float)Math.Cos(rotationSpeed), 0, (float)-Math.Sin(rotationSpeed), 0,
                                      0, 1, 0, 0,
                                      (float)Math.Sin(rotationSpeed), 0, (float)Math.Cos(rotationSpeed), 0,
                                      0, 0, 0, 1));
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
                position += new Vector3(0, 0, translationSpeed);
            }

            if (keyboard[OpenTK.Input.Key.S])
            {
                position += new Vector3(0, 0, -translationSpeed);
            }

            if (keyboard[OpenTK.Input.Key.A])
            {
                position += new Vector3(translationSpeed, 0, 0);
            }

            if (keyboard[OpenTK.Input.Key.D])
            {
                position += new Vector3(-translationSpeed, 0, 0);
            }

            if (keyboard[OpenTK.Input.Key.Q])
            {
                position += new Vector3(0, -translationSpeed, 0);
            }
            
            if (keyboard[OpenTK.Input.Key.E])
            {
                position += new Vector3(0, translationSpeed, 0);
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