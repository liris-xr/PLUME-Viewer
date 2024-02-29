using UnityEngine;
using UnityEngine.UIElements;

namespace PLUME.Viewer.Analysis
{
    public class AnalysisModulesListUI : MonoBehaviour
    {
        public MainWindowUI mainWindowUI;

        public void Add(VisualElement moduleRootElement)
        {
            mainWindowUI.AnalysisContainer.Q<ScrollView>().Add(moduleRootElement);
        }
    }
}