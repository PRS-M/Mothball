using System.Collections.ObjectModel;

namespace CoreApp.Utilities;

public static class EnumValues
{
    public static ReadOnlyCollection<TEnum> CreateReadOnly<TEnum>()
        where TEnum : struct, Enum
        => new(Enum.GetValues<TEnum>());
}