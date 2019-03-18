using System;
using System.IO;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using template;

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
            ClientSize = new Size(1280, 720);
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
            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Texture2D);
            GL.Clear(ClearBufferMask.DepthBufferBit);
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
            // tell OpenTK we're done rendering
            SwapBuffers();
        }
        public static void Main(string[] args)
        {
            // entry point
            using (OpenTKApp app = new OpenTKApp()) { app.Run(60.0, 120.0); }
        }

        private void HandleInput(KeyboardState keyboard)
        {
            float rotationSpeed = 0.03f;
            float translationSpeed = 0.03f;
            if (keyboard[OpenTK.Input.Key.Escape]) this.Exit();
            if (keyboard[OpenTK.Input.Key.Up])
            {
                Util.RotateCamera(
                         new Matrix4(1, 0, 0, 0,
                                     0, (float)Math.Cos(-rotationSpeed), (float)Math.Sin(-rotationSpeed), 0,
                                     0, (float)-Math.Sin(-rotationSpeed), (float)Math.Cos(-rotationSpeed), 0,
                                     0, 0, 0, 1));
            }
            if (keyboard[OpenTK.Input.Key.Down])
            {
                Util.RotateCamera(
                         new Matrix4(1, 0, 0, 0,
                                     0, (float)Math.Cos(rotationSpeed), (float)Math.Sin(rotationSpeed), 0,
                                     0, (float)-Math.Sin(rotationSpeed), (float)Math.Cos(rotationSpeed), 0,
                                     0, 0, 0, 1));
            }
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

            if (keyboard[OpenTK.Input.Key.W])
            {
                position += -ViewDirection * translationSpeed;
            }

            if (keyboard[OpenTK.Input.Key.S])
            {
                position += ViewDirection * translationSpeed;
            }

            if (keyboard[OpenTK.Input.Key.A])
            {
                position += Vector3.Cross(ViewDirection, UpDirection) * translationSpeed;
            }

            if (keyboard[OpenTK.Input.Key.D])
            {
                position += Vector3.Cross(ViewDirection, UpDirection) * -translationSpeed;
            }

            //if (keyboard[OpenTK.Input.Key.Q])
            //{
            //    position = new Vector3(position.X, position.Y - translationSpeed, position.Z);
            //}
            //
            //if (keyboard[OpenTK.Input.Key.E])
            //{
            //    position = new Vector3(position.X, position.Y + translationSpeed, position.Z);
            //}

        }

        /// <summary>
        /// Sets / reset camera
        /// </summary>
        public void ResetCamera()
        {
            Camera = Matrix4.CreatePerspectiveFieldOfView(1f, 1f, .1f, 1000);
            position = new Vector3(-0.5f, 0, -2);
            UpDirectionOriginal = new Vector3(0, 1, 0);
            ViewDirectionOriginal = new Vector3(0, 0, 1);

            ViewDirection = new Vector3(0, 0, 1);
            UpDirection = new Vector3(0, 1, 0);

            // For some reason movementdirections are inverted until you call RotateCamera
            Util.RotateCamera(new Matrix4(1, 0, 0, 0,
                                          0, 1, 0, 0,
                                          0, 0, 1, 0,
                                          0, 0, 0, 1));
        }
    }
}