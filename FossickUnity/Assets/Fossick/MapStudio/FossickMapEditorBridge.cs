// ==================================================
// Copyright (c) Magic Tavern. All rights reserved.
// @Author: Codex
// @Maintainer: 林万厦
// @Date: 2026-07-14
// @Desc: Runtime bridge for opening Fossick map editor from HOP
// ==================================================

using System;
using Fossick.Core.Data;
using Fossick.Core.Definition.Config;

namespace Fossick.MapStudio
{
    public static class FossickMapEditorBridge
    {
        public static string ActSubType { get; private set; } = string.Empty;
        public static Action ExitEditor { get; set; }
        public static Action PlayOfficialMap { get; set; }
        public static Func<string, string> LoadBundledText { get; set; }
        public static bool IsOfficialMapTesting { get; private set; }
        public static int TestSeed { get; private set; }
        public static FossickMapConfig TestMapConfig { get; private set; }
        public static FossickGameplayData TestGameplayData { get; private set; }

        public static void Open(string actSubType)
        {
            ActSubType = string.IsNullOrEmpty(actSubType) ? string.Empty : actSubType.Trim();
        }

        public static void StartOfficialMapTest(FossickMapProjectConfig project, int testSeed)
        {
            TestSeed = testSeed;
            TestMapConfig = project == null ? null : project.ToRuntimeConfig();
            TestGameplayData = null;
            IsOfficialMapTesting = true;
            PlayOfficialMap?.Invoke();
        }

        public static void SaveTestGameplayData(FossickGameplayData data)
        {
            TestGameplayData = data;
        }

        public static void FinishOfficialMapTest()
        {
            IsOfficialMapTesting = false;
            TestSeed = 0;
            TestMapConfig = null;
            TestGameplayData = null;
        }

        public static void Close()
        {
            ExitEditor?.Invoke();
        }
    }
}
