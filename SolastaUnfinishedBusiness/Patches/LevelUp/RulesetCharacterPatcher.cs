﻿using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;

namespace SolastaUnfinishedBusiness.Patches.LevelUp;

//PATCH: ensures auto prepared spells from subclass are considered on level up
[HarmonyPatch(typeof(RulesetCharacter), "ComputeAutopreparedSpells")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class RulesetCharacter_ComputeAutopreparedSpells
{
    internal static bool Prefix([NotNull] RulesetCharacter __instance, [NotNull] RulesetSpellRepertoire spellRepertoire)
    {
        //BEGIN PATCH
        var spellcastingClass = spellRepertoire.SpellCastingClass;
        
        if (spellRepertoire.SpellCastingSubclass != null)
        {
            spellcastingClass = GetClassForSubclass(spellRepertoire.SpellCastingSubclass);
        }
        //END PATCH

        spellRepertoire.AutoPreparedSpells.Clear();
        __instance.EnumerateFeaturesToBrowse<FeatureDefinitionAutoPreparedSpells>(__instance.FeaturesToBrowse);

        foreach (var featureDefinition in __instance.FeaturesToBrowse)
        {
            var autoPreparedSpells = featureDefinition as FeatureDefinitionAutoPreparedSpells;

            if (autoPreparedSpells!.SpellcastingClass != spellcastingClass)
            {
                continue;
            }

            foreach (var preparedSpellsGroup in autoPreparedSpells.AutoPreparedSpellsGroups
                         .Where(preparedSpellsGroup => preparedSpellsGroup.ClassLevel <=
                                                       GetSpellcastingLevel(__instance, spellRepertoire)))
            {
                spellRepertoire.AutoPreparedSpells.AddRange(preparedSpellsGroup.SpellsList);
                spellRepertoire.AutoPreparedTag = autoPreparedSpells.AutoPreparedTag;
            }
        }

        // This includes all the logic for the base function and a little extra, so skip it.
        return false;
    }

    private static int GetSpellcastingLevel(
        [NotNull] RulesetEntity character,
        RulesetSpellRepertoire spellRepertoire)
    {
        if (character is not RulesetCharacterHero hero)
        {
            return character.GetAttribute(AttributeDefinitions.CharacterLevel).BaseValue;
        }

        if (spellRepertoire.SpellCastingClass != null)
        {
            return hero.ClassesAndLevels[spellRepertoire.SpellCastingClass];
        }

        return spellRepertoire.SpellCastingSubclass != null
            ? hero.ComputeSubclassLevel(spellRepertoire.SpellCastingSubclass)
            : character.GetAttribute(AttributeDefinitions.CharacterLevel).BaseValue;
    }

    [CanBeNull]
    private static CharacterClassDefinition GetClassForSubclass(BaseDefinition subclass)
    {
        return DatabaseRepository.GetDatabase<CharacterClassDefinition>().FirstOrDefault(klass =>
        {
            return klass.FeatureUnlocks.Any(unlock =>
            {
                if (unlock.FeatureDefinition is FeatureDefinitionSubclassChoice subclassChoice)
                {
                    return subclassChoice.Subclasses.Contains(subclass.Name);
                }

                return false;
            });
        });
    }
}
