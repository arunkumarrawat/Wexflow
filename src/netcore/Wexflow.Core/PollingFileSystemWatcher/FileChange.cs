﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;

#pragma warning disable CS1591
namespace Wexflow.Core.PollingFileSystemWatcher
{
    public readonly struct FileChange
    {
        internal FileChange(string directory, string path, WatcherChangeTypes type)
        {
            Debug.Assert(path != null);
            Directory = directory;
            Name = path;
            ChangeType = type;
        }

        public string Directory { get; }
        public string Name { get; }
        public WatcherChangeTypes ChangeType { get; }
    }
}
#pragma warning restore CS1591
