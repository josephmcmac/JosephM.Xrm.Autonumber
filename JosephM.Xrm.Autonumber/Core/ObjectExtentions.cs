﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JosephM.Xrm.Autonumber.Core
{
    public static class ObjectExtentions
    {
        public static object GetPropertyValue(this object instance, string propertyName)
        {
            var type = instance.GetType();
            var property = type.GetProperty(propertyName);
            if (property == null)
                throw new NullReferenceException(string.Format("Class {0} Does Not Have A Property Named {1}", type.Name,
                    propertyName));
            if (!property.CanRead)
                throw new MemberAccessException(string.Format("Property {0} Does Not Have Read Access In {1} Class",
                    propertyName, type.Name));
            return property
                .GetGetMethod()
                .Invoke(instance, new object[] { });
        }

        public static object SetPropertyValue(this object instance, string propertyName, object value)
        {
            var type = instance.GetType();
            var property = type.GetProperty(propertyName);
            if (property == null)
                throw new NullReferenceException(string.Format("Class {0} Does Not Have A Property Named {1}", type.Name,
                    propertyName));
            if (!property.CanWrite)
                throw new MemberAccessException(string.Format("Property {0} Does Not Have Write Access In {1} Class",
                    propertyName, type.Name));
            return property
                .GetSetMethod()
                .Invoke(instance, new object[] { value });
        }

        public static bool IsInContext(this object instance, string propertyName)
        {
            var inContextAttributes =
                instance.GetType()
                    .GetProperty(propertyName)
                    .GetCustomAttributes(typeof(PropertyInContext), true)
                    .Cast<PropertyInContext>();
            if (!inContextAttributes.Any())
                return true;
            return inContextAttributes.All(a => a.IsInContext(instance));
        }


        /// <summary>
        ///     Returns if the object value is considered empty. Note an enumerable of no objects is considered empty
        /// </summary>
        public static bool IsEmpty(this object instance)
        {
            if (instance is string)
                return ((string)instance).IsNullOrWhiteSpace();
            if (instance is IEnumerable)
            {
                var any = ((IEnumerable)instance).GetEnumerator().MoveNext();
                return !any;
            }
            return instance == null;
        }

        public static bool IsNotEmpty(this object instance)
        {
            return !instance.IsEmpty();
        }

        /// <summary>
        ///     Sets The Property Value Of The Object From The Raw String Value Loaded From app.config
        /// </summary>
        public static void SetPropertyByString(this object theObject, string propertyName, string rawConfigString)
        {
            if (rawConfigString.IsNullOrWhiteSpace())
                theObject.SetPropertyValue(propertyName, null);

            var property = theObject.GetType().GetProperty(propertyName);
            var propertyType = property.PropertyType;
            object newValue = null;

            if (propertyType == typeof(bool))
                newValue = rawConfigString == "1" || rawConfigString == "true";
            else if (propertyType.IsEnum)
                newValue = Enum.Parse(propertyType, rawConfigString);
            else if (propertyType == typeof(IEnumerable<string>))
                newValue = rawConfigString.Split(',');
            else if (propertyType == typeof(int))
                newValue = int.Parse(rawConfigString);
            else if (propertyType == typeof(Password))
                newValue = Password.CreateFromRawPassword(rawConfigString);
            else if (propertyType.HasStringConstructor())
                newValue = propertyType.CreateFromStringConstructor(rawConfigString);
            else
                newValue = rawConfigString;

            theObject.SetPropertyValue(propertyName, newValue);
        }
    }
}