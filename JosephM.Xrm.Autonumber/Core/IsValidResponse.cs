﻿#region

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace JosephM.Xrm.Autonumber.Core
{
    /// <summary>
    ///     General Use Object For Providing A Response Of Whether Something is Valid Or Not
    /// </summary>
    public class IsValidResponse
    {
        private readonly List<string> _invalidReasons = new List<string>();

        public bool IsValid
        {
            get { return !_invalidReasons.Any(); }
        }

        public void AddInvalidReason(string reason)
        {
            _invalidReasons.Add(reason);
        }

        public IEnumerable<string> InvalidReasons
        {
            get { return _invalidReasons; }
        }

        public string GetErrorString()
        {
            return String.Join("\n", InvalidReasons);
        }
    }
}