using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace lua51interp.Lua.libs.ldotnetlib {
    public static class funcs {
        public static BindingFlags bindingAttrs = BindingFlags.Public;

        public static void Open(LuaState L) {
            TTable gt = L.l_gt;

            gt.RawHSet("tolua", CClosure.New(tolua, "dn.tolua"));
            gt.RawHSet("Type", TypeWrap.New(typeof(Type)));
            gt.RawHSet("typeof", CClosure.New(dntypeof, "dn.dntypeof"));
            gt.RawHSet("t", CClosure.New(dotnettype, "dn.dotnettype"));
            gt.RawHSet("new", CClosure.New(_new, "dn.new"));

            gt.RawHSet("see_private", CClosure.New(see_private, "dn.see_private"));
            gt.RawHSet("breakpoint", CClosure.New(breakpoint, "dn.breakpoint"));

            ltypeconv.Open(L);
            lmethod.Open(L);
            levent.Open(L);
        }

        static int breakpoint(LuaState L) {
            return 0;
        }

        static int see_private(LuaState L) {
            L.luaL_nargcheck(1);
            bool b = L.luaL_checktype(1, LUA_TBOOLEAN).As<TBool>().value;

            bindingAttrs = BindingFlags.Public | (b ? BindingFlags.NonPublic : 0);

            return 0;
        }

        static int _new(LuaState L) {
            L.luaL_nargcheck(1);
            L.luaL_checktype(1, LUA_TUSERDATA);

            if (L.@base.v.As<TUserdata>().ud is TypeWrap tw) {
                return MethodWrap.luadn_call(L, tw.type.GetConstructors(), L.lua_gettop() - 1);
            } else {
                L.luaG_runerror("argument 1 expected Type");
            }

            return 0;
        }

        static int dntypeof(LuaState L) {
            L.luaL_nargcheck(1);
            L.luaL_checktype(1, LUA_TUSERDATA);

            if (L.@base.v.As<TUserdata>().ud is TypeWrap tw) {
                L.top++.v = ObjectWrap.New(tw.type);
                return 1;
            }

            L.luaG_runerror("expected type");
            return 0;
        }

        public static Type? ResolveType(string typename) {
            Type? t = null;

            if (typename.Length > 0)
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    if ((t = asm.GetType(typename)) != null)
                        break;
            return t;
        }

        static int dotnettype(LuaState L) {
            L.luaL_nargcheck(1);
            L.luaL_checktype(1, LUA_TSTRING);

            string typename = L.@base.v.As<TString>().str;
            Type? t = ResolveType(typename);

            if (t == null)
                L.luaG_runerror("no type '", typename, "' exists in any loaded assembly");

            L.top++.v = TypeWrap.New(t);
            return 1;
        }

        public static TValue ToLua(this LuaState L, object? obj) {
            if (obj == null)
                return nil;

            switch (Type.GetTypeCode(obj.GetType())) {
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
                    return TNumber.New((LuaNumberType)Convert.ChangeType(obj, typeof(LuaNumberType)));
                case TypeCode.Char:
                    return TString.New(new string((char)obj, 1));
                case TypeCode.String:
                    return TString.New((String)obj);
                case TypeCode.Boolean:
                    return TBool.New((bool)obj);
            }

            if (obj is TValue tv)
                return tv;

            L.luaG_runerror("Cannot convert '", obj.GetType().FullName, "' into a lua type");
            return null!;
        }

        public static object? FromLua(TValue v) {
            return v.tt switch {
                LUA_TNONE => null,
                LUA_TNIL => null,
                LUA_TBOOLEAN => v.As<TBool>().value,
                LUA_TLIGHTUSERDATA => v.As<TUserdata>().ud,
                LUA_TNUMBER => v.As<TNumber>().value,
                LUA_TSTRING => v.As<TString>().str,
                LUA_TTABLE => throw new NotImplementedException(),
                LUA_TFUNCTION => throw new NotImplementedException(),
                LUA_TUSERDATA => v.As<TUserdata>().ud,
                LUA_TTHREAD => v.As<TThread>().th,
            };
        }

        public static object? FromLua(this LuaState L, TValue v) {
            if (v.tt == LUA_TFUNCTION || v.tt == LUA_TTABLE)
                L.luaG_runerror("attempt to pass a lua ",L.lua_typename(v.tt)," to .NET code");
            return FromLua(v);
        }

        public static object? FromLua(this LuaState L, TValue v, Type T) {
            return Convert.ChangeType(FromLua(L, v), T);
        }

        public static T FromLua<T>(this LuaState L, TValue v) {
            return (T)FromLua(L, v, typeof(T));
        }

        static int tolua(LuaState L) {
            L.luaL_nargcheck(1);
            L.luaL_checktype(1, LUA_TUSERDATA);

            TUserdata lud = (TUserdata)L.@base.v;
            L.top++.v = L.ToLua(lud.ud);

            return 1;
        }
    }
}
