using System;
using Nightfall.SharedUtils.Net.Messaging;

namespace Nightfall.Net.Messaging
{
    public sealed partial class RootHashMessage : Message
    {
        public override string ToString()
        {
            if (Data.Length is 0 or > int.MaxValue / 2)
            {
                return string.Empty;
            }

            return string.Create(Data.Length * 2, (Data, Data.Length),
                static (dst, state) =>
                {
                    static char ToCharLower(int value)
                    {
                        value &= 0xF;
                        value += '0';

                        if (value > '9')
                        {
                            value += 'a' - ('9' + 1);
                        }

                        return (char) value;
                    }

                    var (data, length) = state;
                    var src = new ReadOnlySpan<byte>(data, 0, length);

                    var j = 0;
                    var i = 0;

                    while (i < src.Length)
                    {
                        var b = src[i++];
                        dst[j++] = ToCharLower(b >> 4);
                        dst[j++] = ToCharLower(b);
                    }
                });
        }
    }
}