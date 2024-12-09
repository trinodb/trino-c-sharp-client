using System;
using System.Threading;

namespace Trino.Client
{
    /// <summary>
    /// Tracks the state of the query execution.
    /// </summary>
    public class QueryState
    {
        // State management
        private int state;

        internal QueryState() {
            this.state = (int)TrinoQueryStates.RUNNING;
        }

        public bool IsRunning { get { return this.state == (int)TrinoQueryStates.RUNNING; } }
        public bool IsClientAborted { get { return this.state == (int)TrinoQueryStates.CLIENT_ABORTED; } }
        public bool IsClientError { get { return this.state == (int)TrinoQueryStates.CLIENT_ERROR; } }
        public bool IsFinished { get { return this.state == (int)TrinoQueryStates.FINISHED; } }

        public override string ToString()
        {
            return Enum.GetName(typeof(TrinoQueryStates), this.state);
        }

        /// <summary>
        /// Transition from one state to another. Returns the state before the transition.
        /// </summary>
        internal bool StateTransition(TrinoQueryStates transitionTo, TrinoQueryStates transitionFrom)
        {
            Interlocked.CompareExchange(ref this.state, (int)transitionTo, (int)transitionFrom);
            return (TrinoQueryStates)state == transitionTo;
        }

        /// <summary>
        /// The possible states of the Trino client. Matches Java client.
        /// </summary>
        internal enum TrinoQueryStates
        {
            // submitted to server, not in terminal state (including planning, queued, running, etc)
            RUNNING,
            CLIENT_ERROR,
            CLIENT_ABORTED,
            // finished on remote Trino server (including failed and successfully completed)
            FINISHED,
        }
    }
}
