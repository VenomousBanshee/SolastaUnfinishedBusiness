﻿using System;
using System.Collections.Generic;
using System.Linq;
using SolastaCommunityExpansion.Builders;
using SolastaCommunityExpansion.Builders.Features;
using SolastaCommunityExpansion.CustomFeatureDefinitions;
using SolastaModApi;
using SolastaModApi.Extensions;
using SolastaModApi.Infrastructure;
using static SolastaModApi.DatabaseHelper;
using static SolastaModApi.DatabaseHelper.CharacterSubclassDefinitions;

namespace SolastaCommunityExpansion.Subclasses.Barbarian
{
    internal class PathOfTheLight : AbstractSubclass
    {
        private static readonly Guid SubclassNamespace = new("c2067110-5086-45c0-b0c2-4c140599605c");
        private const string IlluminatedConditionName = "PathOfTheLightIlluminatedCondition";
        private const string IlluminatingStrikeName = "PathOfTheLightIlluminatingStrike";
        private const string IlluminatingBurstName = "PathOfTheLightIlluminatingBurst";

        private static readonly List<ConditionDefinition> InvisibleConditions =
            new()
            {
                ConditionDefinitions.ConditionInvisibleBase,
                ConditionDefinitions.ConditionInvisible,
                ConditionDefinitions.ConditionInvisibleGreater
            };

        private readonly CharacterSubclassDefinition _subclass;

        private static readonly Dictionary<int, int> LightsProtectionAmountHealedByClassLevel = new()
        {
            { 6, 3 },
            { 7, 3 },
            { 8, 4 },
            { 9, 4 },
            { 10, 5 },
            { 11, 5 },
            { 12, 6 },
            { 13, 6 },
            { 14, 7 },
            { 15, 7 },
            { 16, 8 },
            { 17, 8 },
            { 18, 9 },
            { 19, 9 },
            { 20, 10 }
        };

        internal override FeatureDefinitionSubclassChoice GetSubclassChoiceList()
        {
            return FeatureDefinitionSubclassChoices.SubclassChoiceBarbarianPrimalPath;
        }

        internal override CharacterSubclassDefinition GetSubclass()
        {
            return _subclass;
        }

        internal PathOfTheLight()
        {
            ConditionDefinition illuminatedCondition = CreateIlluminatedCondition();

            _subclass = CharacterSubclassDefinitionBuilder.Create("PathOfTheLight", SubclassNamespace)
                .SetGuiPresentation("BarbarianPathOfTheLight", Category.Subclass, DomainSun.GuiPresentation.SpriteReference)
                .AddFeatureAtLevel(CreateIlluminatingStrike(illuminatedCondition), 3)
                .AddFeatureAtLevel(CreatePierceTheDarkness(), 3)
                .AddFeatureAtLevel(CreateLightsProtection(), 6)
                .AddFeatureAtLevel(CreateEyesOfTruth(), 10)
                .AddFeatureAtLevel(CreateIlluminatingStrikeImprovement(), 10)
                .AddFeatureAtLevel(CreateIlluminatingBurst(illuminatedCondition), 14)
                .AddToDB();
        }

        private static string CreateNamespacedGuid(string featureName)
        {
            return GuidHelper.Create(SubclassNamespace, featureName).ToString();
        }

        private static FeatureDefinition CreateIlluminatingStrike(ConditionDefinition illuminatedCondition)
        {
            return FeatureDefinitionBuilder<FeatureDefinitionFeatureSet>
                .Create("PathOfTheLightIlluminatingStrikeFeatureSet", SubclassNamespace)
                .SetGuiPresentation("BarbarianPathOfTheLightIlluminatingStrike", Category.Subclass)
                .Configure<FeatureDefinitionBuilder<FeatureDefinitionFeatureSet>>(
                    featureSetDefinition =>
                    {
                        featureSetDefinition
                            .SetEnumerateInDescription(false)
                            .SetMode(FeatureDefinitionFeatureSet.FeatureSetMode.Union)
                            .SetUniqueChoices(false);

                        var illuminatingStrikeInitiatorBuilder = new IlluminatingStrikeInitiatorBuilder(
                            "PathOfTheLightIlluminatingStrikeInitiator",
                            CreateNamespacedGuid("PathOfTheLightIlluminatingStrikeInitiator"),
                            "Feature/&NoContentTitle",
                            "Feature/&NoContentTitle",
                            illuminatedCondition);

                        featureSetDefinition.FeatureSet.Add(illuminatingStrikeInitiatorBuilder.AddToDB());
                    })
                .AddToDB();
        }

        private static FeatureDefinition CreateIlluminatingStrikeImprovement()
        {
            // Dummy feature to show in UI

            return FeatureDefinitionBuilder<FeatureDefinition>
                .Create("PathOfTheLightIlluminatingStrikeImprovement", SubclassNamespace)
                .SetGuiPresentation("BarbarianPathOfTheLightIlluminatingStrikeImprovement", Category.Subclass)
                .AddToDB();
        }

        private static FeatureDefinition CreatePierceTheDarkness()
        {
            return FeatureDefinitionBuilder<FeatureDefinitionFeatureSet>
                .Create("PathOfTheLightPierceTheDarkness", SubclassNamespace)
                .SetGuiPresentation("BarbarianPathOfTheLightPierceTheDarkness", Category.Subclass)
                .Configure<FeatureDefinitionBuilder<FeatureDefinitionFeatureSet>>(
                    featureSetDefinition =>
                    {
                        featureSetDefinition
                            .SetEnumerateInDescription(false)
                            .SetMode(FeatureDefinitionFeatureSet.FeatureSetMode.Union)
                            .SetUniqueChoices(false);

                        featureSetDefinition.FeatureSet.Add(FeatureDefinitionSenses.SenseSuperiorDarkvision);
                    })
                .AddToDB();
        }

        private static FeatureDefinition CreateLightsProtection()
        {
            return FeatureDefinitionBuilder<FeatureDefinitionFeatureSet>
                .Create("PathOfTheLightLightsProtection", SubclassNamespace)
                .SetGuiPresentation("BarbarianPathOfTheLightLightsProtection", Category.Subclass)
                .Configure<FeatureDefinitionBuilder<FeatureDefinitionFeatureSet>>(
                    definition =>
                    {
                        definition
                            .SetEnumerateInDescription(false)
                            .SetMode(FeatureDefinitionFeatureSet.FeatureSetMode.Union)
                            .SetUniqueChoices(false);

                        var conditionalOpportunityAttackImmunity = FeatureDefinitionBuilder<FeatureDefinitionOpportunityAttackImmunityIfAttackerHasCondition>
                            .Create("PathOfTheLightLightsProtectionOpportunityAttackImmunity", SubclassNamespace)
                            .SetGuiPresentationNoContent()
                            .Configure<FeatureDefinitionBuilder<FeatureDefinitionOpportunityAttackImmunityIfAttackerHasCondition>>(
                                definition => definition.ConditionName = IlluminatedConditionName)
                            .AddToDB();

                        definition.FeatureSet.Add(conditionalOpportunityAttackImmunity);
                    })
                .AddToDB();
        }

        private static void ApplyLightsProtectionHealing(ulong sourceGuid)
        {
            if (RulesetEntity.GetEntity<RulesetCharacter>(sourceGuid) is not RulesetCharacterHero conditionSource || conditionSource.IsDead)
            {
                return;
            }

            if (!conditionSource.ClassesAndLevels.TryGetValue(CharacterClassDefinitions.Barbarian, out int levelsInClass))
            {
                // Character doesn't have levels in class
                return;
            }

            if (!LightsProtectionAmountHealedByClassLevel.TryGetValue(levelsInClass, out int amountHealed))
            {
                // Character doesn't heal at the current level
                return;
            }

            if (amountHealed > 0)
            {
                conditionSource.ReceiveHealing(amountHealed, notify: true, sourceGuid);
            }
        }

        private static FeatureDefinition CreateEyesOfTruth()
        {
            var seeingInvisibleCondition = ConditionDefinitionBuilder.Build(
                "PathOfTheLightEyesOfTruthSeeingInvisible",
                CreateNamespacedGuid("PathOfTheLightEyesOfTruthSeeingInvisible"),
                definition =>
                {
                    var gpb = new GuiPresentationBuilder(
                        "Subclass/&BarbarianPathOfTheLightSeeingInvisibleConditionTitle",
                        "Subclass/&BarbarianPathOfTheLightSeeingInvisibleConditionDescription");

                    gpb.SetSpriteReference(ConditionDefinitions.ConditionSeeInvisibility.GuiPresentation.SpriteReference);

                    definition
                        .SetGuiPresentation(gpb.Build())
                        .SetDurationType(RuleDefinitions.DurationType.Permanent)
                        .SetConditionType(RuleDefinitions.ConditionType.Beneficial)
                        .SetSilentWhenAdded(true)
                        .SetSilentWhenRemoved(true);

                    definition.Features.Add(FeatureDefinitionSenses.SenseSeeInvisible16);
                });

            var seeInvisibleEffectBuilder = new EffectDescriptionBuilder();

            var seeInvisibleConditionForm = new EffectForm
            {
                FormType = EffectForm.EffectFormType.Condition,
                ConditionForm = new ConditionForm
                {
                    Operation = ConditionForm.ConditionOperation.Add,
                    ConditionDefinition = seeingInvisibleCondition
                }
            };

            seeInvisibleEffectBuilder
                .SetDurationData(RuleDefinitions.DurationType.Permanent, 1, RuleDefinitions.TurnOccurenceType.StartOfTurn)
                .SetTargetingData(RuleDefinitions.Side.Ally, RuleDefinitions.RangeType.Self, 1, RuleDefinitions.TargetType.Self, 1, 0, ActionDefinitions.ItemSelectionType.None)
                .AddEffectForm(seeInvisibleConditionForm);

            var seeInvisiblePower = FeatureDefinitionBuilder<FeatureDefinitionPower>
                .Create("PathOfTheLightEyesOfTruthPower", SubclassNamespace)
                .SetGuiPresentation("BarbarianPathOfTheLightEyesOfTruth", Category.Subclass, SpellDefinitions.SeeInvisibility.GuiPresentation.SpriteReference)
                .Configure<FeatureDefinitionBuilder<FeatureDefinitionPower>>(
                    definition =>
                    {
                        definition
                            .SetActivationTime(RuleDefinitions.ActivationTime.Permanent)
                            .SetEffectDescription(seeInvisibleEffectBuilder.Build())
                            .SetRechargeRate(RuleDefinitions.RechargeRate.AtWill)
                            .SetShowCasting(false);
                    })
                .AddToDB();

            return FeatureDefinitionBuilder<FeatureDefinitionFeatureSet>
                .Create("PathOfTheLightEyesOfTruth", SubclassNamespace)
                .SetGuiPresentation("BarbarianPathOfTheLightEyesOfTruth", Category.Subclass)
                .Configure<FeatureDefinitionBuilder<FeatureDefinitionFeatureSet>>(
                    definition =>
                    {
                        definition
                            .SetEnumerateInDescription(false)
                            .SetMode(FeatureDefinitionFeatureSet.FeatureSetMode.Union)
                            .SetUniqueChoices(false);

                        definition.FeatureSet.Add(seeInvisiblePower);
                    })
                .AddToDB();
        }

        private static FeatureDefinition CreateIlluminatingBurst(ConditionDefinition illuminatedCondition)
        {
            return FeatureDefinitionBuilder<FeatureDefinitionFeatureSet>
                .Create("PathOfTheLightIlluminatingBurstFeatureSet", SubclassNamespace)
                .SetGuiPresentation("BarbarianPathOfTheLightIlluminatingBurst", Category.Subclass)
                .Configure<FeatureDefinitionBuilder<FeatureDefinitionFeatureSet>>(
                    definition =>
                    {
                        definition
                            .SetEnumerateInDescription(false)
                            .SetMode(FeatureDefinitionFeatureSet.FeatureSetMode.Union)
                            .SetUniqueChoices(false);

                        ConditionDefinition illuminatingBurstSuppressedCondition = CreateIlluminatingBurstSuppressedCondition();

                        var illuminatingBurstBuilder = new IlluminatingBurstInitiatorBuilder(
                            "PathOfTheLightIlluminatingBurstInitiator",
                            CreateNamespacedGuid("PathOfTheLightIlluminatingBurstInitiator"),
                            "Feature/&NoContentTitle",
                            "Feature/&NoContentTitle",
                            illuminatingBurstSuppressedCondition);

                        definition.FeatureSet.Add(illuminatingBurstBuilder.AddToDB());

                        var illuminatingBurstPowerBuilder = new IlluminatingBurstBuilder(
                            IlluminatingBurstName,
                            CreateNamespacedGuid(IlluminatingBurstName),
                            "Subclass/&BarbarianPathOfTheLightIlluminatingBurstPowerTitle",
                            "Subclass/&BarbarianPathOfTheLightIlluminatingBurstPowerDescription",
                            illuminatedCondition,
                            illuminatingBurstSuppressedCondition);

                        definition.FeatureSet.Add(illuminatingBurstPowerBuilder.AddToDB());

                        definition.FeatureSet.Add(CreateIlluminatingBurstSuppressor(illuminatingBurstSuppressedCondition));
                    })
                .AddToDB();
        }

        private static FeatureDefinition CreateIlluminatingBurstSuppressor(ConditionDefinition illuminatingBurstSuppressedCondition)
        {
            return FeatureDefinitionBuilder<FeatureDefinitionPower>
                .Create("PathOfTheLightIlluminatingBurstSuppressor", SubclassNamespace)
                .SetGuiPresentationNoContent(true)
                .Configure<FeatureDefinitionBuilder<FeatureDefinitionPower>>(
                    definition =>
                    {
                        var suppressIlluminatingBurst = new EffectForm
                        {
                            FormType = EffectForm.EffectFormType.Condition,
                            ConditionForm = new ConditionForm
                            {
                                Operation = ConditionForm.ConditionOperation.Add,
                                ConditionDefinition = illuminatingBurstSuppressedCondition
                            }
                        };

                        var effectDescriptionBuilder = new EffectDescriptionBuilder();

                        effectDescriptionBuilder
                            .SetDurationData(RuleDefinitions.DurationType.Permanent, 1, RuleDefinitions.TurnOccurenceType.StartOfTurn)
                            .SetTargetingData(RuleDefinitions.Side.Ally, RuleDefinitions.RangeType.Self, 1, RuleDefinitions.TargetType.Self, 1, 0, ActionDefinitions.ItemSelectionType.None)
                            .SetRecurrentEffect(RuleDefinitions.RecurrentEffect.OnActivation | RuleDefinitions.RecurrentEffect.OnTurnStart)
                            .AddEffectForm(suppressIlluminatingBurst);

                        definition
                            .SetActivationTime(RuleDefinitions.ActivationTime.Permanent)
                            .SetEffectDescription(effectDescriptionBuilder.Build())
                            .SetRechargeRate(RuleDefinitions.RechargeRate.AtWill);
                    })
                .AddToDB();
        }

        private static ConditionDefinition CreateIlluminatedCondition()
        {
            return ConditionDefinitionBuilder<IlluminatedConditionDefinition>.Build(
                IlluminatedConditionName,
                CreateNamespacedGuid(IlluminatedConditionName),
                definition =>
                {
                    definition
                        .SetGuiPresentation("BarbarianPathOfTheLightIlluminatedCondition",
                            Category.Subclass, ConditionDefinitions.ConditionBranded.GuiPresentation.SpriteReference)
                        .SetSpecialDuration(true)
                        .SetDurationType(RuleDefinitions.DurationType.Irrelevant)
                        .SetConditionType(RuleDefinitions.ConditionType.Detrimental)
                        .SetAllowMultipleInstances(true)
                        .SetSilentWhenAdded(true)
                        .SetSilentWhenRemoved(false);

                    definition.Features.Add(CreateDisadvantageAgainstNonSource());
                    definition.Features.Add(CreatePreventInvisibility());
                });
        }

        private static FeatureDefinitionAttackDisadvantageAgainstNonSource CreateDisadvantageAgainstNonSource()
        {
            return FeatureDefinitionBuilder<FeatureDefinitionAttackDisadvantageAgainstNonSource>
                .Create("PathOfTheLightIlluminatedDisadvantage", SubclassNamespace)
                .SetGuiPresentation("Feature/&NoContentTitle", "Subclass/&BarbarianPathOfTheLightIlluminatedDisadvantageDescription")
                .Configure<FeatureDefinitionBuilder<FeatureDefinitionAttackDisadvantageAgainstNonSource>>(
                    definition => definition.ConditionName = IlluminatedConditionName)
                .AddToDB();
        }

        private static FeatureDefinition CreatePreventInvisibility()
        {
            // Prevents a creature from turning invisible by "granting" immunity to invisibility

            return FeatureDefinitionBuilder<FeatureDefinitionFeatureSet>
                .Create("PathOfTheLightIlluminatedPreventInvisibility", SubclassNamespace)
                .SetGuiPresentation("Feature/&NoContentTitle", "Subclass/&BarbarianPathOfTheLightIlluminatedPreventInvisibilityDescription")
                .Configure<FeatureDefinitionBuilder<FeatureDefinitionFeatureSet>>(
                    featureSetDefinition =>
                    {
                        featureSetDefinition
                            .SetEnumerateInDescription(false)
                            .SetMode(FeatureDefinitionFeatureSet.FeatureSetMode.Union)
                            .SetUniqueChoices(false);

                        foreach (var invisibleConditionName in InvisibleConditions.Select(ic => ic.Name))
                        {
                            var preventInvisibilitySubFeature = FeatureDefinitionBuilder<FeatureDefinitionConditionAffinity>
                                .Create("PathOfTheLightIlluminatedPreventInvisibility" + invisibleConditionName, SubclassNamespace)
                                .SetGuiPresentationNoContent()
                                .Configure<FeatureDefinitionBuilder<FeatureDefinitionConditionAffinity>>(
                                    conditionAffinityDefinition =>
                                    {
                                        conditionAffinityDefinition
                                            .SetConditionAffinityType(RuleDefinitions.ConditionAffinityType.Immunity)
                                            .SetConditionType(invisibleConditionName);
                                    })
                                .AddToDB();

                            featureSetDefinition.FeatureSet.Add(preventInvisibilitySubFeature);
                        }
                    })
                .AddToDB();
        }

        private static ConditionDefinition CreateIlluminatingBurstSuppressedCondition()
        {
            return ConditionDefinitionBuilder.Build(
                "PathOfTheLightIlluminatingBurstSuppressedCondition",
                CreateNamespacedGuid("PathOfTheLightIlluminatingBurstSuppressedCondition"),
                definition =>
                {
                    var gpb = new GuiPresentationBuilder(
                        "Feature/&NoContentTitle",
                        "Feature/&NoContentTitle");

                    var guiPresentation = gpb.Build();

                    guiPresentation.SetHidden(true);

                    definition
                        .SetGuiPresentation(guiPresentation)
                        .SetDurationType(RuleDefinitions.DurationType.Permanent)
                        .SetConditionType(RuleDefinitions.ConditionType.Neutral)
                        .SetSilentWhenAdded(true)
                        .SetSilentWhenRemoved(true);
                });
        }

        private static void HandleAfterIlluminatedConditionRemoved(RulesetActor removedFrom)
        {
            if (removedFrom is not RulesetCharacter character)
            {
                return;
            }

            // Intentionally *includes* conditions that have Illuminated as their parent (like the Illuminating Burst condition)
            if (!character.HasConditionOfTypeOrSubType(IlluminatedConditionName)
                && (character.PersonalLightSource?.SourceName == IlluminatingStrikeName || character.PersonalLightSource?.SourceName == IlluminatingBurstName))
            {
                var visibilityService = ServiceRepository.GetService<IGameLocationVisibilityService>();

                visibilityService.RemoveCharacterLightSource(GameLocationCharacter.GetFromActor(removedFrom), character.PersonalLightSource);
                character.PersonalLightSource = null;
            }
        }

        // Helper classes

        private sealed class IlluminatedConditionDefinition : ConditionDefinition, IConditionRemovedOnSourceTurnStart, INotifyConditionRemoval
        {
            public void AfterConditionRemoved(RulesetActor removedFrom, RulesetCondition rulesetCondition)
            {
                HandleAfterIlluminatedConditionRemoved(removedFrom);
            }

            public void BeforeDyingWithCondition(RulesetActor rulesetActor, RulesetCondition rulesetCondition)
            {
                ApplyLightsProtectionHealing(rulesetCondition.SourceGuid);
            }
        }

        private sealed class IlluminatedByBurstConditionDefinition : ConditionDefinition, INotifyConditionRemoval
        {
            public void AfterConditionRemoved(RulesetActor removedFrom, RulesetCondition rulesetCondition)
            {
                HandleAfterIlluminatedConditionRemoved(removedFrom);
            }

            public void BeforeDyingWithCondition(RulesetActor rulesetActor, RulesetCondition rulesetCondition)
            {
                ApplyLightsProtectionHealing(rulesetCondition.SourceGuid);
            }
        }

        private sealed class IlluminatingStrikeAdditionalDamage : FeatureDefinitionAdditionalDamage, IClassHoldingFeature
        {
            // Allows Illuminating Strike damage to scale with barbarian level
            public CharacterClassDefinition Class => CharacterClassDefinitions.Barbarian;
        }

        private sealed class IlluminatingStrikeFeatureBuilder : BaseDefinitionBuilder<IlluminatingStrikeAdditionalDamage>
        {
            public IlluminatingStrikeFeatureBuilder(string name, string guid, string title, string description, ConditionDefinition illuminatedCondition) : base(name, guid)
            {
                Definition
                    .SetGuiPresentation(CreatePowerGuiPresentation(title, description))
                    .SetAdditionalDamageType(RuleDefinitions.AdditionalDamageType.Specific)
                    .SetSpecificDamageType("DamageRadiant")
                    .SetTriggerCondition(RuleDefinitions.AdditionalDamageTriggerCondition.AlwaysActive)
                    .SetDamageValueDetermination(RuleDefinitions.AdditionalDamageValueDetermination.Die)
                    .SetDamageDiceNumber(1)
                    .SetDamageDieType(RuleDefinitions.DieType.D6)
                    .SetDamageSaveAffinity(RuleDefinitions.EffectSavingThrowType.None)
                    .SetDamageAdvancement(RuleDefinitions.AdditionalDamageAdvancement.ClassLevel)
                    .SetLimitedUsage(RuleDefinitions.FeatureLimitedUsage.OnceInMyturn)
                    .SetNotificationTag("BarbarianPathOfTheLightIlluminatingStrike")
                    .SetRequiredProperty(RuleDefinitions.AdditionalDamageRequiredProperty.None)
                    .SetAddLightSource(true)
                    .SetLightSourceForm(CreateIlluminatedLightSource());

                Definition.DiceByRankTable.AddRange(new[]
                {
                    BuildDiceByRank(3, 1),
                    BuildDiceByRank(4, 1),
                    BuildDiceByRank(5, 1),
                    BuildDiceByRank(6, 1),
                    BuildDiceByRank(7, 1),
                    BuildDiceByRank(8, 1),
                    BuildDiceByRank(9, 1),
                    BuildDiceByRank(10, 2),
                    BuildDiceByRank(11, 2),
                    BuildDiceByRank(12, 2),
                    BuildDiceByRank(13, 2),
                    BuildDiceByRank(14, 2),
                    BuildDiceByRank(15, 2),
                    BuildDiceByRank(16, 2),
                    BuildDiceByRank(17, 2),
                    BuildDiceByRank(18, 2),
                    BuildDiceByRank(19, 2),
                    BuildDiceByRank(20, 2)
                });

                Definition.ConditionOperations.Add(
                    new ConditionOperationDescription
                    {
                        Operation = ConditionOperationDescription.ConditionOperation.Add,
                        ConditionDefinition = illuminatedCondition
                    });

                foreach (ConditionDefinition invisibleCondition in InvisibleConditions)
                {
                    Definition.ConditionOperations.Add(
                        new ConditionOperationDescription
                        {
                            Operation = ConditionOperationDescription.ConditionOperation.Remove,
                            ConditionDefinition = invisibleCondition
                        });
                }
            }

            private static GuiPresentation CreatePowerGuiPresentation(string title, string description)
            {
                var guiPresentationBuilder = new GuiPresentationBuilder(title, description);

                guiPresentationBuilder.SetSpriteReference(FeatureDefinitionAdditionalDamages.AdditionalDamageDomainLifeDivineStrike.GuiPresentation.SpriteReference);

                return guiPresentationBuilder.Build();
            }

            private static LightSourceForm CreateIlluminatedLightSource()
            {
                EffectForm faerieFireLightSource = SpellDefinitions.FaerieFire.EffectDescription.GetFirstFormOfType(EffectForm.EffectFormType.LightSource);

                var lightSourceForm = new LightSourceForm();
                lightSourceForm.Copy(faerieFireLightSource.LightSourceForm);

                lightSourceForm
                    .SetBrightRange(4)
                    .SetDimAdditionalRange(4);

                return lightSourceForm;
            }

            // Common helper: factor out
            private static DiceByRank BuildDiceByRank(int rank, int dice)
            {
                DiceByRank diceByRank = new DiceByRank();
                diceByRank.SetRank(rank);
                diceByRank.SetDiceNumber(dice);
                return diceByRank;
            }
        }

        /// <summary>
        /// Builds the power that enables Illuminating Strike while you're raging.
        /// </summary>
        private sealed class IlluminatingStrikeInitiatorBuilder : BaseDefinitionBuilder<FeatureDefinitionPower>
        {
            public IlluminatingStrikeInitiatorBuilder(string name, string guid, string title, string description, ConditionDefinition illuminatedCondition) : base(name, guid)
            {
                Definition
                    .SetGuiPresentation(CreatePowerGuiPresentation(title, description))
                    .SetActivationTime(RuleDefinitions.ActivationTime.OnRageStartAutomatic)
                    .SetEffectDescription(CreatePowerEffect(illuminatedCondition))
                    .SetRechargeRate(RuleDefinitions.RechargeRate.AtWill)
                    .SetShowCasting(false);
            }

            private static GuiPresentation CreatePowerGuiPresentation(string title, string description)
            {
                var guiPresentationBuilder = new GuiPresentationBuilder(title, description);

                guiPresentationBuilder.SetSpriteReference(FeatureDefinitionAdditionalDamages.AdditionalDamageDomainLifeDivineStrike.GuiPresentation.SpriteReference);

                var guiPresentation = guiPresentationBuilder.Build();
                guiPresentation.SetHidden(true);

                return guiPresentation;
            }

            private static EffectDescription CreatePowerEffect(ConditionDefinition illuminatedCondition)
            {
                var initiatorCondition = ConditionDefinitionBuilder.Build(
                    "PathOfTheLightIlluminatingStrikeInitiatorCondition",
                    CreateNamespacedGuid("PathOfTheLightIlluminatingStrikeInitiatorCondition"),
                    definition =>
                    {
                        var gpb = new GuiPresentationBuilder("Feature/&NoContentTitle", "Feature/&NoContentTitle");

                        GuiPresentation guiPresentation = gpb.Build();

                        guiPresentation.SetHidden(true);

                        definition
                            .SetGuiPresentation(guiPresentation)
                            .SetDurationType(RuleDefinitions.DurationType.Minute)
                            .SetDurationParameter(1)
                            .SetConditionType(RuleDefinitions.ConditionType.Beneficial)
                            .SetTerminateWhenRemoved(true)
                            .SetSilentWhenAdded(true)
                            .SetSilentWhenRemoved(true);

                        var illuminatingStrikeFeature = new IlluminatingStrikeFeatureBuilder(
                            IlluminatingStrikeName,
                            CreateNamespacedGuid(IlluminatingStrikeName),
                            "Feature/&NoContentTitle",
                            "Feature/&NoContentTitle",
                            illuminatedCondition);

                        definition.Features.Add(illuminatingStrikeFeature.AddToDB());

                        definition.SpecialInterruptions.SetRange(RuleDefinitions.ConditionInterruption.RageStop);
                    });

                var enableIlluminatingStrike = new EffectForm
                {
                    FormType = EffectForm.EffectFormType.Condition,
                    ConditionForm = new ConditionForm
                    {
                        Operation = ConditionForm.ConditionOperation.Add,
                        ConditionDefinition = initiatorCondition
                    }
                };

                var effectDescriptionBuilder = new EffectDescriptionBuilder();

                effectDescriptionBuilder
                    .SetDurationData(RuleDefinitions.DurationType.Minute, 1, RuleDefinitions.TurnOccurenceType.StartOfTurn)
                    .AddEffectForm(enableIlluminatingStrike);

                return effectDescriptionBuilder.Build();
            }
        }

        private sealed class IlluminatingBurstPower : FeatureDefinitionPower, IStartOfTurnRecharge
        {
            public bool IsRechargeSilent => true;
        }

        private sealed class IlluminatingBurstBuilder : BaseDefinitionBuilder<IlluminatingBurstPower>
        {
            public IlluminatingBurstBuilder(string name, string guid, string title, string description, ConditionDefinition illuminatedCondition, ConditionDefinition illuminatingBurstSuppressedCondition) : base(name, guid)
            {
                Definition
                    .SetGuiPresentation(CreatePowerGuiPresentation(title, description))
                    .SetActivationTime(RuleDefinitions.ActivationTime.NoCost)
                    .SetEffectDescription(CreatePowerEffect(illuminatedCondition))
                    .SetRechargeRate(RuleDefinitions.RechargeRate.OneMinute) // Actually recharges at the start of your turn, using IStartOfTurnRecharge
                    .SetFixedUsesPerRecharge(1)
                    .SetUsesDetermination(RuleDefinitions.UsesDetermination.Fixed)
                    .SetCostPerUse(1)
                    .SetShowCasting(false)
                    .SetDisableIfConditionIsOwned(illuminatingBurstSuppressedCondition); // Only enabled on the turn you enter a rage
            }

            private static GuiPresentation CreatePowerGuiPresentation(string title, string description)
            {
                var guiPresentationBuilder = new GuiPresentationBuilder(title, description);

                guiPresentationBuilder.SetSpriteReference(FeatureDefinitionPowers.PowerDomainSunHeraldOfTheSun.GuiPresentation.SpriteReference);

                return guiPresentationBuilder.Build();
            }

            private static EffectDescription CreatePowerEffect(ConditionDefinition illuminatedCondition)
            {
                var effectDescriptionBuilder = new EffectDescriptionBuilder();

                var dealDamage = new EffectForm
                {
                    FormType = EffectForm.EffectFormType.Damage,
                    DamageForm = new DamageForm
                    {
                        DamageType = "DamageRadiant",
                        DiceNumber = 4,
                        DieType = RuleDefinitions.DieType.D6
                    },
                    SavingThrowAffinity = RuleDefinitions.EffectSavingThrowType.Negates
                };

                var illuminatedByBurstCondition = ConditionDefinitionBuilder<IlluminatedByBurstConditionDefinition>.Build(
                    "PathOfTheLightIlluminatedByBurstCondition",
                    CreateNamespacedGuid("PathOfTheLightIlluminatedByBurstCondition"),
                    definition =>
                    {
                        var gpb = new GuiPresentationBuilder(
                            "Subclass/&BarbarianPathOfTheLightIlluminatedConditionTitle",
                            "Subclass/&BarbarianPathOfTheLightIlluminatedConditionDescription");

                        gpb.SetSpriteReference(ConditionDefinitions.ConditionBranded.GuiPresentation.SpriteReference);

                        definition
                            .SetGuiPresentation(gpb.Build())
                            .SetDurationType(RuleDefinitions.DurationType.Minute)
                            .SetDurationParameter(1)
                            .SetConditionType(RuleDefinitions.ConditionType.Detrimental)
                            .SetAllowMultipleInstances(true)
                            .SetSilentWhenAdded(true)
                            .SetSilentWhenRemoved(false)
                            .SetParentCondition(illuminatedCondition);
                    });

                var addIlluminatedCondition = new EffectForm
                {
                    FormType = EffectForm.EffectFormType.Condition,
                    ConditionForm = new ConditionForm
                    {
                        Operation = ConditionForm.ConditionOperation.Add,
                        ConditionDefinition = illuminatedByBurstCondition
                    },
                    CanSaveToCancel = true,
                    SaveOccurence = RuleDefinitions.TurnOccurenceType.EndOfTurn,
                    SavingThrowAffinity = RuleDefinitions.EffectSavingThrowType.Negates
                };

                EffectForm faerieFireLightSource = SpellDefinitions.FaerieFire.EffectDescription.GetFirstFormOfType(EffectForm.EffectFormType.LightSource);

                var lightSourceForm = new LightSourceForm();
                lightSourceForm.Copy(faerieFireLightSource.LightSourceForm);

                lightSourceForm
                    .SetBrightRange(4)
                    .SetDimAdditionalRange(4);

                var addLightSource = new EffectForm
                {
                    FormType = EffectForm.EffectFormType.LightSource,
                    SavingThrowAffinity = RuleDefinitions.EffectSavingThrowType.Negates
                };

                addLightSource.SetLightSourceForm(lightSourceForm);

                effectDescriptionBuilder
                    .SetSavingThrowData(
                        hasSavingThrow: true,
                        disableSavingThrowOnAllies: false,
                        savingThrowAbility: "Constitution",
                        ignoreCover: false,
                        RuleDefinitions.EffectDifficultyClassComputation.AbilityScoreAndProficiency,
                        savingThrowDifficultyAbility: "Constitution",
                        fixedSavingThrowDifficultyClass: 10,
                        advantageForEnemies: false,
                        new List<SaveAffinityBySenseDescription>())
                    .SetDurationData(
                        RuleDefinitions.DurationType.Minute,
                        durationParameter: 1,
                        RuleDefinitions.TurnOccurenceType.EndOfTurn)
                    .SetTargetingData(
                        RuleDefinitions.Side.Enemy,
                        RuleDefinitions.RangeType.Distance,
                        rangeParameter: 6,
                        RuleDefinitions.TargetType.IndividualsUnique,
                        targetParameter: 3,
                        targetParameter2: 0,
                        ActionDefinitions.ItemSelectionType.None)
                    .SetSpeed(
                        RuleDefinitions.SpeedType.CellsPerSeconds,
                        speedParameter: 9.5f)
                    .SetParticleEffectParameters(SpellDefinitions.GuidingBolt.EffectDescription.EffectParticleParameters)
                    .AddEffectForm(dealDamage)
                    .AddEffectForm(addIlluminatedCondition)
                    .AddEffectForm(addLightSource);

                return effectDescriptionBuilder.Build();
            }
        }

        /// <summary>
        /// Builds the power that enables Illuminating Burst on the turn you enter a rage (by removing the condition disabling it).
        /// </summary>
        private sealed class IlluminatingBurstInitiatorBuilder : BaseDefinitionBuilder<FeatureDefinitionPower>
        {
            public IlluminatingBurstInitiatorBuilder(string name, string guid, string title, string description, ConditionDefinition illuminatingBurstSuppressedCondition) : base(name, guid)
            {
                Definition
                    .SetGuiPresentation(CreatePowerGuiPresentation(title, description))
                    .SetActivationTime(RuleDefinitions.ActivationTime.OnRageStartAutomatic)
                    .SetEffectDescription(CreatePowerEffect(illuminatingBurstSuppressedCondition))
                    .SetRechargeRate(RuleDefinitions.RechargeRate.AtWill)
                    .SetShowCasting(false);
            }

            private static GuiPresentation CreatePowerGuiPresentation(string title, string description)
            {
                var guiPresentationBuilder = new GuiPresentationBuilder(title, description);

                var guiPresentation = guiPresentationBuilder.Build();
                guiPresentation.SetHidden(true);

                return guiPresentation;
            }

            private static EffectDescription CreatePowerEffect(ConditionDefinition illuminatingBurstSuppressedCondition)
            {
                var enableIlluminatingBurst = new EffectForm
                {
                    FormType = EffectForm.EffectFormType.Condition,
                    ConditionForm = new ConditionForm
                    {
                        Operation = ConditionForm.ConditionOperation.Remove,
                        ConditionDefinition = illuminatingBurstSuppressedCondition
                    }
                };

                var effectDescriptionBuilder = new EffectDescriptionBuilder();

                effectDescriptionBuilder
                    .SetDurationData(RuleDefinitions.DurationType.Round, 1, RuleDefinitions.TurnOccurenceType.EndOfTurn)
                    .AddEffectForm(enableIlluminatingBurst);

                return effectDescriptionBuilder.Build();
            }
        }
    }
}
