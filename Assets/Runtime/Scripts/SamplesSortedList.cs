using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PLUME
{
    public interface IReadOnlySamplesSortedList<T> : IReadOnlyCollection<T> where T : ISample
    {
        public int IndexOf(T item);

        public int FirstIndexAfterTimestamp(ulong time);

        public int FirstIndexBeforeTimestamp(ulong time);

        public IReadOnlySamplesSortedList<T> GetInTimeRange(ulong startTime, ulong endTime);

        public IReadOnlySamplesSortedList<T> Where(Predicate<T> predicate);

        public T this[int index] { get; }
    }

    public class SamplesSortedListSlice<T> : IReadOnlySamplesSortedList<T> where T : ISample
    {
        private readonly SamplesSortedList<T> _samples;
        public readonly int start;
        public readonly int count;
        public int Count => count;

        internal SamplesSortedListSlice(SamplesSortedList<T> samples, int start, int count)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (start > samples.Count)
                throw new ArgumentOutOfRangeException(nameof(start));
            if (start + count > samples.Count)
                throw new ArgumentOutOfRangeException(nameof(count));

            _samples = samples;
            this.start = start;
            this.count = count;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = start; i < start + count; i++)
            {
                yield return _samples[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            var idx = _samples.IndexOf(item);
            if (idx < start || idx >= start + count)
                return -1;
            return idx - start;
        }

        public int FirstIndexAfterTimestamp(ulong time)
        {
            if (_samples.Count == 0)
                return -1;

            var idx = _samples.FirstIndexAfterTimestamp(time);
            if (idx < start)
                return 0;
            if (idx >= start + count)
                return -1;
            return idx - start;
        }

        public int FirstIndexBeforeTimestamp(ulong time)
        {
            if (_samples.Count == 0)
                return -1;

            var idx = _samples.FirstIndexBeforeTimestamp(time);
            if (idx <= start)
                return -1;
            if (idx >= start + count)
                return count - 1;
            return idx - start;
        }

        public IReadOnlySamplesSortedList<T> Where(Predicate<T> predicate)
        {
            var newSamples = new SamplesSortedList<T>();
            for (var i = start; i < start + count; i++)
            {
                if (predicate(_samples[i]))
                {
                    newSamples.Add(_samples[i]);
                }
            }

            return new ReadOnlySamplesSortedList<T>(newSamples);
        }

        public IReadOnlySamplesSortedList<T> GetInTimeRange(ulong startTime, ulong endTime)
        {
            return _samples.GetInTimeRange(startTime, endTime);
        }

        public T this[int index] => _samples[start + index];
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

        public int FirstIndexBeforeTimestamp(ulong time)
        {
            return _samples.FirstIndexBeforeTimestamp(time);
        }

        public IReadOnlySamplesSortedList<T> GetInTimeRange(ulong startTime, ulong endTime)
        {
            return _samples.GetInTimeRange(startTime, endTime);
        }

        public IReadOnlySamplesSortedList<T> Where(Predicate<T> predicate)
        {
            return _samples.Where(predicate);
        }

        public T this[int index] => _samples[index];
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

            if (_samples.Count == 0)
            {
                _samples.Add(item);
                return;
            }

            if (!item.HasTimestamp)
            {
                _samples.Add(item);
                return;
            }

            if (item.Timestamp >= _samples[^1].Timestamp)
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

        public int FirstIndexBeforeTimestamp(ulong time)
        {
            if (_samples.Count == 0)
                return -1;
            if (time > _samples[^1].Timestamp)
                return _samples.Count - 1;

            var idx = FirstIndexAfterTimestamp(time) - 1;
            if (idx < 0)
                return -1;

            while(idx > 0 && this[idx].Timestamp >= time)
            {
                idx--;
            }
            
            return idx;
        }

        public IReadOnlySamplesSortedList<T> GetInTimeRange(ulong startTime, ulong endTime)
        {
            var startIdx = FirstIndexAfterTimestamp(startTime);

            if (startIdx < 0)
                return new SamplesSortedListSlice<T>(this, 0, 0);

            var idx = startIdx;
            var count = 0;

            while (idx < _samples.Count && this[idx].Timestamp <= endTime)
            {
                count++;
                idx++;
            }

            return new SamplesSortedListSlice<T>(this, startIdx, count);
        }

        public IReadOnlySamplesSortedList<T> Where(Predicate<T> predicate)
        {
            var newSamples = new SamplesSortedList<T>();
            foreach (var sample in _samples.Where(t => predicate(t)))
            {
                newSamples.Add(sample);
            }

            return new ReadOnlySamplesSortedList<T>(newSamples);
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