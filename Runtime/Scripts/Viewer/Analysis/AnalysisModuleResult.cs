using System.IO;

namespace PLUME.Viewer.Analysis
{
    public abstract class AnalysisModuleResult
    {
        public AnalysisModuleResult()
        {
        }

        public abstract void Save(Stream outputStream);

        public abstract void Load(Stream inputStream);
    }
}