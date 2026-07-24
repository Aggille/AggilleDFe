using MudBlazor;

namespace AggilleDFe.Web.Shared;

public static class CnpjMask
{
    public static readonly PatternMask Mask = new("aa.aaa.aaa/aaaa-00")
    {
        MaskChars =
        [
            new MaskChar('a', "[A-Za-z0-9]"),
            new MaskChar('0', "[0-9]")
        ]
    };
}
