using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace TurtleRoute
{
    /// <summary>
    /// Logs the failures of pipeline layers without flooding the log during an outage.
    /// </summary>
    /// <remarks>
    /// The first failure of a layer is logged with its exception. Further failures are counted and logged as one summary
    /// per interval while the layer keeps failing, and the first call that succeeds again logs that the layer recovered.
    /// Share one instance between pipelines, as the DI registration does, so the state outlives transient pipelines.
    /// A layer is identified by its name and position, so two layers of the same type, e.g. with different keys, are tracked apart.
    /// </remarks>
    public sealed class LayerFailureLog
    {
        private readonly ConcurrentDictionary<(string Layer, int Position), State> _layers = new();
        private readonly TimeSpan _interval;
        private readonly TimeProvider _timeProvider;

        public LayerFailureLog(TimeSpan? interval = null, TimeProvider timeProvider = null)
        {
            _interval = interval ?? TimeSpan.FromMinutes(5);
            if (_interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "The interval must be positive.");

            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public void Failed(ILogger logger, string layer, int position, Exception exception)
        {
            State state = _layers.GetOrAdd((layer, position), _ => new State());
            DateTimeOffset now = _timeProvider.GetUtcNow();

            lock (state)
            {
                if (!state.Failing)
                {
                    state.Failing = true;
                    state.FailingSince = state.SummarizedAt = now;
                    state.Total = 1;
                    state.SinceSummary = 0;

                    logger.LogWarning(exception, "{Layer} failed, trying the next layer. Further failures are summarized every {Interval} until it recovers.", layer, _interval);
                    return;
                }

                state.Total++;
                state.SinceSummary++;
                state.LastError = exception.Message;

                if (now - state.SummarizedAt < _interval)
                    return;

                logger.LogWarning("{Layer} failed {Failures} more times in the last {Elapsed}, trying the next layer. Last error: {LastError}", layer, state.SinceSummary, now - state.SummarizedAt, state.LastError);
                state.SummarizedAt = now;
                state.SinceSummary = 0;
            }
        }

        public void Succeeded(ILogger logger, string layer, int position)
        {
            if (!_layers.TryGetValue((layer, position), out State state) || !state.Failing)
                return;

            lock (state)
            {
                if (!state.Failing)
                    return;

                state.Failing = false;
                logger.LogInformation("{Layer} recovered after {Failures} failures in {Duration}.", layer, state.Total, _timeProvider.GetUtcNow() - state.FailingSince);
            }
        }

        private sealed class State
        {
            public volatile bool Failing;
            public DateTimeOffset FailingSince;
            public DateTimeOffset SummarizedAt;
            public int Total;
            public int SinceSummary;
            public string LastError;
        }
    }
}
