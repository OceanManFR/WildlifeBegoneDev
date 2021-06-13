using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using ModSettings;

namespace WildlifeBegone
{
    internal class WildlifeBegoneSettings : JsonModSettings
    {

        [Name("Enable Logging")]
        [Description("Determines whether mod activities are shown in the in-game console.")]
        public bool enableLogging = true;
        [Name("Enable in Story Mode")]
        [Description("Determines whether mod is activated in activities are shown in the in-game console.")]
        public bool enableInStoryMode = true;
        [Name("Enable in Story Mode")]
        [Description("Determines whether mod is activated in activities are shown in the in-game console.")]
        [Slider(0f, 10f, 101)]
        public float activeSpawnerCountMultiplier = 1f;

    }
    internal static class Settings
    {
        internal static WildlifeBegoneSettings options;
        internal static void OnLoad()
        {
            options = new WildlifeBegoneSettings();
            options.AddToModSettings("WildlifeBegone", MenuType.MainMenuOnly);
        }
    }
}
