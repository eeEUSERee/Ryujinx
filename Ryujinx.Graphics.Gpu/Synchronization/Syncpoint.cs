using System;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Graphics.Gpu.Synchronization
{
    /// <summary>
    /// Represent GPU hardware syncpoint.
    /// </summary>
    class Syncpoint
    {
        public struct WaiterInformation
        {
            public uint   Threshold;
            public Action Callback;
        }

        private int _storedValue;

        public readonly uint Id;

        // TODO: get ride of this lock
        private object _listLock = new object();

        /// <summary>
        /// The value of the syncpoint.
        /// </summary>
        public uint Value => (uint)_storedValue;

        // TODO: switch to something handling concurrency?
        private List<WaiterInformation> _waiters;

        public Syncpoint()
        {
            _waiters = new List<WaiterInformation>();
        }

        /// <summary>
        /// Register a new callback for a target threshold.
        /// The callback will be called once the threshold is reached and will automatically be unregistered.
        /// </summary>
        /// <param name="threshold">The target threshold</param>
        /// <param name="callback">The callback to call when the threshold is reached</param>
        /// <returns>the created WaiterInformation object</returns>
        public WaiterInformation RegisterCallback(uint threshold, Action callback)
        {
            WaiterInformation waiterInformation = new WaiterInformation
            {
                Threshold = threshold,
                Callback  = callback
            };

            lock (_listLock)
            {
                _waiters.Add(waiterInformation);
            }

            return waiterInformation;
        }

        public void UnregisterCallback(WaiterInformation waiterInformation)
        {
            lock (_listLock)
            {
                _waiters.Remove(waiterInformation);
            }
        }

        /// <summary>
        /// Increment the syncpoint
        /// </summary>
        public void Increment()
        {
            uint currentValue = (uint)Interlocked.Increment(ref _storedValue);

            lock (_listLock)
            {
                _waiters.RemoveAll(item =>
                {
                    bool isPastThreshold = currentValue >= item.Threshold;

                    if (isPastThreshold)
                    {
                        item.Callback();
                    }

                    return isPastThreshold;
                });
            }
        }

    }
}
