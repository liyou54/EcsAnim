using System;
using System.Diagnostics;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    public static class NativeListExtensionsNew
    {
        /// <summary>
        /// Appends an element to the end of this list and returns index on which element was added.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks>
        /// Increments the length by 1 unless doing so would exceed the current capacity.
        /// </remarks>
        /// <returns>Index at which element was added</returns>
        /// <exception cref="Exception">Thrown if adding an element would exceed the capacity.</exception>
        public unsafe static int AddNoResizeEx<T>(this NativeList<T>.ParallelWriter writer, T value)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(writer.m_Safety);
#endif
            var idx = Interlocked.Increment(ref writer.ListData->m_length) - 1;
            CheckSufficientCapacity(writer.ListData->Capacity, idx + 1);

            UnsafeUtility.WriteArrayElement(writer.ListData->Ptr, idx, value);
            return idx;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckSufficientCapacity(int capacity, int length)
        {
            if (capacity < length)
                throw new Exception($"Length {length} exceeds capacity Capacity {capacity}");
        }
    }
}