﻿using System;
using System.Threading;

namespace Ryujinx.Graphics.Gpu.Synchronization
{

    /// <summary>
    /// GPU synchronization handling
    /// </summary>
    public class Synchronization
    {
        /// <summary>
        /// The maximum number of syncpoints supported by the GM20B.
        /// </summary>
        public const int MaxHarwareSyncpoints = 192;

        /// <summary>
        /// Array containing all hardware syncpoints.
        /// </summary>
        private Syncpoint[] _syncpoints;

        public Synchronization()
        {
            _syncpoints = new Syncpoint[MaxHarwareSyncpoints];

            for (int i = 0; i < _syncpoints.Length; i++)
            {
                _syncpoints[i] = new Syncpoint();
            }
        }

        /// <summary>
        /// Increment the value of a syncpoint with a given id
        /// </summary>
        /// <param name="id">The id of the syncpoint</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when id >= MaxHarwareSyncpoints</exception>
        public void IncrementSyncpoint(uint id)
        {
            if (id >= MaxHarwareSyncpoints)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            _syncpoints[id].Increment();
        }

        /// <summary>
        /// Get the value of a syncpoint with a given id
        /// </summary>
        /// <param name="id">The id of the syncpoint</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when id >= MaxHarwareSyncpoints</exception>
        /// <returns>The value of the syncpoint</returns>
        public uint GetSyncpointValue(uint id)
        {
            if (id >= MaxHarwareSyncpoints)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return _syncpoints[id].Value;
        }

        /// <summary>
        /// Register a new callback on a syncpoint with a given id at a target threshold.
        /// The callback will be called once the threshold is reached and will automatically be unregistered.
        /// </summary>
        /// <param name="id">The id of the syncpoint</param>
        /// <param name="threshold">The target threshold</param>
        /// <param name="callback">The callback to call when the threshold is reached</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when id >= MaxHarwareSyncpoints</exception>
        public void RegisterCallbackOnSyncpoint(uint id, uint threshold, Action callback)
        {
            if (id >= MaxHarwareSyncpoints)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            _syncpoints[id].RegisterCallback(threshold, callback);
        }

        /// <summary>
        /// Wait on a syncpoint with a given id at a target threshold.
        /// The callback will be called once the threshold is reached and will automatically be unregistered.
        /// </summary>
        /// <param name="id">The id of the syncpoint</param>
        /// <param name="threshold">The target threshold</param>
        /// <param name="callback">The callback to call when the threshold is reached</param>
        /// <param name="timeout">The timeout</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when id >= MaxHarwareSyncpoints</exception>
        /// <returns>True if </returns>
        public bool WaitOnSyncpoint(uint id, uint threshold, TimeSpan timeout)
        {
            if (id >= MaxHarwareSyncpoints)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            using (ManualResetEvent waitEvent = new ManualResetEvent(false))
            {
                var info = _syncpoints[id].RegisterCallback(threshold, () => waitEvent.Set());

                bool timedout = waitEvent.WaitOne(timeout);

                if (timedout)
                {
                    _syncpoints[id].UnregisterCallback(info);
                }

                return timedout;
            }
        }
    }
}
