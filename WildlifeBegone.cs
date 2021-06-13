using MelonLoader;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace WildlifeBegone
{
    public class WildlifeBegone : MelonMod
    {
        public override void OnApplicationStart()
        {
            Settings.OnLoad();
            Debug.Log($"[{Info.Name}] Version {Info.Version} loaded!");
            // MelonLogger.Log($"Version {Info.Version} loaded!");
        }
    }
}
