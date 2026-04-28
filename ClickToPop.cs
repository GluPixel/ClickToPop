global using BTD_Mod_Helper.Extensions;
using MelonLoader;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.ModOptions;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Simulation.Bloons;
using UnityEngine;
using ClickToPop;

[assembly: MelonInfo(typeof(ClickToPop.ClickToPop), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace ClickToPop;

public class ClickToPop : BloonsTD6Mod
{
    private static readonly ModSettingBool ClickPopEnabled = new(true)
    {
        displayName = "Click To Pop Enabled",
        description = "Left-click near a bloon to pop it."
    };

    private static readonly ModSettingDouble ClickRadius = new(140)
    {
        displayName = "Click Radius (pixels)",
        description = "How close in screen pixels your click needs to be to a bloon.",
        min = 10,
        max = 600
    };

    private static readonly ModSettingInt CashReward = new(1)
    {
        displayName = "Cash Per Pop",
        description = "How much cash you earn per bloon popped. Set to 0 for no reward.",
        min = 0,
        max = 10000
    };

    public override void OnApplicationStart()
    {
        ModHelper.Msg<ClickToPop>("Click To Pop v2.1.5 loaded!");
    }

    private static Camera? GetUsableCamera()
        => Camera.main ?? Object.FindObjectOfType<Camera>();

    internal static void TryPopClickedBloon(InGame inGame)
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (!ClickPopEnabled) return;
        if (inGame == null || !inGame.IsInGame()) return;

        var camera = GetUsableCamera();
        if (camera == null) return;

        var mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        float pixelRadius = (float)(double)ClickRadius;

        Bloon? closestBloon = null;
        float bestDistance = float.MaxValue;

        foreach (var bloon in inGame.GetBloons())
        {
            if (bloon == null) continue;

            try
            {
                var node = bloon.GetUnityDisplayNode();
                if (node == null) continue;

                var screenPos = camera.WorldToScreenPoint(node.transform.position);
                float dist = Vector2.Distance(mousePos, new Vector2(screenPos.x, screenPos.y));

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    closestBloon = bloon;
                }
            }
            catch { }
        }

        if (closestBloon == null || bestDistance > pixelRadius) return;

        try
        {
            closestBloon.Damage(
                1f,
                null!,
                false,
                false,
                true,
                null!,
                default,
                default,
                false,
                true,
                true,
                false,
                new Il2CppSystem.Nullable<int>()
            );

            // Give cash reward if set
            int reward = (int)CashReward;
            if (reward > 0)
            {
                inGame.AddCash(reward);
            }
        }
        catch (System.Exception e)
        {
            ModHelper.Warning<ClickToPop>($"Pop failed: {e.Message}");
        }
    }
}

[HarmonyPatch(typeof(InGame), nameof(InGame.Update))]
internal static class InGame_Update_Patch
{
    [HarmonyPostfix]
    internal static void Postfix(InGame __instance)
    {
        ClickToPop.TryPopClickedBloon(__instance);
    }
}