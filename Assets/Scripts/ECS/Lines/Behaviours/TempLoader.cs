using System;
using System.Collections;
using System.Collections.Generic;
using Sibz.Lines.ECS;
using Sibz.Lines.ECS.Jobs;
using UnityEngine;

namespace Sibz.Lines
{
    public class TempLoader : MonoBehaviour
    {
        private void OnEnable()
        {
            LineWorld.World.GetExistingSystem<NewLineHeightMapChangeSystem>().FirstRun();
        }
    }
}
