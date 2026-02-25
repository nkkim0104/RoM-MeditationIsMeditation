using HarmonyLib;
using Verse;
using System;
using System.Reflection;

namespace RomMeditationIsMeditation 
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            Harmony harmony = new Harmony("com.nkkim0104.rommeditationismeditation");
            harmony.PatchAll();

            Type gizmoType = AccessTools.TypeByName("TorannMagic.Gizmo_EnergyStatus");

            MethodInfo original = AccessTools.Method(gizmoType, "GizmoOnGUI");
            MethodInfo postfix = AccessTools.Method(
                typeof(Patches.Patch_Gizmo_EnergyStatus_GizmoOnGUI), "Postfix");

            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        }
    }
}