using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PLUME.UI.Analysis
{
    public abstract class AnalysisModuleUI<T> : MonoBehaviour where T : AnalysisModule
    {
        public T module;

        public AnalysisModulesListUI modulesListUI;

        /// <summary>
        /// Visual element that will wrap a module inside inside a foldout element with a title.
        /// </summary>
        public VisualTreeAsset containerTemplate;

        public VisualTreeAsset optionsTemplate;

        protected VisualElement Container;
        protected VisualElement Options;

        protected void Awake()
        {
            Container = containerTemplate.Instantiate();
            Options = optionsTemplate.Instantiate();
            var titleLabel = Container.Q<Toggle>().Q<Label>();
            var optionsContainer = Container.Q<VisualElement>("options-container");
            optionsContainer.Add(Options);
            titleLabel.text = GetTitle();

            var resultsFoldout = Container.Q<Foldout>("results-foldout");
            resultsFoldout.style.display = DisplayStyle.None;
            var resultsEmptyLabel = Container.Q<Label>("no-results-label");
            resultsEmptyLabel.style.display = DisplayStyle.None;
        }

        protected void Start()
        {
            modulesListUI.Add(GetRootElement());
        }

        public VisualElement GetRootElement()
        {
            return Container;
        }

        public abstract string GetTitle();
    }

    public abstract class AnalysisModuleWithResultsUI<TU, TV> : AnalysisModuleUI<TU>
        where TU : AnalysisModuleWithResults<TV> where TV : AnalysisModuleResult
    {
        protected VisualElement Results;
        protected Label ResultsEmptyLabel;
        protected Foldout ResultsFoldout;

        protected new void Awake()
        {
            base.Awake();
            Results = Container.Q<VisualElement>("results-container");
            ResultsFoldout = Container.Q<Foldout>("results-foldout");
            ResultsFoldout.style.display = DisplayStyle.Flex;
            ResultsEmptyLabel = Container.Q<Label>("no-results-label");
            ResultsEmptyLabel.style.display = DisplayStyle.Flex;
        }

        protected new void Start()
        {
            base.Start();
            RefreshResults();
        }

        public abstract void RefreshResults();
    }
}