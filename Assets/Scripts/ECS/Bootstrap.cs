﻿using System;
using System.Linq;
using System.Reflection;
using Sibz.Lines.ECS.Systems;
using Unity.Entities;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Sibz.Lines.ECS
{
    public class Bootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            // LineWorld.World = new LineWorld();

            var loop = PlayerLoop.GetDefaultPlayerLoop();

            if (!UpdateSystem<Update, LineWorldSimGroup>(ref loop))
                throw new Exception("Unable to set LineWorldSimGroup player loop");

            if (!UpdateSystem<PreLateUpdate, LineWorldPresGroup>(ref loop))
                throw new Exception("Unable to set LineWorldPresGroup player loop");

            if (!UpdateSystem<Initialization, LineWorldInitGroup>(ref loop))
                throw new Exception("Unable to set LineWorldInitGroup player loop");

            PlayerLoop.SetPlayerLoop(loop);

            LineWorld.World.Initialise();

            return true;
        }

        private static bool UpdateSystem<T>(ref PlayerLoopSystem def, Type type) where T : ComponentSystemBase
        {
            if (def.type == type)
            {
                AddSystem<T>(ref def);
                return true;
            }

            if (def.subSystemList != null)
                for (var i = 0; i < def.subSystemList.Length; i++)
                    if (UpdateSystem<T>(ref def.subSystemList[i], type))
                        return true;

            return false;
        }

        private static bool UpdateSystem<T, T2>(ref PlayerLoopSystem def) where T2 : ComponentSystemBase
        {
            return UpdateSystem<T2>(ref def, typeof(T));
        }

        private static void AddSystem<T>(ref PlayerLoopSystem def) where T : ComponentSystemBase
        {
            var list = new PlayerLoopSystem[def.subSystemList.Length + 1];
            def.subSystemList.CopyTo(list, 0);
            list[def.subSystemList.Length].type = typeof(T);
            var type = Assembly.GetAssembly(typeof(ScriptBehaviourUpdateOrder)).GetTypes()
                               .FirstOrDefault(x => x.Name == "DummyDelegateWrapper");

            if (type != null)
            {
                var instance = Activator.CreateInstance(type, LineWorld.World.GetOrCreateSystem<T>());
                list[def.subSystemList.Length].updateDelegate = (PlayerLoopSystem.UpdateFunction) type
                                                                                                 .GetMethod("TriggerUpdate")
                                                                                                ?.CreateDelegate(typeof(PlayerLoopSystem.UpdateFunction),
                                                                                                                 instance);
            }

            def.subSystemList = list;
        }
    }
}