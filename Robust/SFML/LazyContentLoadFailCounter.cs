using System.Diagnostics;
using System.Linq;
using System.Reflection;
using log4net;
using NetGore;

namespace SFML
{
    /// <summary>
    /// Keeps track of the number of times lazy content fails to load and when it can attempt to load again.
    /// </summary>
    class LazyContentLoadFailCounter
    {
        static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// The number of attempts to make before raising a debug assertion.
        /// </summary>
        const int _attemptsBeforeAssert = 3;

        byte _attempt;
        TickCount _nextAttemptTime;

        /// <summary>
        /// Gets the current attempt number. When 0, there has been no failures since the last attempt.
        /// </summary>
        public int CurrentAttempt
        {
            get { return _attempt; }
        }

        /// <summary>
        /// Gets if enough time has elapsed since the last attempt to attempt loading again.
        /// </summary>
        public bool HasEnoughTimeElapsed
        {
            get { return _attempt == 0 || _nextAttemptTime < TickCount.Now; }
        }

        /// <summary>
        /// Gets the delay between loading attempts.
        /// </summary>
        /// <param name="attempt">The current attempt number.</param>
        /// <returns>The delay for the <paramref name="attempt"/>.</returns>
        static uint GetAttemptDelay(int attempt)
        {
            switch (attempt)
            {
                case 0:
                case 1:
                    return 0; // No delay

                case 2:
                    return 50; // 0.05 seconds

                case 3:
                    return 500; // 0.5 seconds

                case 4:
                    return 5000; // 5 seconds

                default:
                    const int lower = 1000 * 60 * 10; // 10 minutes
                    const int upper = 1000 * 60 * 20; // 20 minutes
                    return (uint)RandomHelper.NextInt(lower, upper);
            }
        }

        /// <summary>
        /// Handles when an <see cref="LoadingFailedException"/> is thrown while trying to load the lazy content.
        /// </summary>
        /// <param name="sender">The lazy content that tried to load.</param>
        /// <param name="ex">The <see cref="LoadingFailedException"/>.</param>
        public void HandleLoadException(object sender, LoadingFailedException ex)
        {
            // Increment attempt counter
            if (_attempt < byte.MaxValue)
                _attempt++;

            // Log
            const string errmsg = "Failed to load content `{0}` (attempt: {1}). Exception: {2}";
            if (log.IsErrorEnabled)
                log.ErrorFormat(errmsg, sender, _attempt, ex);

            // If multiple failures, raise a debug assertion
            if (_attempt == _attemptsBeforeAssert)
                Debug.Fail(string.Format(errmsg, sender, _attempt, ex));

            // Update delay time
            var now = TickCount.Now;
            var delay = GetAttemptDelay(_attempt);

            _nextAttemptTime = now + delay;
        }
    }
}