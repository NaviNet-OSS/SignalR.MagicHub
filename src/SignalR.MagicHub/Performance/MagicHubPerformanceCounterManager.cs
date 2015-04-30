using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
#if !COUNTERINSTALLER
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.AspNet.SignalR;

#endif

namespace SignalR.MagicHub.Performance
{
    /// <summary>
    /// Represents a performance counter manager which will abstract consumption of perf counters
    /// </summary>
    public interface IMagicHubPerformanceCounterManager
    {
        /// <summary>
        /// Initializes the performance counters.
        /// </summary>
        /// <param name="instanceName">The host instance name.</param>
        /// <param name="hostShutdownToken">The CancellationToken representing the host shutdown.</param>
        void Initialize(string instanceName, CancellationToken hostShutdownToken);


        /// <summary>
        /// Gets the counter for "Number of messages sent to SignalR component for distribution"
        /// </summary>
        /// <value>
        /// The number dispatched to signal r total.
        /// </value>
        [PerformanceCounter(CounterType = PerformanceCounterType.NumberOfItems64, Description = "Number of messages sent to SignalR component for distribution", Name = "Messages Dispatched to SignalR Total")]
        IPerformanceCounter NumberDispatchedToSignalRTotal { get; }

        /// <summary>
        /// Gets the counter for "Number of messages received from active message bus component"
        /// </summary>
        /// <value>
        /// The number received from message bus total.
        /// </value>
        [PerformanceCounter(CounterType = PerformanceCounterType.NumberOfItems64, Description = "Number of messages received from active message bus component", Name = "Messages Received from Message Bus Total")]
        IPerformanceCounter NumberReceivedFromMessageBusTotal { get; }

        /// <summary>
        /// Gets the number processed messages per second.
        /// </summary>
        /// <value>
        /// The number processed messages per second.
        /// </value>
        [PerformanceCounter(CounterType = PerformanceCounterType.RateOfCountsPerSecond32, Description = "Number of messages processed per second", Name = "Messages processed per second")]
        IPerformanceCounter NumberProcessedMessagesPerSecond { get; }

        /// <summary>
        /// Gets the counter for "Number of filters parsed total"
        /// </summary>
        /// <value>
        /// The number of filters parsed total.
        /// </value>
        [PerformanceCounter(CounterType = PerformanceCounterType.NumberOfItems64, Description = "Number of filters parsed total", Name = "Filters parsed total")]
        IPerformanceCounter NumberOfFiltersParsedTotal { get; }

        /// <summary>
        /// Gets the counter for "Number of filters parsed per second (cache miss)"
        /// </summary>
        /// <value>
        /// The number of filters parsed per sec.
        /// </value>
        [PerformanceCounter(CounterType = PerformanceCounterType.RateOfCountsPerSecond32, Description = "Number of filters parsed per second (cache miss)", Name = "Filters parsed per second")]
        IPerformanceCounter NumberOfFiltersParsedPerSec { get; }

        /// <summary>
        /// Gets the counter for "Number of active subscriptions total"
        /// </summary>
        /// <value>
        /// The number of subscriptions total.
        /// </value>
        [PerformanceCounter(CounterType = PerformanceCounterType.NumberOfItems64, Description = "Number of active subscriptions total", Name = "Active Subscriptions total")]
        IPerformanceCounter NumberOfSubscriptionsTotal { get; }
    }

    /// <summary>
    /// Wraps interaction with perf counters in MagicHub
    /// </summary>
    public class MagicHubPerformanceCounterManager : IMagicHubPerformanceCounterManager
    {
        /// <summary>
        /// The performance counter category name for SignalR counters.
        /// </summary>
        public const string CategoryName = "MagicHub";

        private static readonly PropertyInfo[] _counterProperties = GetCounterPropertyInfo();
        private static readonly IPerformanceCounter _noOpCounter = new NoOpPerformanceCounter();
        private volatile bool _initialized;
        private readonly object _initLocker = new object();

#if !COUNTERINSTALLER
        private readonly TraceSource _trace;

        /// <summary>
        /// Initializes a new instance of the <see cref="MagicHubPerformanceCounterManager"/> class.
        /// </summary>
        public MagicHubPerformanceCounterManager()
            : this(GlobalHost.DependencyResolver.Resolve<ITraceManager>())
        {
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public MagicHubPerformanceCounterManager(ITraceManager traceManager)
        {
            InitNoOpCounters();

            if (traceManager == null)
            {
                throw new ArgumentNullException("traceManager");
            }

            _trace = traceManager["SignalR.MagicHub.PerformanceCounterManager"];
        }
#else
        public MagicHubPerformanceCounterManager()
        {
            InitNoOpCounters();
        }
#endif


        internal string InstanceName { get; private set; }

        /// <summary>
        /// Initializes the performance counters.
        /// </summary>
        /// <param name="instanceName">The host instance name.</param>
        /// <param name="hostShutdownToken">The CancellationToken representing the host shutdown.</param>
        public void Initialize(string instanceName, CancellationToken hostShutdownToken)
        {
            if (_initialized)
            {
                return;
            }

            var needToRegisterWithShutdownToken = false;
            lock (_initLocker)
            {
                if (!_initialized)
                {
                    InstanceName = SanitizeInstanceName(instanceName);
                    SetCounterProperties();
                    // The initializer ran, so let's register the shutdown cleanup
                    if (hostShutdownToken != CancellationToken.None)
                    {
                        needToRegisterWithShutdownToken = true;
                    }
                    _initialized = true;
                }
            }

            if (needToRegisterWithShutdownToken)
            {
                hostShutdownToken.Register(UnloadCounters);
            }
        }

        private void UnloadCounters()
        {
            lock (_initLocker)
            {
                if (!_initialized)
                {
                    // We were never initalized
                    return;
                }
            }

            var counterProperties = this.GetType()
                .GetProperties()
                .Where(p => p.PropertyType == typeof(IPerformanceCounter));

            foreach (var property in counterProperties)
            {
                var counter = property.GetValue(this, null) as IPerformanceCounter;
                counter.Close();
                counter.RemoveInstance();
            }
        }

        private void InitNoOpCounters()
        {
            // Set all the counter properties to no-op by default.
            // These will get reset to real counters when/if the Initialize method is called.
            foreach (var property in _counterProperties)
            {
                property.SetValue(this, new NoOpPerformanceCounter(), null);
            }
        }

        private void SetCounterProperties()
        {
            var loadCounters = true;

            foreach (var property in _counterProperties)
            {
                PerformanceCounterAttribute attribute = GetPerformanceCounterAttribute(property);

                if (attribute == null)
                {
                    continue;
                }

                IPerformanceCounter counter = null;

                if (loadCounters)
                {
                    counter = LoadCounter(CategoryName, attribute.Name, isReadOnly: false);

                    if (counter == null)
                    {
                        // We failed to load the counter so skip the rest
                        loadCounters = false;
                    }
                }

                counter = counter ?? _noOpCounter;

                property.SetValue(this, counter, null);
            }
        }

        internal static PropertyInfo[] GetCounterPropertyInfo()
        {
            return typeof(MagicHubPerformanceCounterManager)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(IPerformanceCounter))
                .ToArray();
        }

        internal static PerformanceCounterAttribute GetPerformanceCounterAttribute(PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(PerformanceCounterAttribute), false)
                .Cast<PerformanceCounterAttribute>()
                .SingleOrDefault();
        }

        private static string SanitizeInstanceName(string instanceName)
        {
            // Details on how to sanitize instance names are at http://msdn.microsoft.com/en-us/library/vstudio/system.diagnostics.performancecounter.instancename
            if (string.IsNullOrWhiteSpace(instanceName))
            {
                instanceName = Guid.NewGuid().ToString();
            }

            // Substitute invalid chars with valid replacements
            var substMap = new Dictionary<char, char>
            {
                {'(', '['},
                {')', ']'},
                {'#', '-'},
                {'\\', '-'},
                {'/', '-'}
            };
            var sanitizedName = new String(instanceName.Select(c => substMap.ContainsKey(c) ? substMap[c] : c).ToArray());

            // Names must be shorter than 128 chars, see doc link above
            var maxLength = 127;
            return sanitizedName.Length <= maxLength
                ? sanitizedName
                : sanitizedName.Substring(0, maxLength);
        }

        private IPerformanceCounter LoadCounter(string categoryName, string counterName, bool isReadOnly)
        {
            return LoadCounter(categoryName, counterName, InstanceName, isReadOnly);
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "This file is shared")]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "Counters are disposed later")]
        private IPerformanceCounter LoadCounter(string categoryName, string counterName, string instanceName,
            bool isReadOnly)
        {
            // See http://msdn.microsoft.com/en-us/library/356cx381.aspx for the list of exceptions
            // and when they are thrown. 
            try
            {
                var counter = new PerformanceCounter(categoryName, counterName, instanceName, isReadOnly);

                // Initialize the counter sample
                counter.NextSample();

                return new PerformanceCounterWrapper(counter);
            }
#if !COUNTERINSTALLER
            catch (InvalidOperationException ex)
            {
                _trace.TraceEvent(TraceEventType.Error, 0,
                    "Performance counter failed to load: " + ex.GetBaseException());
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                _trace.TraceEvent(TraceEventType.Error, 0,
                    "Performance counter failed to load: " + ex.GetBaseException());
                return null;
            }
            catch (Win32Exception ex)
            {
                _trace.TraceEvent(TraceEventType.Error, 0,
                    "Performance counter failed to load: " + ex.GetBaseException());
                return null;
            }
            catch (PlatformNotSupportedException ex)
            {
                _trace.TraceEvent(TraceEventType.Error, 0,
                    "Performance counter failed to load: " + ex.GetBaseException());
                return null;
            }
#else
            catch (InvalidOperationException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            catch (Win32Exception) { return null; }
            catch (PlatformNotSupportedException) { return null; }
#endif
        }


        #region Performance Counter Properties
        /// <summary>
        /// Gets the counter for "Number of messages sent to SignalR component for distribution"
        /// </summary>
        /// <value>
        /// The number dispatched to signal r total.
        /// </value>
        [PerformanceCounter(CounterType = PerformanceCounterType.NumberOfItems64, Description = "Number of messages sent to SignalR component for distribution", Name = "Messages Dispatched to SignalR Total")]
        public IPerformanceCounter NumberDispatchedToSignalRTotal { get; set; }
        /// <summary>
        /// Gets the counter for "Number of messages received from active message bus component"
        /// </summary>
        /// <value>
        /// The number received from message bus total.
        /// </value>
        [PerformanceCounter(CounterType = PerformanceCounterType.NumberOfItems64, Description = "Number of messages received from active message bus component", Name = "Messages Received from Message Bus Total")]
        public IPerformanceCounter NumberReceivedFromMessageBusTotal { get; set; }
        /// <summary>
        /// Gets the number processed messages per second.
        /// </summary>
        /// <value>
        /// The number processed messages per second.
        /// </value>
        [PerformanceCounter(CounterType = PerformanceCounterType.RateOfCountsPerSecond32, Description = "Number of messages processed per second", Name = "Messages processed per second")]
        public IPerformanceCounter NumberProcessedMessagesPerSecond { get; set; }

        /// <summary>
        /// Gets the counter for "Number of filters parsed total"
        /// </summary>
        /// <value>
        /// The number of filters parsed total.
        /// </value>
        [PerformanceCounter(CounterType = PerformanceCounterType.NumberOfItems64, Description = "Number of filters parsed total", Name = "Filters parsed total")]
        public IPerformanceCounter NumberOfFiltersParsedTotal{ get; set; }

        /// <summary>
        /// Gets the counter for "Number of filters parsed per second (cache miss)"
        /// </summary>
        /// <value>
        /// The number of filters parsed per sec.
        /// </value>
        [PerformanceCounter(CounterType = PerformanceCounterType.RateOfCountsPerSecond32, Description = "Number of filters parsed per second (cache miss)", Name = "Filters parsed per second")]
        public IPerformanceCounter NumberOfFiltersParsedPerSec { get; set; }

        /// <summary>
        /// Gets the counter for "Number of active subscriptions total"
        /// </summary>
        /// <value>
        /// The number of subscriptions total.
        /// </value>
        [PerformanceCounter(CounterType = PerformanceCounterType.NumberOfItems64, Description = "Number of active subscriptions total", Name = "Active Subscriptions total")]
        public IPerformanceCounter NumberOfSubscriptionsTotal { get; set; }
        #endregion
    }
}