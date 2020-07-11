using Unity.Entities;

namespace Sibz.Lines.ECS
{
    public class Bootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            LineWorld.World = new LineWorld();
            return true;
        }
    }
}