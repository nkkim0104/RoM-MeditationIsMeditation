using HarmonyLib;
using TorannMagic;
using UnityEngine;
using Verse;

namespace RomMeditationIsMeditation.Patches
{
    public static class Patch_Gizmo_EnergyStatus_GizmoOnGUI
    {
        private static bool draggingBar;

        private static readonly Texture2D ChiBarTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(1f, 0.75f, 0f));
        private static readonly Texture2D ChiBarHighlightTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(1f, 0.85f, 0.3f));
        private static readonly Texture2D EmptyBarTex =
            SolidColorMaterials.NewSolidColorTexture(Color.clear);
        private static readonly Texture2D ChiTargetTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.74f, 0.97f, 0.8f));

        public static void Postfix(object __instance, Vector2 topLeft, float maxWidth)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null || !MonkMeditationUtility.IsMonk(pawn)) return;
            if (!pawn.IsColonistPlayerControlled) return;

            Hediff chiHD = MonkMeditationUtility.GetChiHediff(pawn);
            if (chiHD == null) return;

            // Calculate barCount and num — replicates Gizmo_EnergyStatus logic
            CompAbilityUserMagic compMagic = pawn.GetCompAbilityUserMagic();
            CompAbilityUserMight compMight = pawn.GetCompAbilityUserMight();

            bool isMage      = compMagic.IsMagicUser && !pawn.story.traits.HasTrait(TorannMagicDefOf.Faceless);
            bool isFighter   = compMight.IsMightUser;
            bool isPsionic   = pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_PsionicHD"));
            bool isBloodMage = pawn.health.hediffSet.HasHediff(HediffDef.Named("TM_BloodHD"));
            bool isBrightmage = pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_LightCapacitanceHD);
            bool isMonk      = pawn.health.hediffSet.HasHediff(TorannMagicDefOf.TM_ChiHD);
            bool isSpirit    = TM_Calc.IsPossessedByOrIsSpirit(pawn);

            bool isDeathKnight = false;
            for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
            {
                if (pawn.health.hediffSet.hediffs[i].def.defName.Contains("TM_HateHD"))
                {
                    isDeathKnight = true;
                    break;
                }
            }

            float barCount = 0f;
            if (isFighter)    barCount++;
            if (isMage)       barCount++;
            if (isPsionic)    barCount++;
            if (isDeathKnight) barCount++;
            if (isBloodMage)  barCount++;
            if (isSpirit)     barCount++;
            if (isBrightmage) barCount++;
            if (isMonk)       barCount++;

            float initialShift     = barCount == 1f ? 15f : 5f;
            float barSpacing       = barCount >= 2f ? 2f : 0f;
            float contractionAmount = barCount >= 2f ? Mathf.Max(6f - barCount, 0f) : 6f;
            float barHeight        = (75f - 2f * contractionAmount - 2f * initialShift - barSpacing * (barCount - 1f)) / barCount;

            // Chi bar y-offset — matches isMonk block order in Gizmo_EnergyStatus
            float num = initialShift;
            if (isPsionic)     num += barHeight + barSpacing;
            if (isDeathKnight) num += barHeight + barSpacing;

            Rect overRect = new Rect(topLeft.x + 2f, topLeft.y, 100f, 75f);
            Rect rect     = overRect.AtZero().ContractedBy(contractionAmount);
            rect.height   = barHeight;

            Rect chiBarRect = new Rect(
                overRect.x + rect.x,
                overRect.y + rect.y + num,
                rect.width,
                barHeight
            );

            Find.WindowStack.ImmediateWindow(984699, overRect, WindowLayer.GameUI, delegate
            {
                Rect localRect = new Rect(
                    chiBarRect.x - overRect.x,
                    chiBarRect.y - overRect.y,
                    chiBarRect.width,
                    chiBarRect.height
                );

                float currentChi    = Mathf.Clamp01(chiHD.Severity / 100f);
                float targetValue   = MonkMeditationUtility.GetChiTarget(pawn);
                float lastTargetValue = targetValue;

                Widgets.DraggableBar(localRect, ChiBarTex, ChiBarHighlightTex, EmptyBarTex, ChiTargetTex,
                    ref draggingBar, currentChi, ref targetValue, null, 16, 0f, 1f);

                if (lastTargetValue != targetValue)
                    MonkMeditationUtility.SetChiTarget(pawn, targetValue);

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(localRect, chiHD.Severity.ToString("F0") + " / 100");
                Text.Anchor = TextAnchor.UpperLeft;

                TooltipHandler.TipRegion(localRect, () =>
                    "Chi: " + chiHD.Severity.ToString("F0") + " / 100\n" +
                    "Target: " + (MonkMeditationUtility.GetChiTarget(pawn) * 100f).ToString("F0"),
                    Gen.HashCombineInt(pawn.GetHashCode(), 77731));

            }, doBackground: false, absorbInputAroundWindow: false, shadowAlpha: 0f);
        }
    }
}