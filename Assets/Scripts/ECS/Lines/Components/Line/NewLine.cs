﻿using Unity.Entities;

namespace Sibz.Lines.ECS.Components
{
    public struct NewLine : IComponentData
    {
        public NewLineModifiers Modifiers;
    }
}