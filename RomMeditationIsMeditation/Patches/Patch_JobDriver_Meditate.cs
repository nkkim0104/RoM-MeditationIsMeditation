using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using HarmonyLib;
using Verse;
using Verse.AI;
using TorannMagic;

namespace RomMeditationIsMeditation.Patches
{
    [HarmonyPatch(typeof(JobDriver_Meditate), "MeditationTick")]
    public static class Patch_JobDriver_Meditate_MeditationTick
    {
        [HarmonyPostfix]
        public static void Postfix(JobDriver_Meditate __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn == null || !pawn.Spawned) return;
            
            // 1. Check if pawn is a Monk
            if (!MonkMeditationUtility.IsMonk(pawn)) return;

            // 2. Visual effect (every 12 ticks)
            if (Find.TickManager.TicksGame % 12 == 0)
            {
                ThrowChiMote(pawn);
            }

            // 3. Monk meditation logic (every 60 ticks = 1 second)
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                ApplyMonkMeditationLogic(pawn);
            }
        }

        private static void ThrowChiMote(Pawn pawn)
        {
            Vector3 drawPos = pawn.DrawPos;
            
            drawPos.x += Rand.Range(-0.5f, 0.5f);
            drawPos.z += Rand.Range(-0.4f, 0.6f);

            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            float angle = (pawn.DrawPos - drawPos).ToAngleFlat();
            TM_MoteMaker.ThrowGenericMote(TorannMagicDefOf.Mote_Chi_Grayscale, drawPos, pawn.Map, 
                Rand.Range(0.1f, 0.22f), 0.2f, 0.3f, 0.2f, 30, 
                0.2f * (drawPos - pawn.DrawPos).MagnitudeHorizontal(), angle, angle);
        }

        private static void ApplyMonkMeditationLogic(Pawn pawn)
        {
            CompAbilityUserMight compMight = pawn.GetCompAbilityUserMight();
            if (compMight == null) return;

            Hediff chiHD = MonkMeditationUtility.GetChiHediff(pawn);
            if (chiHD == null) return;

            var skills = compMight.MightData.MightPowerSkill_Meditate;
            int effVal = skills.FirstOrDefault(x => x.label == "TM_Meditate_eff")?.level ?? 0;
            int pwrVal = skills.FirstOrDefault(x => x.label == "TM_Meditate_pwr")?.level ?? 0;
            int verVal = skills.FirstOrDefault(x => x.label == "TM_Meditate_ver")?.level ?? 0;

            int chiMultiplier = (chiHD.Severity > 1f) ? 5 : 1;

            var isPawnInjured = TM_Calc.IsPawnInjured(pawn, 0f);
            var afflictions = TM_Calc.GetPawnAfflictions(pawn);
            var addictions = TM_Calc.GetPawnAddictions(pawn);

            // 1. Heal injuries
            if (isPawnInjured)
            {
                float healAmt = Rand.Range(0.25f, 0.4f) * chiMultiplier * (1f + 0.1f * pwrVal);
                TM_Action.DoAction_HealPawn(pawn, pawn, 1, healAmt);

                chiHD.Severity -= 1f;
                compMight.MightUserXP += 2 * chiMultiplier;
            }
            // 2. Heal afflictions
            else if (!afflictions.NullOrEmpty())
            {
                Hediff hediff = afflictions.RandomElement();
                hediff.Severity -= 0.001f * chiMultiplier * (1f + 0.1f * pwrVal);

                var disappears = hediff.TryGetComp<HediffComp_Disappears>();
                if (disappears != null)
                {
                    int currentTicks = Traverse.Create(disappears).Field("ticksToDisappear").GetValue<int>();
                    int reduction = Mathf.RoundToInt(10000f * chiMultiplier * (1f + 0.1f * pwrVal));
                    Traverse.Create(disappears).Field("ticksToDisappear").SetValue(currentTicks - reduction);
                }

                chiHD.Severity -= 1f;
                compMight.MightUserXP += 2 * chiMultiplier;
            }
            // 3. Heal addictions
            else if (!addictions.NullOrEmpty())
            {
                Hediff addiction = addictions.RandomElement();
                addiction.Severity -= 0.0015f * chiMultiplier * (1f + 0.1f * pwrVal);

                chiHD.Severity -= 1f;
                compMight.MightUserXP += 2 * chiMultiplier;
            }
            // 4. Manage mood break risk
            else if (BreakRiskAlertUtility.PawnsAtRiskMinor.Contains(pawn) || 
                     BreakRiskAlertUtility.PawnsAtRiskMajor.Contains(pawn) || 
                     BreakRiskAlertUtility.PawnsAtRiskExtreme.Contains(pawn))
            {
                if (pawn.needs.mood != null) pawn.needs.mood.CurLevel += 0.004f * chiMultiplier * (1f + 0.1f * verVal);

                chiHD.Severity -= 1f;
                compMight.MightUserXP += 2 * chiMultiplier;
            }
            // 5. Charge Chi and recover needs
            else
            {
                // Charge Chi
                chiHD.Severity += Rand.Range(0.2f, 0.3f) * (1f + effVal * 0.1f);
                
                // Recover needs
                if (pawn.needs.rest != null) pawn.needs.rest.CurLevel += 0.003f * (1f + 0.1f * verVal);
                if (pawn.needs.joy != null) pawn.needs.joy.CurLevel += 0.004f * (1f + 0.1f * verVal);
                if (pawn.needs.mood != null) pawn.needs.mood.CurLevel += 0.001f * (1f + 0.1f * verVal);
            }
        }
    }
    
    [HarmonyPatch(typeof(JobDriver_Meditate), "MakeNewToils")]
    public static class Patch_JobDriver_Meditate_MakeNewToils
    {
        [HarmonyPostfix]
        public static IEnumerable<Toil> Postfix(IEnumerable<Toil> __result, JobDriver_Meditate __instance)
        {
            foreach (Toil toil in __result)
            {
                if (MonkMeditationUtility.IsMonk(__instance.pawn))
                {
                    toil.AddPreTickAction(delegate
                    {
                        Pawn pawn = __instance.pawn;
                        Hediff chiHD = MonkMeditationUtility.GetChiHediff(pawn);
                        if (chiHD == null) return;

                        if (!pawn.IsHashIntervalTick(4000)) return;

                        bool chiSatisfied = chiHD.Severity >= Mathf.Max(MonkMeditationUtility.GetChiTarget(pawn) + 0.05f, 0.99f);
                        bool restSatisfied = pawn.needs?.rest == null || pawn.needs.rest.CurLevel >= 1.0f;

                        if (chiSatisfied || restSatisfied)
                        {
                            pawn.jobs.CheckForJobOverride(0f, true);
                        }
                    });
                }

                yield return toil;
            }
        }
    }
}