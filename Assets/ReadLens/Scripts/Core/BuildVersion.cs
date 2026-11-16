using System;

namespace ReadLens.Core
{
    public static class BuildVersion
    {
        static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Android versionCode must grow with every upload to the Meta dashboard: minutes since 2026-01-01 UTC.
        public static int Code(DateTime utcNow) => (int)(utcNow.ToUniversalTime() - Epoch).TotalMinutes;
    }
}
