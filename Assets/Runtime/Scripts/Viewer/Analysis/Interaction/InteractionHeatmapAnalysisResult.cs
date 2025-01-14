using System;
using System.Collections.Generic;
using System.IO;

namespace PLUME.Viewer.Analysis.Interaction
{
    public enum InteractionType
    {
        Hover,
        Select,
        Activate
    }

    public struct InteractionAnalysisModuleParameters
    {
        public Guid[] InteractorsIds;
        public Guid[] InteractablesIds;
        public InteractionType InteractionType;
        public ulong StartTime;
        public ulong EndTime;
    }

    public class InteractionHeatmapAnalysisResult : AnalysisModuleResult
    {
        public InteractionAnalysisModuleParameters GenerationParameters { get; }

        // Mapping between interactors record identifier and number of interactions
        public readonly Dictionary<Guid, int> Interactions = new();

        public readonly int TotalInteractionCount;

        public readonly int MaxInteractionCount;

        public InteractionHeatmapAnalysisResult()
        {
        }

        public InteractionHeatmapAnalysisResult(InteractionAnalysisModuleParameters generationParameters,
            Dictionary<Guid, int> interactions, int totalInteractionCount, int maxInteractionCount)
        {
            GenerationParameters = generationParameters;
            Interactions = interactions;
            TotalInteractionCount = totalInteractionCount;
            MaxInteractionCount = maxInteractionCount;
        }

        public override void Save(Stream outputStream)
        {
            throw new NotImplementedException();
        }

        public override void Load(Stream inputStream)
        {
            throw new NotImplementedException();
        }
    }
}