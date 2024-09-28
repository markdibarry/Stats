using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace GameCore.Statistics;

public static class EffectDefDB
{
    static EffectDefDB()
    {
        Data = s_data.AsReadOnly();
    }

    public static ReadOnlyDictionary<string, EffectDef> Data { get; }
    private static readonly Dictionary<string, EffectDef> s_data = [];

    public static bool TryGetValue(string statusEffectId, [MaybeNullWhen(false)] out EffectDef data)
    {
        return s_data.TryGetValue(statusEffectId, out data);
    }

    public static void Add(EffectDef effectDef)
    {
        if (effectDef.StatTypeId.Length == 0)
            throw new System.Exception("Effect definition must have a unique type id.");

        s_data.Add(effectDef.StatTypeId, effectDef);
    }
}
