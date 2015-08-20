﻿using System;

namespace JosephM.Xrm.Autonumber.Core
{
    /// <summary>
    ///     Attribute To Define A Property As Cascading The Record Type To Another Property
    ///     Initally Used For Cacading A selected Record Type To A Record Field Or Lookup Property
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property,
        AllowMultiple = true)]
    public class RecordTypeFor : Attribute
    {
        public string LookupProperty { get; private set; }

        public RecordTypeFor(string lookupProperty)
        {
            LookupProperty = lookupProperty;
        }
    }
}