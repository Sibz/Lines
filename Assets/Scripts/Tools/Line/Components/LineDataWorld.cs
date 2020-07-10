using Sibz.Lines.Systems;
using Unity.Entities;

namespace Sibz.Lines
{
    public static class LineDataWorld
    {
        private static World world;

        public static World World
        {
            get
            {
                if (world !=null &&  world.IsCreated) return world;
                world = new World("LineDataWorld");
                var group = world.CreateSystem<LineSystemGroup>();
                group.AddSystemToUpdateList(world.CreateSystem<LineCreateSystem>());
                group.AddSystemToUpdateList(world.CreateSystem<LineUpdateSystem>());
                return world;
            }
        }

    }
}