namespace SignalR.MagicHub.Filtering.Expressions
{
    /// <summary>
    /// Enum for operators used to define filter expressions
    /// </summary>
    public enum FilterOperator
    {
        GreaterThan = 1,
        GreaterThanOrEqualTo = 2,
        EqualTo = 4,
        NotEqualTo = 8,
        LessThanOrEqualTo = 16,
        LessThan = 32,
        And = 64,
        Or = 128
    }   
}
