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
        static int screenID;
        static Game game;
        static bool terminated = false;
        public static Matrix4 Camera;
        public static Vector3 ViewDirectionOriginal;
        public static Vector3 UpDirectionOriginal;

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
            // called upon window resize
            GL.Viewport(0, 0, Width, Height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 4.0);
        }
        protected override void OnUpdateFrame(FrameEventArgs e)
        {

            // called once per frame; app logic
            var keyboard = OpenTK.Input.Keyboard.GetState();
            HandleInput(keyboard);
            game.Tick(e);

        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            // called once per frame; render
            if (terminated)
            {
                Exit();
                return;
            }
            GL.ClearColor(Color.Black);
            GL.Disable(EnableCap.Texture2D);
            GL.DepthFunc(DepthFunction.Never);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            
            GL.Color3(0.5f, 1.0f, 1.0f);

            // convert Game.screen to OpenGL texture
            GL.BindTexture(TextureTarget.Texture2D, screenID);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                           game.screen.width, game.screen.height, 0,
                           OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                           PixelType.UnsignedByte, game.screen.pixels
                         );
            // clear window contents
            GL.Clear(ClearBufferMask.ColorBufferBit |
                      ClearBufferMask.DepthBufferBit);

            // setup camera
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();


            GL.LoadMatrix(ref Camera);
            GL.Translate(position);

            game.RenderGL();
            if (Game.Recording) {
                SaveImage();
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