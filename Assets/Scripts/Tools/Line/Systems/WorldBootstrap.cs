﻿using Sibz.Lines.Tools.Systems;
using Unity.Entities;

namespace Sibz.Lines.Systems
{
    public class WorldBootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            var world = LineDataWorld.World;
            var group = world.CreateSystem<LineSystemGroup>();
            group.AddSystemToUpdateList(world.CreateSystem<LineCreateSystem>());
            group.AddSystemToUpdateList(world.CreateSystem<LineUpdateSystem>());
            group.AddSystemToUpdateList(world.CreateSystem<NewLineSystem>());
            group.AddSystemToUpdateList(world.CreateSystem<DebugDrawlLinesSystem>());
            return true;
        }
    }
}