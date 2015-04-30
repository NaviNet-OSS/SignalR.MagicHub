using System.Diagnostics;

namespace SignalR.MagicHub.Performance
{
    /// <summary>
    /// Represents a wrapper for a windows performance counter
    /// </summary>
    public interface IPerformanceCounter
    {
        /// <summary>
        /// Gets the name of the counter.
        /// </summary>
        /// <value>
        /// The name of the counter.
        /// </value>
        string CounterName { get; }

        /// <summary>
        /// Decrements this instance.
        /// </summary>
        /// <returns></returns>
        long Decrement();

        /// <summary>
        /// Increments this instance.
        /// </summary>
        /// <returns></returns>
        long Increment();

        /// <summary>
        /// Increments the by a given value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        long IncrementBy(long value);

        /// <summary>
        /// Sets up the counter.
        /// </summary>
        /// <returns></returns>
        CounterSample NextSample();

        /// <summary>
        /// Gets or sets the raw value.
        /// </summary>
        /// <value>
        /// The raw value.
        /// </value>
        long RawValue { get; set; }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        void Close();

        /// <summary>
        /// Removes the instance.
        /// </summary>
        void RemoveInstance();
    }
}
