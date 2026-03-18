using Challenges;
using HarmonyLib;
using Rhythm;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomBeatmaps.Patches
{
    public static class GameplayPatch
    {
        /// <summary>
        /// Modified version of <see href="https://github.com/Zachava96/PinkNoteBugFix/blob/main/PinkNoteBugFix.cs"/> <br/>
        /// Credit to @zachava
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(BaseKillableNote))]
        [HarmonyPatch("IsSwungAt")]
        static bool BaseKillableNotePatches(ref BaseKillableNote __instance, ref bool __result)
        {
            // ignore on non-custom beatmaps
            if (JeffBezosController.rhythmProgression is ArcadeProgression && !((ArcadeProgression)JeffBezosController.rhythmProgression).isCustomChart)
                return true;

            if (!__instance.PlayerCanHit() || !__instance.PlayerSwung() || !__instance.upcoming)
            {
                __result = false;
                return false;
            }
            if (__instance.height != Height.Mid && __instance.height != Height.Top && __instance.height != Height.Low)
            {
                __result = true;
                return false;
            }

            float? topTime = null;
            float? midTime = null;
            float? lowTime = null;
            foreach (KeyValuePair<Lane, BaseKillableNote> upcomingNote in __instance.controller.upcoming)
            {
                float hitTime = upcomingNote.Value.hitTime;

                if (upcomingNote.Value is DoubleNote doubleNote
                    && Traverse.Create(doubleNote).Field("worstAttack").GetValue<AttackInfo>() != null
                    && Traverse.Create(doubleNote).Field("worstAttack.position").GetValue<float>() == __instance.songPosition)
                    //&& doubleNote.worstAttack != null
                    //&& doubleNote.worstAttack.position == __instance.songPosition)
                {
                    hitTime = doubleNote.prevTime;
                }
                else if (upcomingNote.Value is SpamNote spamNote
                        && Traverse.Create(spamNote).Field("attack").GetValue<AttackInfo>() != null
                        && Traverse.Create(spamNote).Field("attack.position").GetValue<float>() == __instance.songPosition)
                        //&& spamNote.attack != null
                        //&& spamNote.attack.position == __instance.songPosition)
                {
                    //hitTime = spamNote.prevTime;
                    hitTime = Traverse.Create(spamNote).Field("prevTime").GetValue<float>();
                }
                else if (upcomingNote.Value is HoldNote holdNote
                        && Traverse.Create(holdNote).Field("attack").GetValue<AttackInfo>() != null
                        && Traverse.Create(holdNote).Field("attack.position").GetValue<float>() == __instance.songPosition)
                //&& holdNote.attack != null
                //&& holdNote.attack.position == __instance.songPosition)
                {
                    hitTime = Traverse.Create(holdNote).Field("prevTime").GetValue<float>();
                    //hitTime = holdNote.prevTime;
                }

                switch (upcomingNote.Key.height)
                {
                    case Height.Low:
                        if (lowTime == null || hitTime < lowTime)
                        {
                            lowTime = hitTime;
                        }
                        break;
                    case Height.Mid:
                        if (midTime == null || hitTime < midTime)
                        {
                            midTime = hitTime;
                        }
                        break;
                    case Height.Top:
                        if (topTime == null || hitTime < topTime)
                        {
                            topTime = hitTime;
                        }
                        break;
                }
            }
            switch (__instance.height)
            {
                case Height.Low:
                    if (__instance.player.input.anyLow && (midTime == null || midTime >= __instance.hitTime))
                    {
                        __result = true;
                        return false;
                    }
                    break;
                case Height.Mid:
                    if (__instance.player.input.anyTop && (topTime == null || topTime >= __instance.hitTime))
                    {
                        __result = true;
                        return false;
                    }
                    if (__instance.player.input.anyLow && (lowTime == null || lowTime >= __instance.hitTime))
                    {
                        __result = true;
                        return false;
                    }
                    break;
                case Height.Top:
                    if (__instance.player.input.anyTop && (midTime == null || midTime >= __instance.hitTime))
                    {
                        __result = true;
                        return false;
                    }
                    break;
            }
            __result = false;
            return false;
        }
    }
}
