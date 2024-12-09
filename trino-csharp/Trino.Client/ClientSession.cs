using System;

using Trino.Client.Auth;

namespace Trino.Client
{
    /// <summary>
    /// Contains the configuration and state for a Trino client session, including authentication
    /// and session properties. Handles both initial setup and updates during query execution.
    /// </summary>
    public class ClientSession
    {
        /// <summary>
        /// Gets or sets the authentication method used for Trino connections.
        /// </summary>
        public ITrinoAuth Auth { get; set; }

        /// <summary>
        /// Gets the session properties that define connection behavior and settings.
        /// </summary>
        public ClientSessionProperties Properties { get; private set; }

        /// <summary>
        /// Creates a new client session with default settings.
        /// </summary>
        public ClientSession()
            : this(new ClientSessionProperties(), null)
        {
        }

        /// <summary>
        /// Creates a new client session with the specified server and authentication.
        /// </summary>
        /// <param name="server">The Trino server URI.</param>
        /// <param name="auth">The authentication provider.</param>
        public ClientSession(Uri server, ITrinoAuth auth)
            : this(new ClientSessionProperties { Server = server }, auth)
        {
        }

        /// <summary>
        /// Creates a new client session with custom properties and authentication.
        /// </summary>
        /// <param name="sessionProperties">The session configuration properties.</param>
        /// <param name="auth">The authentication provider.</param>
        public ClientSession(ClientSessionProperties sessionProperties, ITrinoAuth auth = null)
        {
            Properties = sessionProperties;
            Auth = auth;
        }

        /// <summary>
        /// Updates the session properties with output values returned from Trino during query execution.
        /// This ensures the session maintains consistency with server-side settings.
        /// </summary>
        /// <param name="clientSessionOutput">The session output values from Trino.</param>
        internal void Update(ClientSessionOutput clientSessionOutput)
        {
            Properties = Properties.Combine(clientSessionOutput);
        }
    }
}