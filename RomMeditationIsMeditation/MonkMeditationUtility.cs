using Verse;
using TorannMagic;
using UnityEngine;

namespace RomMeditationIsMeditation
{
    public static class MonkMeditationUtility
    {
        public const float ChiTarget = 0.8f;
        public const float RestTarget = 0.8f;
        public const float RestLowerThreshold = 0.3f;

        public static bool IsMonk(Pawn pawn) =>
                pawn?.story?.traits?.HasTrait(TorannMagicDefOf.TM_Monk) == true;

        public static Hediff GetChiHediff(Pawn pawn) =>
                pawn.health.hediffSet.GetFirstHediffOfDef(TorannMagicDefOf.TM_ChiHD);

        public static float GetChiTarget(Pawn pawn)
        {
            Hediff chiHD = GetChiHediff(pawn);
            if (chiHD == null) return ChiTarget;
            HediffComp_ChiTarget comp = (chiHD as HediffWithComps)?.GetComp<HediffComp_ChiTarget>();
            return comp?.TargetValue ?? ChiTarget;
        }

        public static void SetChiTarget(Pawn pawn, float value)
        {
            Hediff chiHD = GetChiHediff(pawn);
            if (chiHD == null) return;
            HediffComp_ChiTarget comp = (chiHD as HediffWithComps)?.GetComp<HediffComp_ChiTarget>();
            if (comp != null) comp.TargetValue = Mathf.Clamp(value, 0f, 1f);
        }
    }

    public class HediffCompProperties_ChiTarget : HediffCompProperties
    {
        public HediffCompProperties_ChiTarget()
        {
            compClass = typeof(HediffComp_ChiTarget);
        }
    }

    public class HediffComp_ChiTarget : HediffComp
    {
        public float TargetValue = MonkMeditationUtility.ChiTarget;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref TargetValue, "chiTargetValue", MonkMeditationUtility.ChiTarget);
        }
    }
}