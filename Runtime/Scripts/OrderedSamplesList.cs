using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using PLUME.Sample;
using UnityEngine;

namespace PLUME
{
    /// <summary>
    /// List of samples ensuring that added/inserted samples have increasing times.
    /// </summary>
    public class OrderedSamplesList : IList<UnpackedSample>
    {
        private readonly List<UnpackedSample> _samples = new();

        public int Count => _samples.Count;
        public bool IsReadOnly { get; }

        /// <summary>
        /// Return the first sample in the set where its time is superior or equal to <paramref name="time"/>.
        ///
        /// For instance if we have the following set of samples S with their associated times T:
        /// S : s0  s1  s2  s3  s4
        /// T : 1   1   1   3   4
        /// If we call FindFirstClosestSample(S, 2) the returned index will be 3 (s3).
        /// If we call FindFirstClosestSample(S, 0) the returned index will be 0 (s0)
        /// If we call FindFirstClosestSample(S, 5) the returned index will be -1 (not found)
        ///
        /// Uses binary search internally.
        /// </summary>
        /// <param name="time">The time</param>
        /// <returns></returns>
        public int FirstIndexAfterOrAtTime(ulong time)
        {
            if (_samples.Count == 0)
                return -1;
            
            var left = 0;
            var right = _samples.Count - 1;

            do
            {
                var mid = left + (right - left) / 2;
                var s = this[mid];

                if (s.Header.Time == time)
                {
                    while (mid > 0 && this[mid - 1].Header.Time == time)
                    {
                        mid--;
                    }

                    return mid;
                }

                if ((mid == 0 || this[mid - 1].Header.Time < time) && this[mid].Header.Time > time)
                {
                    return mid;
                }

                if (s.Header.Time < time)
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

        public IEnumerator<UnpackedSample> GetEnumerator()
        {
            return _samples.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(UnpackedSample item)
        {
            var lastItem = _samples.LastOrDefault();

            if (item == null)
            {
                throw new Exception("Can't add null item to the list.");
            }

            if (item.Header == null)
            {
                throw new Exception("Header can't be null.");
            }

            if (item.Payload == null)
            {
                throw new Exception("Payload can't be null.");
            }

            if (lastItem != null && item.Header.Time < lastItem.Header.Time)
            {
                Debug.Log(item.Payload + " | " + lastItem.Payload);
                throw new Exception("Can't add a sample with a time smaller than the last sample's time in the list.");
            }

            _samples.Add(item);
        }

        public void Clear()
        {
            _samples.Clear();
        }

        public bool Contains(UnpackedSample item)
        {
            return _samples.Contains(item);
        }

        public void CopyTo(UnpackedSample[] array, int arrayIndex)
        {
            _samples.CopyTo(array, arrayIndex);
        }

        public bool Remove(UnpackedSample item)
        {
            return _samples.Remove(item);
        }

        public int IndexOf(UnpackedSample item)
        {
            return _samples.IndexOf(item);
        }

        public void Insert(int index, UnpackedSample item)
        {
            if (item == null)
            {
                throw new Exception("Can't insert null item to the list.");
            }

            if (item.Header == null)
            {
                throw new Exception("Header can't be null.");
            }

            if (item.Payload == null)
            {
                throw new Exception("Payload can't be null.");
            }

            if (index > 0 && _samples[index - 1].Header.Time > item.Header.Time ||
                index < Count - 1 && _samples[index + 1].Header.Time < item.Header.Time)
            {
                throw new Exception(
                    "Can't insert a sample with a time smaller than the previous sample's time or greater than the next sample's time in the list.");
            }

            _samples.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _samples.RemoveAt(index);
        }

        public UnpackedSample this[int index]
        {
            get => _samples[index];
            set
            {
                if (value == null)
                {
                    throw new Exception("Can't insert null item to the list.");
                }

                if (index > 0 && _samples[index - 1].Header.Time > value.Header.Time ||
                    index < Count - 1 && _samples[index + 1].Header.Time < value.Header.Time)
                {
                    throw new Exception(
                        "Can't insert a sample with a time smaller than the previous sample's time or greater than the next sample's time in the list.");
                }

                _samples[index] = value;
            }
        }
    }
}