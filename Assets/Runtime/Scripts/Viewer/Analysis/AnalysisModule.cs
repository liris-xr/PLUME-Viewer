using System;
using System.Collections.Generic;
using UnityEngine;

namespace PLUME.Viewer.Analysis
{
    public abstract class AnalysisModule : MonoBehaviour
    {
        public virtual bool HasResults()
        {
            return false;
        }

        public virtual Type GetResultType()
        {
            return null;
        }
    }

    public abstract class AnalysisModuleWithResults<T> : AnalysisModule, IDisposable where T : AnalysisModuleResult
    {
        private readonly List<T> _results = new();

        public virtual void Dispose()
        {
        }

        public void SaveResults()
        {
            throw new NotImplementedException();
        }

        public void LoadResults()
        {
            throw new NotImplementedException();
        }

        public virtual void AddResult(T result)
        {
            if (result != null && !_results.Contains(result))
                _results.Add(result);
        }

        public virtual void RemoveResult(T result)
        {
            _results.Remove(result);
        }

        public virtual int GetResultIndex(T result)
        {
            return _results.IndexOf(result);
        }

        public IEnumerator<T> GetResultsEnumerator()
        {
            return _results.GetEnumerator();
        }

        public IEnumerable<T> GetResults()
        {
            return _results;
        }

        public int GetResultsCount()
        {
            return _results.Count;
        }

        public override bool HasResults()
        {
            return true;
        }

        public override Type GetResultType()
        {
            return typeof(T);
        }
    }
}