using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ReadLens.EditorTools
{
    // Release signing for the Meta dashboard, from a keystore kept outside the repo.
    // ~/.android-keystores/readlens-release.txt holds keystore=, alias=, storepass=, keypass= lines;
    // READLENS_KEYSTORE, READLENS_KEY_ALIAS, READLENS_STORE_PASS, READLENS_KEY_PASS override them.
    static class ReleaseSigning
    {
        static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        static readonly string Config = Path.Combine(Home, ".android-keystores", "readlens-release.txt");

        public static bool Apply()
        {
            var values = File.Exists(Config)
                ? File.ReadAllLines(Config).Select(l => l.Split(new[] { '=' }, 2)).Where(p => p.Length == 2)
                    .ToDictionary(p => p[0].Trim(), p => p[1].Trim())
                : new Dictionary<string, string>();
            string Get(string key, string env, string fallback = "") =>
                Environment.GetEnvironmentVariable(env) ?? (values.TryGetValue(key, out var v) ? v : fallback);

            var keystore = Get("keystore", "READLENS_KEYSTORE", Path.Combine(Home, ".android-keystores", "readlens-release.keystore"));
            if (!File.Exists(keystore))
            {
                Debug.LogWarning($"[ReadLens] No release keystore at {keystore}: DEBUG-signed APK, the Meta dashboard will reject it.");
                PlayerSettings.Android.useCustomKeystore = false;
                return false;
            }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = Get("storepass", "READLENS_STORE_PASS");
            PlayerSettings.Android.keyaliasName = Get("alias", "READLENS_KEY_ALIAS", "readlens");
            PlayerSettings.Android.keyaliasPass = Get("keypass", "READLENS_KEY_PASS");
            Debug.Log($"[ReadLens] Release signing with {keystore} (alias {PlayerSettings.Android.keyaliasName})");
            return true;
        }

        // Passwords and the home-dir path never end up in ProjectSettings.
        public static void Clear()
        {
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasPass = "";
            PlayerSettings.Android.keystoreName = "";
            PlayerSettings.Android.keyaliasName = "";
            PlayerSettings.Android.useCustomKeystore = false;
        }
    }
}
