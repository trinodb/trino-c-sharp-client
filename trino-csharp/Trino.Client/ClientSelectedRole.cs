using System;
using System.Text.RegularExpressions;
using Trino.Client.Utils;

namespace Trino.Client
{
    /// <summary>
    /// Supports Trino ClientSelectedRole header.
    /// </summary>
    public class ClientSelectedRole
    {
        public enum Type
        {
            ROLE, ALL, NONE
        }

        public Type RoleType { get; }
        public string Role { get; }

        public ClientSelectedRole(Type roleType, string role)
        {
            this.RoleType = roleType.IsNullArgument("roletype");
            this.Role = role.IsNullArgument("role");
        }

        public override bool Equals(Object o)
        {
            if (this == o)
            {
                return true;
            }
            if (o == null || this.GetType() != o.GetType())
            {
                return false;
            }
            ClientSelectedRole that = (ClientSelectedRole)o;
            return RoleType == that.RoleType &&
                    Role == this.Role;
        }

        public override int GetHashCode()
        {
            return RoleType.GetHashCode() ^ Role.GetHashCode();
        }

        public ClientSelectedRole Clone()
        {
            return new ClientSelectedRole(this.RoleType, this.Role);
        }
    }
}
