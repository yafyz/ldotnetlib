using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace lua51interp.Lua.libs.ldotnetlib {
    internal class ltypeconv {
        public static void Open(LuaState L) {
            TTable gt = L.l_gt;

            CClosure cc_to_char = CClosure.New(to_char, "dn.conv.to_char");
            gt.RawHSet("char", cc_to_char);
            gt.RawHSet("c", cc_to_char);

            CClosure cc_to_int32 = CClosure.New(L => to_number<int>(L), "dn.conv.to_int32");
            gt.RawHSet("i32", cc_to_int32);
            gt.RawHSet("i", cc_to_int32);

            gt.RawHSet("i16", CClosure.New(L => to_number<short>(L), "dn.conv.to_int16"));
            gt.RawHSet("i8", CClosure.New(L => to_number<sbyte>(L), "dn.conv.to_int8"));
            gt.RawHSet("i64", CClosure.New(L => to_number<long>(L), "dn.conv.to_int64"));

            CClosure cc_to_uint32 = CClosure.New(L => to_number<uint>(L), "dn.conv.to_uint64");
            gt.RawHSet("u32", cc_to_uint32);
            gt.RawHSet("u", cc_to_uint32);

            gt.RawHSet("u16", CClosure.New(L => to_number<ushort>(L), "dn.conv.to_uint16"));
            gt.RawHSet("u8", CClosure.New(L => to_number<byte>(L), "dn.conv.to_uint8"));
            gt.RawHSet("u64", CClosure.New(L => to_number<ulong>(L), "dn.conv.to_uint64"));

            CClosure cc_to_decimal = CClosure.New(L => to_number<decimal>(L), "dn.conv.to_decimal");
            gt.RawHSet("dec", cc_to_decimal);
            gt.RawHSet("decimal", cc_to_decimal);
        }

        static int to_number<T>(LuaState L) {
            L.luaL_nargcheck(1);
            
            object? value = L.FromLua(L.@base.v);
            
            if (value is string str) {
                try {
                    L.top++.v = ObjectWrap.New(typeof(T)
                            .GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(string) })!
                            .Invoke(null, new string[] { str })!);
                } catch (Exception ex) {
                    if (ex.InnerException is FormatException)
                        L.luaG_runerror("bad format");
                    if (ex.InnerException is OverflowException)
                        L.luaG_runerror("number too large");
                    throw;
                }
            } else if (is_number(value)) {
                L.top++.v = ObjectWrap.New(Convert.ChangeType(value, typeof(T)));
            } else {
                L.luaG_runerror("expected number or string");
            }

            return 1;
        }

        static int to_char(LuaState L) {
            L.luaL_nargcheck(1);

            object? value = L.FromLua(L.@base.v);

            if (value is string str) {
                if (str.Length != 1)
                    L.luaG_runerror("string must have a length of 1");
                L.top++.v = ObjectWrap.New(str[0]);
            } else if (is_number(value)) {
                L.top++.v = ObjectWrap.New((char)value);
            } else {
                L.luaG_runerror("expected number or string");
            }

            return 1;
        }

        static bool is_number(object? obj) {
            return is_number(obj.GetType());
        }

        public static bool is_number(Type t) {
            switch (Type.GetTypeCode(t)) {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
            }

            return false;
        }
    }
}
