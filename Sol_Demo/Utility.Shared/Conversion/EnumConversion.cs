using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility.Shared.Conversion;

public static class EnumConversion
{
    public static TEnum FromObject<TEnum, TValue>(TValue value)
    {
        return (TEnum)Enum.ToObject(typeof(TEnum), value!);
    }
}