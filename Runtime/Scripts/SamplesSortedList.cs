using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PLUME
{
    public interface IReadOnlySamplesSortedList<T> : IReadOnlyCollection<T> where T : ISample
    {
        public int IndexOf(T item);

        public int FirstIndexAfterTimestamp(ulong time);

        public IEnumerable<T> GetInTimeRange(ulong startTime, ulong endTime);
    }

    public class ReadOnlySamplesSortedList<T> : IReadOnlySamplesSortedList<T> where T : ISample
    {
        private readonly SamplesSortedList<T> _samples;

        public int Count => _samples.Count;

        internal ReadOnlySamplesSortedList(SamplesSortedList<T> samples)
        {
            _samples = samples;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _samples.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _samples.IndexOf(item);
        }

        public int FirstIndexAfterTimestamp(ulong time)
        {
            return _samples.FirstIndexAfterTimestamp(time);
        }

        public IEnumerable<T> GetInTimeRange(ulong startTime, ulong endTime)
        {
            return _samples.GetInTimeRange(startTime, endTime);
        }
    }

    public class SamplesSortedList<T> : ICollection<T>, IReadOnlySamplesSortedList<T>
        where T : ISample
    {
        private readonly List<T> _samples = new();

        public IEnumerator<T> GetEnumerator()
        {
            return _samples.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (item == null)
                return;
            
            if(_samples.Count == 0)
            {
                _samples.Add(item);
                return;
            }
            
            if (!item.HasTimestamp)
            {
                _samples.Add(item);
                return;
            }
            
            if(item.Timestamp >= _samples[^1].Timestamp)
            {
                _samples.Add(item);
                return;
            }
            
            // Find where to insert the item
            var idx = FirstIndexAfterTimestamp(item.Timestamp);
            if (idx < 0)
            {
                _samples.Add(item);
            }
            else
            {
                _samples.Insert(idx, item);
            }
        }

        public void Clear()
        {
            _samples.Clear();
        }

        public bool Contains(T item)
        {
            return _samples.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _samples.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _samples.Remove(item);
        }

        public int IndexOf(T item)
        {
            return _samples.IndexOf(item);
        }

        /// <summary>
        /// Return the first sample index in the set where its time is superior or equal to <paramref name="time"/>.
        ///
        /// For instance if we have the following set of samples S with their associated times T:
        /// S : s0  s1  s2  s3  s4
        /// T : 1   1   1   3   4
        /// If we call IndexByTimestamp(S, 2) the returned index will be 3 (s3).
        /// If we call IndexByTimestamp(S, 0) the returned index will be 0 (s0)
        /// If we call IndexByTimestamp(S, 5) the returned index will be -1 (not found)
        ///
        /// Uses binary search internally.
        /// </summary>
        /// <param name="time">The time</param>
        /// <returns></returns>
        public int FirstIndexAfterTimestamp(ulong time)
        {
            if (_samples.Count == 0)
                return -1;

            var left = 0;
            var right = _samples.Count - 1;

            do
            {
                var mid = left + (right - left) / 2;
                var s = this[mid];

                if (s.Timestamp == time)
                {
                    while (mid > 0 && this[mid - 1].Timestamp == time)
                    {
                        mid--;
                    }

                    return mid;
                }

                if ((mid == 0 || this[mid - 1].Timestamp < time) && this[mid].Timestamp > time)
                {
                    return mid;
                }

                if (s.Timestamp < time)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            } while (left <= right);

            return -1;
        }

        public IEnumerable<T> GetInTimeRange(ulong startTime, ulong endTime)
        {
            var startIdx = FirstIndexAfterTimestamp(startTime);

            if (startIdx < 0)
                yield break;

            var idx = startIdx;

            do
            {
                var sample = this[idx];

                if (sample == null)
                    yield break;

                if (sample.Timestamp > endTime)
                    yield break;

                yield return sample;
                idx++;
            } while (idx < _samples.Count);
        }

        public T this[int index] => _samples[index];

        public ReadOnlySamplesSortedList<T> AsReadOnly()
        {
            return new ReadOnlySamplesSortedList<T>(this);
        }

        public int Count => _samples.Count;
        public bool IsReadOnly => false;
    }
}