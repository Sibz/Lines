using System.Collections;
using System.Collections.Generic;
using Sibz.Lines.Systems;
using UnityEngine;

namespace Sibz.Lines
{
    public class LineSystemRunner : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            LineDataWorld.World.GetOrCreateSystem<LineSystemGroup>().Update();
        }
    }
}
