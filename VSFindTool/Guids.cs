// Guids.cs
// MUST match guids.h
using System;

namespace VSFindTool
{
    static class GuidList
    {
        public const string guidVSFindToolPkgString = "d37a74de-de35-4523-9a74-ace463f70eb1";
        public const string guidVSFindToolCmdSetString = "ecf219a1-c79f-454d-8731-158ecfd8ee8d";
        public const string guidToolWindowPersistanceString = "f812bf92-f6c3-47d8-b4f9-dc175924ac28";

        public static readonly Guid guidVSFindToolCmdSet = new Guid(guidVSFindToolCmdSetString);
    };
}