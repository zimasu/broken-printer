using HarmonyLib;
using STRINGS;
using System;

namespace DuplicantLifecycles
{
    public static class DuplicantLifecycleStrings
    {
        public static StatusItem AgingYouth;
        public static StatusItem AgingMiddle;
        public static StatusItem AgingElderly;
        public static StatusItem AgingDying;
        public static StatusItem Immortal;

        public static LocString AgingID = (LocString)"ImperfectCopy.CorruptedDNA";
        public static LocString AgingNAME = (LocString)"Imperfect Copy";
        public static LocString AgingDESC = (LocString)"Printed with corrupted DNA—this clone will degrade and expire after 14 to 28 cycles!";

        public static LocString ImmortalID = (LocString)"ImperfectCopy.StabilizedDNA";
        public static LocString ImmortalNAME = (LocString)"Stabilized Core";
        public static LocString ImmortalDESC = (LocString)"Free from biological DNA decay—this unit will never expire from cellular breakdown.";

        public static string AgingYouthKey = "STRINGS.DUPLICANTS.STATUSITEMS.AGINGYOUTH";
        public static LocString AgingYouthName = "Fresh Copy";
        public static LocString AgingYouthTooltip = (LocString)("This clone's DNA is still holding together: \n {0}");

        public static string AgingMiddleKey = "STRINGS.DUPLICANTS.STATUSITEMS.AGINGMIDDLE";
        public static LocString AgingMiddleName = "Unstable DNA";
        public static LocString AgingMiddleTooltip = (LocString)("Minor cellular degradation is setting in: \n {0}");

        public static string AgingElderlyKey = "STRINGS.DUPLICANTS.STATUSITEMS.AGINGELDERLY";
        public static LocString AgingElderlyName = "Degrading Copy";
        public static LocString AgingElderlyTooltip = (LocString)("Severe DNA fragmentation—nearing expiration: \n {0}");

        public static string AgingDyingKey = "STRINGS.DUPLICANTS.STATUSITEMS.AGINGDYING";
        public static LocString AgingDyingName = "Terminal DNA Collapse";
        public static LocString AgingDyingTooltip = (LocString)("Critical cellular failure—this clone will expire very soon: \n {0}");

        public static string ImmortalKey = "STRINGS.DUPLICANTS.STATUSITEMS.IMMORTAL";
        public static LocString ImmortalName = "Immortal / Bionic";
        public static LocString ImmortalTooltip = (LocString)("Immune to DNA degradation: \n {0}");

        public static string DeathMessage = "{Target} succumbed to terminal DNA collapse.";

        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public class GeneratedBuildings_LoadGeneratedBuildings_Patch
        {
            public static void Prefix()
            {
                SetupStringsFromConfig();
                AddStrings();
            }
        }

        private static string GetSign(float value)
        {
            if (value == 0)
                return "";
            else if (value > 0)
                return "+";
            else
                return "-";
        }

        public static void SetupStringsFromConfig()
        {
            string description;
            float custom_base;

            if (DuplicantLifecycleConfigChecker.UsePercentage)
            {
                description = "• All " + UI.PRE_KEYWORD + "Attributes" + UI.PST_KEYWORD + " {0}<b>{1}%</b>";
                custom_base = 100f;
            }
            else
            {
                description = "• All " + UI.PRE_KEYWORD + "Attributes" + UI.PST_KEYWORD + " are {0}<b>{1}</b>";
                custom_base = DuplicantLifecycleConfigChecker.CustomBase;
            }

            DuplicantLifecycleStrings.AgingYouthTooltip = string.Format(
                DuplicantLifecycleStrings.AgingYouthTooltip,
                string.Format(
                    description,
                    GetSign(DuplicantLifecycleConfigChecker.YouthAttributeMultiplier),
                    Math.Abs(custom_base * DuplicantLifecycleConfigChecker.YouthAttributeMultiplier).ToString()
                ));

            DuplicantLifecycleStrings.AgingMiddleTooltip = string.Format(
                DuplicantLifecycleStrings.AgingMiddleTooltip,
                string.Format(
                    description,
                    GetSign(DuplicantLifecycleConfigChecker.MiddleAttributeMultiplier),
                    Math.Abs(custom_base * DuplicantLifecycleConfigChecker.MiddleAttributeMultiplier).ToString()
                ));

            DuplicantLifecycleStrings.AgingElderlyTooltip = string.Format(
                DuplicantLifecycleStrings.AgingElderlyTooltip,
                string.Format(
                    description,
                    GetSign(DuplicantLifecycleConfigChecker.ElderlyAttributeMultiplier),
                    Math.Abs(custom_base * DuplicantLifecycleConfigChecker.ElderlyAttributeMultiplier).ToString()
                ));

            DuplicantLifecycleStrings.AgingDyingTooltip = string.Format(
                DuplicantLifecycleStrings.AgingDyingTooltip,
                string.Format(
                    description,
                    GetSign(DuplicantLifecycleConfigChecker.DyingAttributeMultiplier),
                    Math.Abs(custom_base * DuplicantLifecycleConfigChecker.DyingAttributeMultiplier).ToString()
                ));

            DuplicantLifecycleStrings.ImmortalTooltip = string.Format(
                DuplicantLifecycleStrings.ImmortalTooltip,
                string.Format(
                    description,
                    GetSign(DuplicantLifecycleConfigChecker.ImmortalAttributeMultiplier),
                    Math.Abs(custom_base * DuplicantLifecycleConfigChecker.ImmortalAttributeMultiplier).ToString()
                ));
        }

        private static void AddStrings()
        {
            Strings.Add(DuplicantLifecycleStrings.AgingYouthKey + ".NAME", DuplicantLifecycleStrings.AgingYouthName);
            Strings.Add(DuplicantLifecycleStrings.AgingYouthKey + ".TOOLTIP", DuplicantLifecycleStrings.AgingYouthTooltip);
            Strings.Add(DuplicantLifecycleStrings.AgingMiddleKey + ".NAME", DuplicantLifecycleStrings.AgingMiddleName);
            Strings.Add(DuplicantLifecycleStrings.AgingMiddleKey + ".TOOLTIP", DuplicantLifecycleStrings.AgingMiddleTooltip);
            Strings.Add(DuplicantLifecycleStrings.AgingElderlyKey + ".NAME", DuplicantLifecycleStrings.AgingElderlyName);
            Strings.Add(DuplicantLifecycleStrings.AgingElderlyKey + ".TOOLTIP", DuplicantLifecycleStrings.AgingElderlyTooltip);
            Strings.Add(DuplicantLifecycleStrings.AgingDyingKey + ".NAME", DuplicantLifecycleStrings.AgingDyingName);
            Strings.Add(DuplicantLifecycleStrings.AgingDyingKey + ".TOOLTIP", DuplicantLifecycleStrings.AgingDyingTooltip);
            Strings.Add(DuplicantLifecycleStrings.ImmortalKey + ".NAME", DuplicantLifecycleStrings.ImmortalName);
            Strings.Add(DuplicantLifecycleStrings.ImmortalKey + ".TOOLTIP", DuplicantLifecycleStrings.ImmortalTooltip);

            DuplicantLifecycleStrings.AgingYouth =
                (StatusItem)Traverse.Create(Db.Get().DuplicantStatusItems).Method("CreateStatusItem", new object[] {
                    "AgingYouth",
                    "DUPLICANTS",
                    string.Empty,
                    StatusItem.IconType.Info,
                    NotificationType.Neutral,
                    false,
                    OverlayModes.None.ID,
                    true,
                    2 }
                ).GetValue();

            DuplicantLifecycleStrings.AgingMiddle =
                (StatusItem)Traverse.Create(Db.Get().DuplicantStatusItems).Method("CreateStatusItem", new object[] {
                    "AgingMiddle",
                    "DUPLICANTS",
                    string.Empty,
                    StatusItem.IconType.Info,
                    NotificationType.Neutral,
                    false,
                    OverlayModes.None.ID,
                    true,
                    2 }
                ).GetValue();

            DuplicantLifecycleStrings.AgingElderly =
                (StatusItem)Traverse.Create(Db.Get().DuplicantStatusItems).Method("CreateStatusItem", new object[] {
                    "AgingElderly",
                    "DUPLICANTS",
                    string.Empty,
                    StatusItem.IconType.Exclamation,
                    NotificationType.BadMinor,
                    false,
                    OverlayModes.None.ID,
                    true,
                    2 }
                ).GetValue();

            DuplicantLifecycleStrings.AgingDying =
                (StatusItem)Traverse.Create(Db.Get().DuplicantStatusItems).Method("CreateStatusItem", new object[] {
                    "AgingDying",
                    "DUPLICANTS",
                    string.Empty,
                    StatusItem.IconType.Exclamation,
                    NotificationType.DuplicantThreatening,
                    false,
                    OverlayModes.None.ID,
                    true,
                    2 }
                ).GetValue();

            DuplicantLifecycleStrings.Immortal =
                (StatusItem)Traverse.Create(Db.Get().DuplicantStatusItems).Method("CreateStatusItem", new object[] {
                    "Immortal",
                    "DUPLICANTS",
                    string.Empty,
                    StatusItem.IconType.Info,
                    NotificationType.Good,
                    false,
                    OverlayModes.None.ID,
                    true,
                    2 }
                ).GetValue();
        }
    }
}
