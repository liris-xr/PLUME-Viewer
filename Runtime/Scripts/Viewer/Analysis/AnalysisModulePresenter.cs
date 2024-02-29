using UnityEngine;

namespace PLUME.UI.Analysis
{
    public abstract class AnalysisModuleWithResultsPresenter<TU, TV, TW> : MonoBehaviour
        where TU : AnalysisModuleWithResults<TV>
        where TV : AnalysisModuleResult
        where TW : AnalysisModuleWithResultsUI<TU, TV>
    {
        public TU module;
        public TW ui;
    }

    public abstract class AnalysisModulePresenter<TU, TV> : MonoBehaviour
        where TU : AnalysisModule where TV : AnalysisModuleUI<TU>
    {
        public TU module;
        public TV ui;
    }
}