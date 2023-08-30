using System.IO;

namespace PLUME
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