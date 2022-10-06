# Instant Cache Cleaner (ICC)
This is a simple C# console application that quickly soft-deletes (moves) files from a client application's cache directory. This is intended to clear the cache of a client application really quickly and to postpone the work of actually deleting the data for a later time. This code is published as the [Instant Cache Cleaner](https://find.vu/downloads/icc/ "Instant Cache Cleaner") program for the Find.vu website.

The .NET standard was targetted to initially to get binaries for Windows, Mac, and Linux, but the lack of testing environments and a few .NET hiccups means versions 1.0.x will be targeting Windows only.

Compiled on Windows 10, Visual Studio 2019.

## How to run
  1. Download and install Microsoft Visual Studio. ([Community edition](https://visualstudio.microsoft.com/vs/community/) is free)
2. Open "Instant Cache Cleaner.sln"
3. Hit F5 (Debug > Start Debugging)
