using UnityEngine;

namespace PLUME
{
    public static class ComputeShaderExtensions
    {
        /**
         * Compute shader's dispatch is limited to a maximum of 65535 thread groups. This method sends as many dispatch as
         * necessary to cope for this limitation.
         */
        public static void SplitDispatch(
            this ComputeShader shader, int kernelId,
            int totalNumberOfGroupsNeededX,
            int totalNumberOfGroupsNeededY)
        {
            const int maxAllowedGroups = 65535;
            var numberOfNeededDispatchesX = Mathf.CeilToInt(totalNumberOfGroupsNeededX / (float)maxAllowedGroups);
            var numberOfNeededDispatchesY = Mathf.CeilToInt(totalNumberOfGroupsNeededY / (float)maxAllowedGroups);

            for (var xDispatchIdx = 0; xDispatchIdx < numberOfNeededDispatchesX; ++xDispatchIdx)
            {
                var nThreadGroupsX = xDispatchIdx == numberOfNeededDispatchesX - 1
                    ? totalNumberOfGroupsNeededX % maxAllowedGroups
                    : maxAllowedGroups;

                for (var yDispatchIdx = 0; yDispatchIdx < numberOfNeededDispatchesY; ++yDispatchIdx)
                {
                    var nThreadGroupsY = yDispatchIdx == numberOfNeededDispatchesY - 1
                        ? totalNumberOfGroupsNeededY % maxAllowedGroups
                        : maxAllowedGroups;

                    shader.SetInt("x_dispatch_index", xDispatchIdx);
                    shader.SetInt("y_dispatch_index", yDispatchIdx);
                    shader.SetInt("dispatch_max_thread_group", maxAllowedGroups);
                    shader.Dispatch(kernelId, nThreadGroupsX, nThreadGroupsY, 1);
                }
            }
        }
    }
}