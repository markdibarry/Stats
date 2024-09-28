using System.Text.Json.Serialization;
using GameCore.Utility;

namespace GameCore.Statistics;

[JsonConverter(typeof(ConditionConverter))]
public abstract class Condition : IPoolable
{
    protected Condition(string conditionType)
    {
        ConditionType = conditionType;
    }

    private bool _result;
    private Condition? _parent;

    [JsonPropertyOrder(-5)]
    public string ConditionType { get; }
    [JsonPropertyOrder(-4)]
    public bool Not { get; set; }
    [JsonPropertyOrder(-3)]
    public bool ReupOnMet { get; set; }
    [JsonPropertyOrder(-2)]
    public bool IgnoreModsWithSource { get; set; }
    [JsonPropertyOrder(20)]
    public Condition? And { get; set; }
    [JsonPropertyOrder(21)]
    public Condition? Or { get; set; }
    [JsonIgnore]
    public StatsBase? Stats => Conditional?.Stats;
    [JsonIgnore]
    public bool Registered { get; private set; }
    protected IConditional? Conditional { get; private set; }

    public bool CheckAllConditions(bool hasSource = false)
    {
        if (_result && !(IgnoreModsWithSource && hasSource))
            return And?.CheckAllConditions(hasSource) ?? true;
        else
            return Or?.CheckAllConditions(hasSource) ?? false;
    }

    public T? GetFirstCondition<T>() where T : Condition, new()
    {
        if (this is T t)
            return t;

        if (And?.GetFirstCondition<T>() is T andResult)
            return andResult;

        if (Or?.GetFirstCondition<T>() is T orResult)
            return orResult;

        return null;
    }

    public void ClearObject()
    {
        _result = false;

        And?.ReturnToPool();
        Or?.ReturnToPool();

        And = null;
        Or = null;
        _parent = null;
        Conditional = null;
        ReupOnMet = false;
        Not = false;
        IgnoreModsWithSource = false;
        ClearData();
    }

    public Condition Clone()
    {
        Condition clone = CloneSingle();

        if (And is not null)
            clone.And = And.Clone();

        if (Or is not null)
            clone.Or = Or.Clone();

        return clone;
    }

    public Condition CloneSingle()
    {
        Condition clone = Pool.GetOfSameType(this);
        clone.ReupOnMet = ReupOnMet;
        clone.Not = Not;
        clone.IgnoreModsWithSource = IgnoreModsWithSource;
        SetCloneData(clone);
        return clone;
    }

    /// <summary>
    /// Recursively calls up until it returns the top-most condition.
    /// </summary>
    /// <returns></returns>
    public Condition GetHeadCondition()
    {
        return _parent?.GetHeadCondition() ?? this;
    }

    public void Reup()
    {
        ReupData();
        UpdateCondition();
    }

    public void ReupAllData()
    {
        And?.ReupData();
        Or?.ReupData();
        ReupData();
    }

    public void Register(IConditional owner, Condition? parent)
    {
        if (Registered)
            return;

        _parent = parent;
        Conditional = owner;
        SubscribeEvents();
        And?.Register(owner, this);
        Or?.Register(owner, this);
        UpdateCondition();
        Registered = true;
    }

    public void Unregister()
    {
        if (!Registered)
            return;

        UnsubscribeEvents();
        And?.Unregister();
        Or?.Unregister();
        _parent = null;
        Conditional = null;
        Registered = false;
    }

    public void InsertOr(Condition child)
    {
        Insert(child);
        Or = child;
    }

    public void InsertAnd(Condition child)
    {
        Insert(child);
        And = child;
    }

    protected abstract void SubscribeEvents();

    protected abstract void UnsubscribeEvents();

    protected abstract bool GetResult();

    /// <summary>
    /// Reverts condition data to initial user set values.
    /// </summary>
    protected abstract void ReupData();

    /// <summary>
    /// Clears all condition data.
    /// </summary>
    protected abstract void ClearData();

    /// <summary>
    /// Used to assign values for a derived Condition object.
    /// </summary>
    /// <param name="clone">A cloned Condition of the derived type.</param>
    protected abstract void SetCloneData(Condition clone);

    /// <summary>
    /// Updates the _conditionMet flag and returns true if the result is different
    /// than the previous value.
    /// </summary>
    /// <returns>The result of whether the condition was updated or not.</returns>
    protected bool UpdateCondition()
    {
        bool result = GetResult();

        if (Not)
            result = !result;

        if (result != _result)
        {
            _result = result;
            return true;
        }

        return false;
    }

    protected void RaiseConditionChanged()
    {
        if (!UpdateCondition())
            return;

        Conditional?.OnConditionChanged(this);
    }

    private void Insert(Condition child)
    {
        _parent = child._parent;
        child._parent = this;
    }
}

public static class ConditionExtensions
{
    public static T AddOr<T>(this T condition, Condition or) where T : Condition
    {
        condition.Or = or;
        return condition;
    }

    public static T AddAnd<T>(this T condition, Condition and) where T : Condition
    {
        condition.And = and;
        return condition;
    }

    public static T EnableIgnoreModsWithSource<T>(this T condition) where T : Condition
    {
        condition.IgnoreModsWithSource = true;
        return condition;
    }

    public static T EnableNot<T>(this T condition) where T : Condition
    {
        condition.Not = true;
        return condition;
    }

    public static T EnableReupOnMet<T>(this T condition) where T : Condition
    {
        condition.ReupOnMet = true;
        return condition;
    }
}