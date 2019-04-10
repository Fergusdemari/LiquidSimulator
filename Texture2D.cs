using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template
{
    public class Texture2D
    {
        private int id;
        private int width, height;

        public int Id { get => id; }
        public int Width { get => width; }
        public int Height { get => height; }

        public Texture2D(int id, int width, int height)
        {
            this.id = id;
            this.width = width;
            this.height = height;
        }
    }
}
