using Unity.Collections;
using Unity.Jobs;

namespace Sibz.Lines.ECS.Jobs
{
    public struct DeallocateJob<T> : IJob where T : struct
    {
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<T> NativeArray1;

        public void Execute()
        {
        }
    }

    public struct DeallocateJob<T, T2> : IJob
        where T : struct
        where T2 : struct
    {
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<T> NativeArray1;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<T2> NativeArray2;

        public void Execute()
        {
        }
    }

    public struct DeallocateJob<T, T2, T3> : IJob
        where T : struct
        where T2 : struct
        where T3 : struct
    {
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<T> NativeArray1;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<T2> NativeArray2;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<T3> NativeArray3;

        public void Execute()
        {
        }
    }

    public struct DeallocateJob<T, T2, T3, T4> : IJob
        where T : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<T> NativeArray1;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<T2> NativeArray2;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<T3> NativeArray3;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<T4> NativeArray4;

        public void Execute()
        {
        }
    }
}