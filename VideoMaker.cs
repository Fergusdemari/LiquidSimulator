using System;
using System.Drawing;
using System.IO;

namespace template {
    class VideoMaker {
        public static int imageCount = 0;
        public static string uniqueTimer = "..\\..\\assets\\" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "-" + DateTime.Now.Millisecond;

        public static void writeImage(Bitmap bm) {
            //writer.AddVideoStream(false, 1, bm);
            //VideoStream stream = writer.GetVideoStream();
            //stream.AddFrame(bm);    
            Image img = bm;
            Directory.CreateDirectory(uniqueTimer);
            img.Save(uniqueTimer + "\\img_" + imageCount + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            imageCount++;
        }
    }
}
