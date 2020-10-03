/*
 * Copyright (c) 2012-2019 Snowflake Computing Inc. All rights reserved.
 */

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;

namespace Snowflake.Data.Client
{
    public class SnowflakeDbConnectionStringBuilder : DbConnectionStringBuilder
    {
        internal static readonly Dictionary<string, ConnectionPropertySpecs> _validKeywords = CreateKeywords();

        // Regex pattern to validate authenticator settings.
        private static readonly Regex _validAuthenticators = new Regex(@"^(snowflake|externalbrowser|(https://.+\.okta\.com))$", RegexOptions.IgnoreCase);

        internal struct ConnectionPropertySpecs
        {
            public ConnectionPropertySpecs(Type valueType, object defaultValue, Func<DbConnectionStringBuilder, bool> required)
            {
                this.valueType = valueType;
                this.defaultValue = defaultValue;
                this.required = required;
            }
            internal Type valueType;
            internal object defaultValue;
            internal Func<DbConnectionStringBuilder, bool> required;
        }

        private static Dictionary<string, ConnectionPropertySpecs> CreateKeywords()
        {
            Dictionary<string, ConnectionPropertySpecs> keywords = new Dictionary<string, ConnectionPropertySpecs>(StringComparer.OrdinalIgnoreCase);
            keywords.Add("account", new ConnectionPropertySpecs(typeof(string), null, (b) => { return true; }));
            keywords.Add("db", new ConnectionPropertySpecs(typeof(string), null, (b) => { return false; }));
            keywords.Add("host", new ConnectionPropertySpecs(typeof(string), null, (b) => { return false; }));
            keywords.Add("password", new ConnectionPropertySpecs(typeof(string), null, (b) => { return !"externalbrowser".Equals(b["authenticator"] as string, StringComparison.OrdinalIgnoreCase); }));
            keywords.Add("role", new ConnectionPropertySpecs(typeof(string), null, (b) => { return false; }));
            keywords.Add("schema", new ConnectionPropertySpecs(typeof(string), null, (b) => { return false; }));
            keywords.Add("user", new ConnectionPropertySpecs(typeof(string), null, (b) => { return true; }));
            keywords.Add("warehouse", new ConnectionPropertySpecs(typeof(string), null, (b) => { return false; }));
            keywords.Add("connection_timeout", new ConnectionPropertySpecs(typeof(int), 120, (b) => { return false; }));
            keywords.Add("authenticator", new ConnectionPropertySpecs(typeof(string), "snowflake", (b) => { return false; }));
            keywords.Add("validate_default_parameters", new ConnectionPropertySpecs(typeof(string), true, (b) => { return false; }));
            return keywords;
        }

        public SnowflakeDbConnectionStringBuilder() : base()
        {

        }

        /// <summary>
        /// Helper to create valid connection strings from a collection of properties
        /// </summary>
        /// <param name="useOdbcRules">True uses curly braces as field delimiters, false uses quotation marks.</param>
        public SnowflakeDbConnectionStringBuilder(bool useOdbcRules) : base(useOdbcRules)
        {

        }

        public override object this[string keyword]
        {
            get
            {
                if (keyword == null)
                {
                    throw new ArgumentNullException(nameof(keyword));
                }
                if (_validKeywords.ContainsKey(keyword))
                {
                    return base[keyword];
                }
                return null;
            }
            set
            {
                if (keyword == null)
                {
                    throw new ArgumentNullException(nameof(keyword));
                }
                if (_validKeywords.ContainsKey(keyword))
                {
                    if (keyword.Equals("authenticator") && !_validAuthenticators.IsMatch(value as string))
                    {
                        throw new ArgumentException("invalid authenticator value", nameof(value));
                    }
                    base[keyword] = value;
                }
                else
                {
                    throw new ArgumentException("unsupported keyword", nameof(keyword));
                }
            }
        }

        public new string ConnectionString
        {
            get
            {
                if (_validKeywords.All(k => k.Value.required(this) && base[k.Key] != null))
                {
                    return base.ConnectionString;
                }
                throw new InvalidOperationException($"Missing required connection parameters: {string.Join(", ", _validKeywords.Where(k => k.Value.required(this) && base[k.Key] == null).Select(k => k.Key))}.");
            }
            set {
                var dbsb = new DbConnectionStringBuilder();
                dbsb.ConnectionString = value;
                foreach (var key in dbsb.Keys) {
                    if (key is string && _validKeywords.ContainsKey((string)key)) {
                        continue;
                    }
                    throw new InvalidOperationException($"invalid key supplied: {key}");
                }
                base.ConnectionString = value;
            }
        }
    }
}
