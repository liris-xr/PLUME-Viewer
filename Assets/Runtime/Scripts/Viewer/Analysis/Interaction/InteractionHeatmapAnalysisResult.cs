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
        public string[] InteractorsIds;
        public string[] InteractablesIds;
        public InteractionType InteractionType;
        public ulong StartTime;
        public ulong EndTime;
    }

    public class InteractionHeatmapAnalysisResult : AnalysisModuleResult
    {
        // Mapping between interactors record identifier and number of interactions
        public readonly Dictionary<string, int> Interactions = new();

        public readonly int MaxInteractionCount;

        public readonly int TotalInteractionCount;

        public InteractionHeatmapAnalysisResult()
        {
        }

        public InteractionHeatmapAnalysisResult(InteractionAnalysisModuleParameters generationParameters,
            Dictionary<string, int> interactions, int totalInteractionCount, int maxInteractionCount)
        {
            GenerationParameters = generationParameters;
            Interactions = interactions;
            TotalInteractionCount = totalInteractionCount;
            MaxInteractionCount = maxInteractionCount;
        }

        public InteractionAnalysisModuleParameters GenerationParameters { get; }

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