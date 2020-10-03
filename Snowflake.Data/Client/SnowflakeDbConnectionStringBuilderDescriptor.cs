using System.ComponentModel;
using System;
using System.Runtime.Serialization;
using System.Data;
using System.Data.Common;

namespace Snowflake.Data.Client
{
    internal class SnowflakeDbConnectionStringBuilderDescriptor : PropertyDescriptor
    {
        private Type _componentType;
        private Type _propertyType;
        private bool _isReadOnly;
        private bool _refreshOnChange;

        internal SnowflakeDbConnectionStringBuilderDescriptor(string propertyName, Type componentType, Type propertyType, bool isReadOnly, Attribute[] attributes) : base(propertyName, attributes)
        {
            _componentType = componentType;
            _propertyType = propertyType;
            _isReadOnly = isReadOnly;
        }

        internal bool RefreshOnChange
        {
            get
            {
                return _refreshOnChange;
            }
            set
            {
                _refreshOnChange = value;
            }
        }

        public override Type ComponentType
        {
            get
            {
                return _componentType;
            }
        }
        public override bool IsReadOnly
        {
            get
            {
                return _isReadOnly;
            }
        }
        public override Type PropertyType
        {
            get
            {
                return _propertyType;
            }
        }

        public override bool CanResetValue(object component)
        {
            DbConnectionStringBuilder builder = (component as DbConnectionStringBuilder);
            return ((null != builder) && builder.ShouldSerialize(DisplayName));
        }

        public override object GetValue(object component)
        {
            DbConnectionStringBuilder builder = (component as DbConnectionStringBuilder);
            if (null != builder)
            {
                object value;
                if (builder.TryGetValue(DisplayName, out value))
                {
                    return value;
                }
            }
            return null;
        }

        public override void ResetValue(object component)
        {
            DbConnectionStringBuilder builder = (component as DbConnectionStringBuilder);
            if (null != builder)
            {
                builder.Remove(DisplayName);

                if (RefreshOnChange)
                {
                    builder.ClearPropertyDescriptors();
                }
            }
        }
        
        public override void SetValue(object component, object value)
        {
            DbConnectionStringBuilder builder = (component as DbConnectionStringBuilder);
            if (null != builder)
            {
                // via the editor, empty string does a defacto Reset
                if ((typeof(string) == PropertyType) && String.Empty.Equals(value))
                {
                    value = null;
                }
                builder[DisplayName] = value;

                if (RefreshOnChange)
                {
                    builder.ClearPropertyDescriptors();
                }
            }
        }
        public override bool ShouldSerializeValue(object component)
        {
            DbConnectionStringBuilder builder = (component as DbConnectionStringBuilder);
            return ((null != builder) && builder.ShouldSerialize(DisplayName));
        }
    }
}