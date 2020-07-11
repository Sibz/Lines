using Sibz.Lines.Systems;
using Unity.Entities;

namespace Sibz.Lines
{
    public static class LineDataWorld
    {
        private static World world;
        public static World World => world ?? (world = new World("LineDataWorld"));

    }
}