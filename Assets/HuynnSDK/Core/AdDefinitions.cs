using System;
using UnityEngine;

namespace GameDevToi.ThirdLib.Core
{
    /// <summary>
    /// Base class cho Ad Format - có thể extend
    /// </summary>
    [Serializable]
    public class AdFormatDefinition
    {
        public string id;
        public string displayName;
        public string description;

        public AdFormatDefinition(string id, string displayName, string description = "")
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
        }

        public override string ToString() => displayName;
        public override bool Equals(object obj)
        {
            if (obj is AdFormatDefinition other)
                return id == other.id;
            if (obj is string str)
                return id == str;
            return false;
        }
        public override int GetHashCode() => id?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Built-in Ad Formats (trong DLL)
    /// </summary>
    public static class AdFormats
    {
        public static readonly AdFormatDefinition Banner = new AdFormatDefinition("banner", "Banner", "Standard banner ads");
        public static readonly AdFormatDefinition Mrec = new AdFormatDefinition("mrec", "MREC", "Medium Rectangle ads (300x250)");
        public static readonly AdFormatDefinition Interstitial = new AdFormatDefinition("interstitial", "Interstitial", "Full-screen interstitial ads");
        public static readonly AdFormatDefinition Rewarded = new AdFormatDefinition("rewarded", "Rewarded", "Rewarded video ads");
        public static readonly AdFormatDefinition AppOpen = new AdFormatDefinition("appopen", "App Open", "App open ads");
        public static readonly AdFormatDefinition RewardedInterstitial = new AdFormatDefinition("rewarded_interstitial", "Rewarded Interstitial", "Rewarded interstitial ads");
        public static readonly AdFormatDefinition Native = new AdFormatDefinition("native", "Native", "Native ads");
    }

    /// <summary>
    /// Base class cho Ad Network - có thể extend
    /// </summary>
    [Serializable]
    public class AdNetworkDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public Color editorColor = Color.white;

        public AdNetworkDefinition(string id, string displayName, string description = "", Color? color = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.editorColor = color ?? Color.white;
        }

        public override string ToString() => displayName;
        public override bool Equals(object obj)
        {
            if (obj is AdNetworkDefinition other)
                return id == other.id;
            if (obj is string str)
                return id == str;
            return false;
        }
        public override int GetHashCode() => id?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Built-in Ad Networks (trong DLL)
    /// </summary>
    public static class AdNetworks
    {
        public static readonly AdNetworkDefinition AdMob = new AdNetworkDefinition("admob", "AdMob", "Google AdMob", new Color(0.2f, 0.6f, 1f));
        public static readonly AdNetworkDefinition AppLovin = new AdNetworkDefinition("applovin", "AppLovin", "AppLovin MAX", new Color(0.2f, 0.8f, 0.4f));
        public static readonly AdNetworkDefinition IronSource = new AdNetworkDefinition("ironsource", "IronSource", "IronSource mediation", new Color(1f, 0.4f, 0.2f));
        public static readonly AdNetworkDefinition UnityAds = new AdNetworkDefinition("unityads", "Unity Ads", "Unity Ads", new Color(0.1f, 0.1f, 0.1f));
    }
}
