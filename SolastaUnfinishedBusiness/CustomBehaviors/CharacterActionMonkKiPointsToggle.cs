using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

//This should have default namespace so that it can be properly created by `CharacterActionPatcher`
// ReSharper disable once CheckNamespace
[UsedImplicitly]
public class CharacterActionMonkKiPointsToggle : CharacterAction
{
    public const string KiPointsTag = "#KiPoints#";

    public CharacterActionMonkKiPointsToggle(CharacterActionParams actionParams) : base(actionParams)
    {
    }

    public override IEnumerator ExecuteImpl()
    {
        var rulesetCharacter = this.ActingCharacter.RulesetCharacter;

        if (rulesetCharacter.dummy.Contains(KiPointsTag))
        {
            rulesetCharacter.dummy = rulesetCharacter.dummy.Replace(KiPointsTag, String.Empty);
        }
        else
        {
            rulesetCharacter.dummy += KiPointsTag;
        }

        rulesetCharacter.KiPointsAltered?.Invoke(rulesetCharacter, rulesetCharacter.RemainingKiPoints);

        yield return null;
    }
}
