using AForge.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template
{
    class VideoMaker
    {
        public static VideoFileWriter writer = new VideoFileWriter();

        public static void Start(string location = @"..\..\assets\", int width = 1024, int height = 1024)
        {
            if (writer.IsOpen)
            {
                Console.WriteLine("WARNING: Tried to Start a new filewriter while another is still open");
            }
            else
            {
                writer.Open(location + DateTime.Now.ToLongTimeString() + ".avi", width, height);
            }
        }

        public static void Close()
        {
            if (writer.IsOpen)
            {
                writer.Close();
            }
            else
            {
                Console.WriteLine("WARNING: tried to close filewriter while it was already closed");
            }
        }

        public static void writeImage(Bitmap bm) {
            if (!writer.IsOpen)
            {
                Console.WriteLine("WARNING: Tried to write to a closed image");
                return;
            }
            writer.WriteVideoFrame(bm);
        }

        public static bool isOpen()
        {
            return writer.IsOpen;
        }

    }
}
