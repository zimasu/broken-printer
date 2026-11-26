// ExpressiveDupes -- Brings Duplicants' unique selection-card poses and expressions into colony gameplay.
// Copyright (C) 2026  Zidane Syukri (zimasu)
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
using Database;
using HarmonyLib;
using Klei.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ExpressiveDupes
{
    public class SavedCardLook
    {
        public string IdleAnim;
        public string EyesId;
        public string MouthId;
    }

    public static class CardAnimStore
    {
        private static readonly Dictionary<string, SavedCardLook> SavedLooks = new Dictionary<string, SavedCardLook>();
        private static bool loaded;

        private static string SaveFilePath
        {
            get
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(dir ?? ".", "saved_card_looks.txt");
            }
        }

        public static void LoadIfNeeded()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (!File.Exists(SaveFilePath)) return;
                foreach (string line in File.ReadAllLines(SaveFilePath))
                {
                    int sep = line.IndexOf('=');
                    if (sep <= 0) continue;

                    string key = line.Substring(0, sep).Trim();
                    string[] parts = line.Substring(sep + 1).Split('|');
                    if (parts.Length >= 3)
                    {
                        SavedLooks[key] = new SavedCardLook
                        {
                            IdleAnim = parts[0].Trim(),
                            EyesId = parts[1].Trim(),
                            MouthId = parts[2].Trim()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ExpressiveDupes] Failed loading saved_card_looks.txt: {ex}");
            }
        }

        public static void Save(string key, SavedCardLook look)
        {
            if (string.IsNullOrEmpty(key) || look == null) return;
            LoadIfNeeded();
            SavedLooks[key] = look;
            try
            {
                var lines = SavedLooks.Select(kv => $"{kv.Key}={kv.Value.IdleAnim}|{kv.Value.EyesId}|{kv.Value.MouthId}");
                File.WriteAllLines(SaveFilePath, lines);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ExpressiveDupes] Failed saving saved_card_looks.txt: {ex}");
            }
        }

        public static SavedCardLook GetLook(MinionIdentity minion)
        {
            if (minion == null) return null;
            LoadIfNeeded();

            if (!string.IsNullOrEmpty(minion.name) && SavedLooks.TryGetValue(minion.name, out var byName))
                return byName;

            string persId = minion.personalityResourceId.IsValid ? HashCache.Get().Get(minion.personalityResourceId) : null;
            if (!string.IsNullOrEmpty(persId) && SavedLooks.TryGetValue(persId, out var byPers))
                return byPers;

            return null;
        }
    }

    public static class DupeStyleHelper
    {
        private static readonly HashSet<int> ActiveCardIdles = new HashSet<int>();

        public static void ApplyCardAccessories(MinionIdentity minion, Accessorizer accessorizer)
        {
            if (minion == null || accessorizer == null) return;

            var slots = Db.Get().AccessorySlots;
            var look = CardAnimStore.GetLook(minion);
            var personality = Db.Get().Personalities.TryGet(minion.personalityResourceId);

            string eyesId = !string.IsNullOrEmpty(look?.EyesId) ? look.EyesId : (personality != null ? $"eyes_{personality.eyes:D3}" : null);
            string mouthId = !string.IsNullOrEmpty(look?.MouthId) ? look.MouthId : (personality != null ? $"mouth_{personality.mouth:D3}" : null);

            if (!string.IsNullOrEmpty(eyesId))
            {
                var cardEyes = slots.Eyes.Lookup(eyesId);
                if (cardEyes != null && accessorizer.GetAccessory(slots.Eyes) != cardEyes)
                    accessorizer.AddAccessory(cardEyes);
            }

            if (!string.IsNullOrEmpty(mouthId))
            {
                var cardMouth = slots.Mouth.Lookup(mouthId);
                if (cardMouth != null && accessorizer.GetAccessory(slots.Mouth) != cardMouth)
                    accessorizer.AddAccessory(cardMouth);
            }
        }

        public static bool IsWaterCardAnim(string animName)
        {
            if (string.IsNullOrEmpty(animName)) return true;
            string s = animName.ToLowerInvariant();
            return s.Contains("swim") || s.Contains("diver") || s.Contains("water") || s.Contains("float") || s.Contains("aqua");
        }

        public static bool IsNegativeMood(Expression exp)
        {
            if (exp == null) return false;
            var all = Db.Get().Expressions;
            return exp != all.Neutral &&
                   exp != all.Happy &&
                   exp != all.Relief &&
                   exp != all.Productive &&
                   exp != all.Determined &&
                   exp != all.Sticker &&
                   exp != all.Balloon &&
                   exp != all.Sparkle &&
                   exp != all.Music &&
                   exp != all.Tickled &&
                   exp != all.BionicJoy;
        }

        public static void SyncCardIdleOverride(MinionIdentity minion, bool enableCardIdle)
        {
            if (minion == null) return;
            var kac = minion.GetComponent(typeof(KBatchedAnimController)) as KBatchedAnimController;
            if (kac == null) return;

            string animName = CardAnimStore.GetLook(minion)?.IdleAnim;
            if (string.IsNullOrEmpty(animName) || IsWaterCardAnim(animName)) return;

            var idleFile = Assets.GetAnim(animName);
            if (idleFile == null) return;

            int id = minion.GetInstanceID();
            if (enableCardIdle && !ActiveCardIdles.Contains(id))
            {
                kac.AddAnimOverrides(idleFile, 1f);
                ActiveCardIdles.Add(id);
            }
            else if (!enableCardIdle && ActiveCardIdles.Contains(id))
            {
                kac.RemoveAnimOverrides(idleFile);
                ActiveCardIdles.Remove(id);
            }
        }

        // Standard resting face is Happy (smiling), while stoic/chill personalities keep Neutral (._.)
        public static Expression GetDefaultPersonalityExpression(MinionIdentity minion)
        {
            var exps = Db.Get().Expressions;
            if (minion == null) return exps.Happy;

            var look = CardAnimStore.GetLook(minion);
            string idle = (look?.IdleAnim ?? "").ToLowerInvariant();

            if (idle.Contains("coaster") || idle.Contains("breathershallow") || idle.Contains("breatherdeep"))
                return exps.Neutral;

            return exps.Happy;
        }
    }

    // 1. Capture card demeanor & Eyes/Mouth accessories from the selection card
    [HarmonyPatch(typeof(CharacterContainer), "SetAnimator")]
    public static class CharacterContainer_SetAnimator_Patch
    {
        private static readonly FieldInfo StatsField = typeof(CharacterContainer).GetField("stats", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo IdleAnimField = typeof(CharacterContainer).GetField("idle_anim", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo AnimCtrlField = typeof(CharacterContainer).GetField("animController", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Postfix(CharacterContainer __instance)
        {
            try
            {
                var stats = StatsField?.GetValue(__instance) as MinionStartingStats;
                var idleAnim = IdleAnimField?.GetValue(__instance) as KAnimFile;
                var animCtrl = AnimCtrlField?.GetValue(__instance) as KBatchedAnimController;
                if (stats == null || idleAnim == null) return;

                string eyesId = "";
                string mouthId = "";
                var acc = animCtrl != null ? animCtrl.GetComponent(typeof(Accessorizer)) as Accessorizer : null;
                if (acc != null)
                {
                    var slots = Db.Get().AccessorySlots;
                    eyesId = acc.GetAccessory(slots.Eyes)?.Id ?? "";
                    mouthId = acc.GetAccessory(slots.Mouth)?.Id ?? "";
                }

                var look = new SavedCardLook
                {
                    IdleAnim = idleAnim.name,
                    EyesId = eyesId,
                    MouthId = mouthId
                };

                if (!string.IsNullOrEmpty(stats.Name))
                    CardAnimStore.Save(stats.Name, look);

                if (stats.personality != null && !string.IsNullOrEmpty(stats.personality.Id))
                    CardAnimStore.Save(stats.personality.Id, look);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ExpressiveDupes] Card capture warning: {ex}");
            }
        }
    }

    // 2. Apply card Eyes/Mouth accessories, ground-safe card idle animation, and personality walk cycle on spawn
    [HarmonyPatch(typeof(MinionIdentity), "OnSpawn")]
    public static class MinionIdentity_OnSpawn_Patch
    {
        public static void Postfix(MinionIdentity __instance)
        {
            try
            {
                DupeStyleHelper.ApplyCardAccessories(
                    __instance,
                    __instance.GetComponent(typeof(Accessorizer)) as Accessorizer
                );

                // Start with the selection-card idle animation active
                DupeStyleHelper.SyncCardIdleOverride(__instance, true);
                var kac = __instance.GetComponent(typeof(KBatchedAnimController)) as KBatchedAnimController;
                if (kac == null) return;
                string animName = CardAnimStore.GetLook(__instance)?.IdleAnim;
                var traits = __instance.GetComponent(typeof(Traits)) as Traits;
                var ids = traits != null ? traits.GetTraitIds() : new List<string>();
                string walkKanimName = PickWalkKanim(animName, ids);
                if (!string.IsNullOrEmpty(walkKanimName))
                {
                    var walkFile = Assets.GetAnim(walkKanimName);
                    if (walkFile != null)
                        kac.AddAnimOverrides(walkFile, 1f);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ExpressiveDupes] OnSpawn warning: {ex}");
            }
        }
        private static string PickWalkKanim(string cardIdleAnim, List<string> traitIds)
        {
            string lowerIdle = (cardIdleAnim ?? "").ToLowerInvariant();
            if (lowerIdle.Contains("breathershallow") || lowerIdle.Contains("breatherdeep") ||
            traitIds.Contains("NoodleArms") || traitIds.Contains("SlowLearner") || traitIds.Contains("Anemic") || traitIds.Contains("Uncultured"))
            {
                return "anim_loco_walk_exhausted_kanim";
            }
            if (lowerIdle.Contains("fastfeet") || lowerIdle.Contains("keener") ||
            traitIds.Contains("Athletic") || traitIds.Contains("QuickLearner") || traitIds.Contains("EarlyBird") || traitIds.Contains("Twinkletoes"))
            {
                return "anim_loco_walk_happy_kanim";
            }
            return null;
        }
    }
    // 3. Toggle card idle animation off during negative moods (reverting to vanilla animation + sad/angry face) and back on when happy/neutral!
    [HarmonyPatch(typeof(FaceGraph), "UpdateFace")]
    public static class FaceGraph_UpdateFace_Patch
    {
        public static void Postfix(FaceGraph __instance)
        {
            var minion = __instance.GetComponent(typeof(MinionIdentity)) as MinionIdentity;
            if (minion == null) return;
            bool negativeMood = DupeStyleHelper.IsNegativeMood(__instance.currentExpression);
            DupeStyleHelper.SyncCardIdleOverride(minion, !negativeMood);
        }
    }
    // 4. Keep card Eyes/Mouth equipped; when resting (Neutral), use Happy smile (or ._. for stoic dupes), and let vanilla take over for any sad/stressed/angry state!
    [HarmonyPatch(typeof(FaceGraph), nameof(FaceGraph.ApplyShape), new Type[] { })]
    public static class FaceGraph_ApplyShape_Patch
    {
        public static void Prefix(FaceGraph __instance)
        {
            DupeStyleHelper.ApplyCardAccessories(
            __instance.GetComponent(typeof(MinionIdentity)) as MinionIdentity,
            __instance.GetComponent(typeof(Accessorizer)) as Accessorizer
            );
        }
    }
    [HarmonyPatch(typeof(FaceGraph), "ApplyShape", typeof(KAnim.Build.Symbol), typeof(KBatchedAnimController), typeof(KAnimFile), typeof(KAnimHashedString), typeof(bool))]
    public static class FaceGraph_ApplyShapeInternal_Patch
    {
        private static readonly PropertyInfo CurrentExpProp = typeof(FaceGraph).GetProperty("currentExpression", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        public static void Prefix(FaceGraph __instance, ref Expression __state)
        {
            var minion = __instance.GetComponent(typeof(MinionIdentity)) as MinionIdentity;
            if (minion == null) return;
            var current = __instance.currentExpression;
            if (current == null || current == Db.Get().Expressions.Neutral)
            {
                var targetExp = DupeStyleHelper.GetDefaultPersonalityExpression(minion);
                if (targetExp != null && targetExp != current)
                {
                    __state = current;
                    CurrentExpProp?.SetValue(__instance, targetExp);
                }
            }
        }
        public static void Postfix(FaceGraph __instance, Expression __state)
        {
            if (__state != null)
            {
                CurrentExpProp?.SetValue(__instance, __state);
            }
        }
    }
}

