using AviFile;
using System;
using System.Drawing;
using System.IO;

namespace template {
    class VideoMaker {
        public static AviManager writer;
        public static int imageCount = 0;
        public static string uniqueTimer = "..\\..\\assets\\" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "-" + DateTime.Now.Millisecond;

        public static void Start(string location = @"D:\assets\new.avi", int width = 1024, int height = 1024) {
            //if (writer == null) {
            //    writer = new AviManager(location/* + DateTime.Now.ToLongTimeString() + ".avi"*/, true);
            //}
        }

        public static void Close() {
            writer.Close();
            //Console.WriteLine("WARNING: tried to close filewriter while it was already closed");

        }

        public static void writeImage(Bitmap bm) {
            //writer.AddVideoStream(false, 1, bm);
            //VideoStream stream = writer.GetVideoStream();
            //stream.AddFrame(bm);    
            Image img = bm;
            Directory.CreateDirectory(uniqueTimer);
            img.Save(uniqueTimer + "\\img_" + imageCount + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            imageCount++;
        }

        public static bool IsOpen() {
            return writer != null;
        }
    }
}
