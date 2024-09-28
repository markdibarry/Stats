using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GameCore.Utility;

namespace GameCore.Statistics;

public static class ConditionDB
{
    static ConditionDB()
    {
        Add<TimedCondition>(TimedCondition.TypeId);
        Types = s_types.AsReadOnly();
    }

    public static ReadOnlyDictionary<string, Type> Types { get; }
    private static readonly Dictionary<string, Type> s_types = [];

    public static void Add<T>(string typeId) where T : Condition, IPoolable, new()
    {
        s_types.Add(typeId, typeof(T));
        Pool.Register<T>();
    }
}
