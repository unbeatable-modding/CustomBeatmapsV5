using CustomBeatmaps.Util;
using FMOD;
using FMOD.Studio;
using HarmonyLib;
using Rhythm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.UI.ContentSizeFitter;

namespace CustomBeatmaps.Patches
{
    public static class EditorModePatch
    {
        public static RhythmController Controller => RhythmController.Instance;

        [HarmonyPatch(typeof(RhythmController), nameof(RhythmController.InitializeAndPlay))]
        [HarmonyPrefix]
        static bool NoCountDown(ref bool countdownOn)
        {
            countdownOn = _enableCountdown;
            return true;
        }

        [HarmonyPatch(typeof(RhythmController), nameof(RhythmController.InitializeAndPlay))]
        [HarmonyPostfix]
        static void GetController(RhythmController __instance)
        {
            if (_editMode && _enableStartTime)
                ForcedSeek(__instance.songTracker.instance, (int)_startTime);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="seekTime"></param>
        /// <param name="buffer">Time before notes spawn</param>
        static void ForcedSeek(EventInstance instance, int desiredTime, int buffer = 1000)
        {
            // Sanitize start time
            if (desiredTime < 0) desiredTime = 0;
            if (desiredTime > Controller.notes.Last().time) desiredTime = (int)Controller.notes.Last().time - 200;

            // Load timing points
            TimingPointInfo countdownTiming = null;
            List<TimingPointInfo> timings = new List<TimingPointInfo>(Controller.beatmap.timingPoints);
            timings.RemoveAll((TimingPointInfo t) => !t.uninherited);
            Controller.timingPointIndex = 0;
            Controller.measureBarIndex = 0;

            int index = 0;
            // while index in range and current point is less than desired time and next point is also less than desired time, or current is the last point
            while (index < timings.Count && timings[index].time <= desiredTime && index != timings.Count - 1 && timings[index + 1].time <= desiredTime)
            {
                index++;
            }
            countdownTiming = timings[index];

            // Set timing
            // desiredTime = when the first notes should reach the player
            // seekTime = time to start the audio, time to start the countdown
            int seekTime = desiredTime - buffer;// - Mathf.RoundToInt(8 * countdownTiming.beatLength);
            if (_enableCountdown)
                seekTime = seekTime - Mathf.RoundToInt(8 * countdownTiming.beatLength);

            

            // Countdown
            if (_enableCountdown)
            {
                // Calculate additional delay for song to be on beat
                float measureProgress = Mathf.Repeat(seekTime - countdownTiming.time, countdownTiming.beatLength) / countdownTiming.beatLength;
                int additionalTime = Mathf.RoundToInt((1 - measureProgress) * countdownTiming.beatLength);
                if (measureProgress * countdownTiming.beatLength < 20) additionalTime = 0;

                Controller.songTracker.AddCountdown(500 + 8f * countdownTiming.beatLength + additionalTime, desiredTime + additionalTime, countdownTiming.beatLength);
                Controller.song.countdownBeatLength = countdownTiming.beatLength;
            }

            // Remove notes that are before the start time
            while (Controller.notes.Count > 0 && Controller.notes.Peek().time <= desiredTime + FileStorage.options.rhythmTrackerPositionOffset)
            {
                Controller.notes.Dequeue();
            }

            // Process flips
            FlipInfo flipInfo = null;
            while (Controller.flips.Count > 0 && Controller.flips.Peek().time <= seekTime)
            {
                flipInfo = Controller.flips.Dequeue();

                if (flipInfo.toggleCenter) Controller.cameraIsCentered = !Controller.cameraIsCentered;
                else Controller.player.side = Controller.player.side.GetOpposite();
            }

            if (flipInfo != null)
            {
                if (Controller.cameraIsCentered) Controller.cameraObject.SetTargetPoint(Controller.centerCameraTargetPoint);
                else if (Controller.player.side == Side.Right) Controller.cameraObject.SetTargetPoint(Controller.rightCameraTargetPoint);
                else if (Controller.player.side == Side.Left) Controller.cameraObject.SetTargetPoint(Controller.leftCameraTargetPoint);

                Controller.player.ChangeSide(Controller.player.side);

                bool toggleCenter = flipInfo.toggleCenter;
                Controller.indicatingFlip = false;
            }

            // FMOD magic
            Task.Run(() =>
            {
                if (instance.isValid() && Controller._play)
                {
                    //ControllerNewTime(Controller.notes, seekTime);
                    ChannelGroup cg;
                    while (instance.getChannelGroup(out cg) != RESULT.OK) { }
                    //if (instance.getChannelGroup(out ChannelGroup cg) == RESULT.OK)
                    {
                        cg.getNumGroups(out _);
                        cg.getGroup(0, out ChannelGroup subGroup);
                        subGroup.getNumChannels(out int numChan);
                        while (numChan <= 0)
                        {
                            cg.getNumGroups(out _);
                            cg.getGroup(0, out subGroup);
                            subGroup.getNumChannels(out numChan);
                        }
                        //if (numChan > 0)
                        {
                            subGroup.getChannel(0, out Channel chan);
                            chan.setPosition((uint)seekTime, TIMEUNIT.MS);
                            instance.setTimelinePosition(seekTime);
                        }

                    }

                }
            });
        }

        public static bool _editMode = false;
        private static bool _enableCountdown;
        private static bool _enableStartTime;
        private static float _startTime;

        public static void SetEditMode(bool editMode, bool enableCountdown = true, bool enableStartTime = false, float startTime = 0f)
        {
            ScheduleHelper.SafeLog($"EDIT MODE {editMode}");
            _editMode = editMode;
            _enableCountdown = enableCountdown;
            _enableStartTime = enableStartTime;
            _startTime = startTime;
        }

        //[HarmonyPatch(typeof(Rhythm.RhythmController), "Update")]
        //[HarmonyPrefix]
        private static void RhythmControllerReloadUpdatePre(Rhythm.RhythmController __instance)
        {

            if (Input.GetKeyDown(KeyCode.R))
            {
                ForcedSeek(__instance.songTracker.instance, 10000);
                Controller.player.ResetTwinTimer();
                Controller.player.RhythmUpdate();
                //Controller.Awake();
            }
        }

    }
}
