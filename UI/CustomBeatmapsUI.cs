
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CustomBeatmaps.CustomPackages;
using CustomBeatmaps.UISystem;
using CustomBeatmaps.Util;
using UnityEngine;

using static CustomBeatmaps.Util.ArcadeHelper;

namespace CustomBeatmaps.UI
{
    public static class CustomBeatmapsUI
    {
        private static PackageTabUIOnline OnlinePackageList = new PackageTabUIOnline(CustomBeatmaps.LocalServerPackages);
        private static PackageTabUILocal LocalPackageList = new PackageTabUILocal(CustomBeatmaps.LocalUserPackages);
        private static PackageTabUIEditor EditorPackageList = new PackageTabUIEditor(CustomBeatmaps.LocalEditorPackages);
        //private static PackageTabUISubmission SubmissionPackageList = new PackageTabUISubmission(CustomBeatmaps.LocalSubmissionPackages);

        public static void PreviewCurrentAudio()
        {
            // idk this is a bad way of doing this
            try
            {
                switch (CustomBeatmaps.Memory.SelectedTab)
                {
                    case Tab.Online:
                        try
                        {
                            OnlinePackageList.PreviewAudio();
                        }
                        catch
                        {
                            LocalPackageList.PreviewAudio();
                        }
                        break;
                    case Tab.Local:
                        LocalPackageList.PreviewAudio();
                        break;
                    case Tab.Edit:
                        EditorPackageList.PreviewAudio();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception) { }
        }

        public static void Render()
        {
            /*
             *  State:
             *      - Which tab we're on
             *  
             *  UI:
             *      List:
             *      - Tab Picker
             *      - User/Online Info
             *      - Depending on current tab, local/server/submission/OSU!
             *      - Assist info on the bottom
             */

            GUIHelper.SetDefaultStyles();

            // Remember our tab state statically for convenience (ShaiUI might have been right here, maybe I didn't even need react lmfao)
            (Tab tab, Action<Tab> setTab) = (CustomBeatmaps.Memory.SelectedTab, val => {
                CustomBeatmaps.Memory.SelectedTab = val;

                if (CustomBeatmapsUIBehaviour.Opened)
                    PreviewCurrentAudio();
            }
            );

            try
            {
                switch (tab)
                {
                    case Tab.Online:
                        try
                        {
                            OnlinePackageList.Render(() => RenderListTop(tab, setTab));
                        }
                        catch
                        {
                            LocalPackageList.Render(() => RenderListTop(tab, setTab));
                        }
                        break;
                    case Tab.Local:
                        LocalPackageList.Render(() => RenderListTop(tab, setTab));
                        break;
                    case Tab.Edit:
                        EditorPackageList.Render(() => RenderListTop(tab, setTab));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                // NOTE: there are a fuck ton of errors, but somehow it still works
                
                CustomBeatmaps.Log.LogError(e);
                //setTab.Invoke(Tab.Local);
            }



            // Keyboard Shortcut: Cycle tabs
            if (GUIHelper.CanDoInput() && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                int count = Enum.GetValues(typeof(Tab)).Length;
                int ind = (int)tab;
                if (Input.GetKeyDown(KeyCode.PageUp))
                {
                    ind -= 1;
                    if (ind < 0)
                        ind = count - 1;
                    // Net for Bugs
                    BGM.StopSongPreview();
                    setTab((Tab)ind);
                }
                else if (Input.GetKeyDown(KeyCode.PageDown))
                {
                    ind += 1;
                    ind %= count;
                    // Net for Bugs
                    BGM.StopSongPreview();
                    setTab((Tab)ind);
                }
            }
        }

        private static void RenderListTop(Tab tab, Action<Tab> onSetTab)
        {
            GUILayout.BeginHorizontal();
            EnumTooltipPickerUI.Render(tab, onSetTab, tabName =>
            {
                switch (tabName)
                {
                    case Tab.Online:
                        return "Online";
                    case Tab.Local:
                        return "Local";
                    case Tab.Edit:
                        return "Editor";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(tabName), tabName, null);
                }
            });
            GUILayout.EndHorizontal();
            UserOnlineInfoBarUI.Render();
        }
    }
}