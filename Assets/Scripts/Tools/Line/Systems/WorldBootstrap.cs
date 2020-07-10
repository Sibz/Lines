using Unity.Entities;

namespace Sibz.Lines.Systems
{
    public class WorldBootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            return true;
        }
    }
}