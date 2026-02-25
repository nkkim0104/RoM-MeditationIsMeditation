using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RomMeditationIsMeditation.Patches
{
    [HarmonyPatch(typeof(JobGiver_GetRest), "GetPriority")]
    public static class Patch_JobGiver_GetRest_GetPriority
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn pawn, ref float __result)
        {
            if (!MonkMeditationUtility.IsMonk(pawn)) return;
            if (pawn.needs?.rest == null) return;
            if (pawn.needs.rest.CurLevel < MonkMeditationUtility.RestLowerThreshold) return;
            
            if (pawn.needs.rest.CurLevel >= MonkMeditationUtility.RestTarget) return;

            TimeAssignmentDef assignment = pawn.timetable?.CurrentAssignment ?? TimeAssignmentDefOf.Anything;

            float priority = 0f;
            if (assignment == TimeAssignmentDefOf.Anything)       priority = 5.0f;
            else if (assignment == TimeAssignmentDefOf.Joy)       priority = 7.5f;
            else if (assignment == TimeAssignmentDefOf.Meditate)  priority = 9f;

            if (priority <= 0f) return;

            __result = Mathf.Max(__result, priority);
        }
    }

    [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
    public static class Patch_JobGiver_GetRest_TryGiveJob
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result == null) return;
            if (!MonkMeditationUtility.IsMonk(pawn)) return;

            if (pawn.timetable?.CurrentAssignment == TimeAssignmentDefOf.Sleep) return;
            if (pawn.needs?.rest != null && pawn.needs.rest.CurLevel < MonkMeditationUtility.RestLowerThreshold) return;

            Job meditationJob = MeditationUtility.GetMeditationJob(pawn, false);
            if (meditationJob == null) return;

            __result = meditationJob;
        }
    }
}