using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;

using static lua51interp.Lua.libs.ldotnetlib.funcs;

namespace lua51interp.Lua.libs.ldotnetlib {
    public class ObjectWrap {
        static ConditionalWeakTable<object?, TValue> cache = new();

        public static TValue New(object? o) {
            if (o == null)
                return nil;

            if (cache.TryGetValue(o, out TValue c))
                return c;
            
            if (o is bool b)
                return TBool.New(b);

            var ud = TUserdata.New(o);
            ud.metatable = TTable.New();

            ud.metatable.RawHSet("__index", CClosure.New(__index, "ObjectWrap.__index"));
            ud.metatable.RawHSet("__newindex", CClosure.New(__newindex, "ObjectWrap.__newindex"));
            ud.metatable.RawHSet("__tostring", CClosure.New(__tostring, "ObjectWrap.__tostring"));
            ud.metatable.RawHSet("__add", CClosure.New(get_op("op_Addition"), "ObjectWrap.__add"));

            ud.metatable.RawHSet("__metatable", TString.New("this metatable is locked"));

            return ud;
        }

        static LuaCFunction get_op(string opname) {
            return L => {
                L.luaL_nargcheck(2);
                object? l = L.FromLua(L.@base.v);
                object? r = L.FromLua((L.@base + 1).v);

                // todo: basic operators (eg. int+int)

                var mis = l.GetType().GetMethods(BindingFlags.Static | bindingAttrs)
                    .Union(r.GetType().GetMethods(BindingFlags.Static | bindingAttrs))
                    .Where(x => x.Name == opname)
                    .ToArray();

                return MethodWrap.luadn_call(L, mis, 2);
            };
        }

        static int __tostring(LuaState L) {
            L.luaL_nargcheck(1);
            L.luaL_checktype(1, LUA_TUSERDATA);
            L.top++.v = L.ToLua(L.@base.v.As<TUserdata>().ud.ToString());
            return 1;
        }

        static int __index(LuaState L) {
            L.luaL_nargcheck(2);
            L.luaL_checktype(1, LUA_TUSERDATA);

            object? ud = ((TUserdata)L.@base.v).ud;
            TValue key = (L.@base + 1).v;

            if (ud == null)
                L.luaG_runerror("Attempt to index a null value");

            Type t = ud.GetType();

            if (t.IsArray && (L.@base + 1).v.tt == LUA_TNUMBER) {
                var en = (object?[])ud;
                int i = L.luaL_checkint(2);
                
                L.top++.v = New(en[i]);
                return 1;
            }

            if (t.IsClass || t.IsValueType) {
                if (key.tt == LUA_TSTRING) {
                    string str = ((TString)key).str;
                    
                    var m = t.GetMember(str, BindingFlags.Instance | bindingAttrs);
                    if (m.Length < 1)
                        L.luaG_runerror("no member named '", str, "' exists on type '", t.FullName, "'");

                    switch (m[0].MemberType) {
                        case MemberTypes.Field:
                            if (m.Length > 1)
                                L.luaG_runerror("more than one field \"", str, "\" defined on type \"", t.FullName, "\"");
                            FieldInfo fi = (FieldInfo)m[0];
                            L.top++.v = New(fi.GetValue(ud));
                            break;
                        case MemberTypes.Property:
                            if (m.Length > 1)
                                L.luaG_runerror("more than one property \"", str, "\" defined on type \"", t.FullName, "\"");
                            PropertyInfo pi = (PropertyInfo)m[0];
                            L.top++.v = New(pi.GetValue(ud));
                            break;
                        case MemberTypes.Method:
                            L.top++.v = MethodWrap.New(m, ud);
                            break;
                        case MemberTypes.Event:
                            if (m.Length > 1)
                                L.luaG_runerror("more than one event \"", str, "\" defined on type \"", t.FullName, "\"");
                            EventInfo ei = (EventInfo)m[0];
                            L.top++.v = levent.EventWrap.New(ei, ud);
                            break;
                        default:
                            L.luaG_runerror("__index not implemented for member type '", m[0].MemberType, "'");
                            break;
                    }

                    return 1;
                }
            }

            L.luaG_runerror("unimplemented");

            return 0;
        }

        static int __newindex(LuaState L) {
            L.luaL_nargcheck(3);
            L.luaL_checktype(1, LUA_TUSERDATA);

            object? ud = ((TUserdata)L.@base.v).ud;
            TValue key = (L.@base + 1).v;
            TValue value = (L.@base + 2).v;

            if (ud == null)
                L.luaG_runerror("Attempt to index a null value");

            Type t = ud.GetType();

            if (t.IsArray && (L.@base+1).v.tt == LUA_TNUMBER) {
                var en = (object?[])ud;
                int i = L.luaL_checkint(2);

                en[i] = L.FromLua(value);
                return 0;
            }

            if (t.IsClass || t.IsValueType) {
                if (key.tt == LUA_TSTRING) {
                    string str = key.As<TString>().str;

                    var m = t.GetMember(str, BindingFlags.Instance | bindingAttrs);
                    if (m.Length < 1)
                        L.luaG_runerror("No member named '", str, "' exists on type '", t.FullName, "'");

                    switch (m[0].MemberType) {
                        case MemberTypes.Field:
                            if (m.Length > 1)
                                L.luaG_runerror("more than one field \"", str, "\" defined on type \"", t.FullName, "\"");
                            FieldInfo fi = (FieldInfo)m[0];
                            fi.SetValue(ud, L.FromLua(value, fi.FieldType));
                            break;
                        case MemberTypes.Property:
                            if (m.Length > 1)
                                L.luaG_runerror("more than one property \"", str, "\" defined on type \"", t.FullName, "\"");
                            PropertyInfo pi = (PropertyInfo)m[0];
                            pi.SetValue(ud, L.FromLua(value, pi.PropertyType));
                            break;
                        default:
                            L.luaG_runerror("__newindex not implemented for member type '", m[0].MemberType, "'");
                            break;
                    }

                    return 0;
                }
            }

            L.luaG_runerror("unimplemented");

            return 0;
        }
    }
}
