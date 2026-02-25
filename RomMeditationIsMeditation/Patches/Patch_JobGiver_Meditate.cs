using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RomMeditationIsMeditation.Patches
{
    [HarmonyPatch(typeof(JobGiver_Meditate), "GetPriority")]
    public static class Patch_JobGiver_Meditate_GetPriority
    {

        [HarmonyPostfix]
        public static void Postfix(Pawn pawn, ref float __result)
        {
            if (!MonkMeditationUtility.IsMonk(pawn)) return;

            Hediff chiHD = MonkMeditationUtility.GetChiHediff(pawn);
            if (chiHD == null) return;

            Log.Message("chiHD.Severity : " + chiHD.Severity + ", chiTarget : " + MonkMeditationUtility.GetChiTarget(pawn));

            if (chiHD.Severity / 100 >=  Mathf.Min(MonkMeditationUtility.GetChiTarget(pawn), 0.95f)) return;

            float priority = 0f;
            if (pawn.CurrentBed() == null)
            {
                if (pawn.timetable?.CurrentAssignment == TimeAssignmentDefOf.Anything) priority = 7.1f;
            }
            else
            {
                if (pawn.health.hediffSet.PainTotal <= 0.3f) priority = 6f;
            }

            __result = Mathf.Max(__result, priority);
        }
    }
}