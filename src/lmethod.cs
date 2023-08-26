using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;

namespace lua51interp.Lua.libs.ldotnetlib {
    internal class lmethod {
        public static void Open(LuaState L) {
            L.l_gt.RawHSet("method", CClosure.New(method, "dn.method"));
        }

        public class luamethodctx {
            public LuaState owning_state;
            public Closure func;
        }

        static int method(LuaState L) {
            L.luaL_nargcheck(1);
            Closure cl = (Closure)L.luaL_checktype(1, LUA_TFUNCTION);
            TTable? ltypes = (TTable?)L.luaL_checktypeopt(2, LUA_TTABLE);
            TUserdata? lret = (TUserdata?)L.luaL_getopt(3);
            string name = ((TString?)L.luaL_checktypeopt(4, LUA_TSTRING))?.str ?? $"{L.luaL_where(1)}_no_name";

            Type[] ptypes;
            if (ltypes != null) {
                ptypes = ltypes.RawIPairs().Select(kv => LGetType(L, kv.Value)).Prepend(typeof(luamethodctx)).ToArray();
            } else if (!cl.isC) {
                LClosure lc = (LClosure)cl;
                ptypes = new Type[lc.p.numparams+1];
                ptypes[0] = typeof(luamethodctx);
                Array.Fill(ptypes, typeof(object), 1, ptypes.Length-1);
            } else {
                ptypes = new Type[] { typeof(luamethodctx) };
            }

            Type ret = lret != null ? LGetType(L, lret!) : typeof(void);

            DynamicMethod dm = new DynamicMethod(name, ret, ptypes, typeof(luamethodctx), true);
            ILGenerator il = dm.GetILGenerator();

            BindingFlags pub_static = BindingFlags.Public | BindingFlags.Static;
            FieldInfo lua_state_top = typeof(LuaState).GetField("top")!;
            FieldInfo stkid_v = typeof(StkId).GetField("v")!;
            FieldInfo stkid_next = typeof(StkId).GetField("next")!;
            FieldInfo stkid_prev = typeof(StkId).GetField("prev")!;

            // this = luamethodctx
            // stack: top -> bottom
            
            il.DeclareLocal(typeof(LuaState));

            il.Emit(OpCodes.Ldarg_0);                                                       // stack: ctx
            il.Emit(OpCodes.Ldfld, typeof(luamethodctx).GetField("owning_state")!);         // stack: owning_state
            il.EmitCall(OpCodes.Call, typeof(LuaState).GetMethod("luaE_newthread")!, null); // stack: new_state
            
            il.Emit(OpCodes.Dup);                                           // stack: new_state, new_state
            il.Emit(OpCodes.Stloc_0);                                       // stack: new_state
            
            il.Emit(OpCodes.Dup);                                           // stack: new_state, new_state
            il.Emit(OpCodes.Ldfld, lua_state_top);                          // stack: new_state.top, new_state

            il.Emit(OpCodes.Dup);                                           // stack: new_state.top, new_state.top, new_state
            il.Emit(OpCodes.Ldarg_0);                                       // stack: ctx, new_state.top, new_state.top, new_state
            il.Emit(OpCodes.Ldfld, typeof(luamethodctx).GetField("func")!); // stack: ctx.func, new_state.top, new_state.top, new_state
            il.Emit(OpCodes.Stfld, stkid_v);                                // stack: new_state.top, new_state
            il.Emit(OpCodes.Ldfld, stkid_next);                             // stack: new_state.next, new_state
            
            for (int i = 1; i < ptypes.Length; i++) {
                il.Emit(OpCodes.Dup);                                       // stack: new_state.top, new_state.top, new_state
                il.Emit(OpCodes.Ldarg, i);                                  // stack: arg, new_state.top, new_state.top, new_state
                if (ptypes[i].IsValueType)
                    il.Emit(OpCodes.Box, ptypes[i]);                                   // stack: box(arg), new_state.top, new_state.top, new_state
                il.EmitCall(OpCodes.Call, typeof(ObjectWrap).GetMethod("New")!, null); // stack: ow(arg), new_state.top, new_state.top, new_state
                il.Emit(OpCodes.Stfld, stkid_v);                            // new_state.top, new_state
                il.Emit(OpCodes.Ldfld, stkid_next);                         // new_state.top+1, new_state
            }

            il.Emit(OpCodes.Stfld, lua_state_top);          // stack: empty
            
            il.Emit(OpCodes.Ldloc_0);                       // stack: new_state
            il.Emit(OpCodes.Ldc_I4, ptypes.Length-1);       // stack: nargs, new_state
            il.Emit(OpCodes.Ldc_I4, lret != null ? 1 : 0);  // stack: nres, nargs, new_state
            il.EmitCall(OpCodes.Call, typeof(lapi).GetMethod("lua_call", pub_static)!, null); // stack: empty
            
            if (ret != typeof(void)) {
                il.Emit(OpCodes.Ldloc_0);               // stack: new_state
                il.Emit(OpCodes.Dup);                   // stack: new_state, new_state
                il.Emit(OpCodes.Ldfld, lua_state_top);  // stack: new_state.top, new_state
                il.Emit(OpCodes.Ldfld, stkid_prev);     // stack: new_state.top-1, new_state
                il.Emit(OpCodes.Stfld, lua_state_top);  // stack: empty
            
                il.Emit(OpCodes.Ldloc_0);               // stack: new_state
                il.Emit(OpCodes.Dup);                   // stack: new_state, new_state
                il.Emit(OpCodes.Ldfld, lua_state_top);  // stack: new_state.top, new_state
                il.Emit(OpCodes.Ldfld, stkid_v);        // stack: tvalue, new_state
            
                il.EmitCall(OpCodes.Call, typeof(funcs)
                    .GetMethods(pub_static)
                    .Where(x => x.Name == "FromLua")
                    .Where(x => x.ContainsGenericParameters == false)
                    .Where(x => x.GetParameters()[0].ParameterType == typeof(LuaState) && x.GetParameters()[1].ParameterType == typeof(TValue))
                    .First(), null); // stack: box(retval)

                if (ret.IsValueType) {
                    if (ltypeconv.is_number(ret)) {
                        il.Emit(OpCodes.Ldc_I4, (int)Type.GetTypeCode(ret)); // stack: typecode, box(retval)
                        il.EmitCall(OpCodes.Call, typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(TypeCode) })!, null); // stack: box(retval)
                    }

                    il.EmitCall(OpCodes.Call, typeof(Nullable<>).MakeGenericType(ret).GetMethod("get_Value")!, null); // stack: retval
                }
            }

            il.Emit(OpCodes.Ret);

            L.top++.v = MethodWrap.New(new MemberInfo[] { dm }, new luamethodctx { owning_state = L, func = cl });
            

            return 1;
        }

        public static Type LGetType(LuaState L, object type) {
            if (type is TString tstr)
                return funcs.ResolveType(tstr.str)
                    ?? (Type)L.luaG_runerror("no type named '", tstr.str, "' found");
            if (type is TUserdata ud && ud.ud is TypeWrap tw)
                return tw.type;

            L.luaG_runerror("type or string expected");
            return null;
        }
    }
}
