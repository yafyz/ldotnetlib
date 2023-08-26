using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using static lua51interp.Lua.libs.ldotnetlib.funcs;

namespace lua51interp.Lua.libs.ldotnetlib {
    public class TypeWrap {
        public Type type;

        TypeWrap(Type type) {
            this.type = type;
        }

        public static TUserdata New(Type type) {
            var tw = new TypeWrap(type);
            var ud = TUserdata.New(tw);

            ud.metatable = TTable.New();
            ud.metatable.RawHSet("__index", CClosure.New(__index, "TypeWrap.__index"));
            ud.metatable.RawHSet("__call", CClosure.New(tw.__call, "TypeWrap.__call"));

            return ud;
        }

        int __call(LuaState L) {
            var types = new Type[L.lua_gettop()-1];
            for (int i = 0; i < types.Length; i++)
                types[i] = lmethod.LGetType(L, (L.@base + i + 1).v);

            L.top++.v = New(type.MakeGenericType(types));            

            return 1;
        }

        static int __index(LuaState L) {
            L.luaL_nargcheck(2);
            L.luaL_checktype(1, LUA_TUSERDATA);
            L.luaL_checktype(2, LUA_TSTRING);

            TypeWrap tw = L.@base.v.As<TUserdata>().ud as TypeWrap;
            string key = (L.@base + 1).v.As<TString>().str;

            var m = tw.type.GetMember(key, BindingFlags.Static | bindingAttrs);

            if (m.Length < 1)
                L.luaG_runerror("no static field \"", key, "\" defined on type \"", tw.type.FullName, "\"");

            switch (m[0].MemberType) {
                case MemberTypes.Field:
                    if (m.Length > 1)
                        L.luaG_runerror("more than one static field \"", key, "\" defined on type \"", tw.type.FullName, "\"");
                    FieldInfo fi = (FieldInfo)m[0];
                    L.top++.v = ObjectWrap.New(fi.GetValue(null));
                    break;
                case MemberTypes.Property:
                    if (m.Length > 1)
                        L.luaG_runerror("more than one static property \"", key, "\" defined on type \"", tw.type.FullName, "\"");
                    PropertyInfo pi = (PropertyInfo)m[0];
                    L.top++.v = ObjectWrap.New(pi.GetValue(null));
                    break;
                case MemberTypes.Method:
                    L.top++.v = MethodWrap.New(m, null);
                    break;
                case MemberTypes.Event:
                    if (m.Length > 1)
                        L.luaG_runerror("more than one static event \"", key, "\" defined on type \"", tw.type.FullName, "\"");
                    EventInfo ei = (EventInfo)m[0];
                    L.top++.v = levent.EventWrap.New(ei, null);
                    break;
                default:
                    L.luaG_runerror("__index not implemented for member type '", m[0].MemberType, "'");
                    break;
            }


            return 1;
        }
    }
}
