﻿using System.Linq;
using ModKit;
using UnityEngine;
using UnityModManagerNet;
using static SolastaCommunityExpansion.Viewers.Displays.BlueprintDisplay;
using static SolastaCommunityExpansion.Viewers.Displays.CreditsDisplay;
using static SolastaCommunityExpansion.Viewers.Displays.DiagnosticsDisplay;
using static SolastaCommunityExpansion.Viewers.Displays.GameServicesDisplay;
using static SolastaCommunityExpansion.Viewers.Displays.Level20HelpDisplay;
using static SolastaCommunityExpansion.Viewers.Displays.PatchesDisplay;

namespace SolastaCommunityExpansion.Viewers
{
    public class HelpAndCreditsViewer : IMenuSelectablePage
    {
        public string Name => "Modding, Help & Credits";

        public int Priority => 999;


        private static int selectedPane;

        private static readonly NamedAction[] actions =
        {
            new NamedAction("Help & Credits", DisplayHelpAndCredits),
            new NamedAction("Blueprints", DisplayBlueprints),
            new NamedAction("Services", DisplayGameServices),
            new NamedAction("Diagnostics & Patches", DisplayDiagnosticsAndPatches),
        };

        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            UI.Label("Welcome to Solasta Community Expansion".yellow().bold());
            UI.Div();

            if (Main.Enabled)
            {
                var titles = actions.Select((a, i) => i == selectedPane ? a.name.orange().bold() : a.name).ToArray();

                UI.SelectionGrid(ref selectedPane, titles, titles.Length, UI.ExpandWidth(true));
                GUILayout.BeginVertical("box");
                actions[selectedPane].action();
                GUILayout.EndVertical();
            }
        }

        public static void DisplayHelpAndCredits()
        {
            DisplayLevel20Help();
            DisplayCredits();
        }

        public static void DisplayDiagnosticsAndPatches()
        {
            DisplayModdingTools();
            DisplayDumpDescription();
            UI.Label("");
            UI.Div();
            UI.Label("");
            DisplayPatches();
        }
    }
}
