using UnityEngine;
using UnityEngine.UIElements;

namespace PLUME.UI.Analysis
{
    public class AnalysisModulesListUI : MonoBehaviour
    {
        public MainWindowUI mainWindowUI;

        private ScrollView _scrollView;

        private void Awake()
        {
            _scrollView = mainWindowUI.Q("analysis-container").Q<ScrollView>();
        }

        public void Add(VisualElement moduleRootElement)
        {
            _scrollView.Add(moduleRootElement);
        }
    }
}