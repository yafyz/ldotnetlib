using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;

using static lua51interp.Lua.libs.ldotnetlib.funcs;

namespace lua51interp.Lua.libs.ldotnetlib {
    public class MethodWrap {
        public object? context;
        public MemberInfo[] methods;

        MethodWrap(MemberInfo[] methods, object? context) {
            this.context = context;
            this.methods = methods;
        }

        public static TValue New(MemberInfo[] methods, object? context) {
            var mw = new MethodWrap(methods, context);

            TUserdata ud = TUserdata.New(mw);
            CClosure cl = CClosure.New(__call, $"{methods[0].ReflectedType?.FullName ?? "dynamic"}.{methods[0].Name}");
            ud.metatable = TTable.New();
            ud.metatable.RawHSet("__call", cl);
            
            return ud;
        }

        static int __call(LuaState L) {
            L.luaL_nargcheck(1);
            if (L.luaL_checktype(1, LUA_TUSERDATA).As<TUserdata>().ud is MethodWrap mw) {
                return luadn_call(L, mw.methods, L.lua_gettop()-1, mw.context);
            }
            L.luaG_runerror("expected a method");
            return 0;
        }

        public static int luadn_call(LuaState L, MemberInfo[] methods, int nargs, object? context = null) {
            var args = new object?[nargs];

            for (int i = nargs-1; i >= 0; i--)
                args[i] = L.FromLua((--L.top).v);

            var mi = FindMethod(methods, context, args);

            if (mi == null)
                L.luaG_runerror("No method overload found for arguments (", string.Join(", ", args.Select(x => x.GetType().FullName)), ")");

            if (isStaticWithThis(mi, context)) {
                args = args.Prepend(context).ToArray();
                context = null;
            }

            try {
                var ret = InvokeMethod(mi, context, args);
                L.top++.v = ObjectWrap.New(ret);
            } catch (TargetInvocationException ex) {
                L.luaG_runexception(ex.InnerException);
            }

            return 1;
        }

        static bool isParams(MethodBase x) { 
            var p = x.GetParameters();
            if (p.Length < 1)
                return false;
            return p[^1].IsDefined(typeof(ParamArrayAttribute), false);
        }

        static bool isStaticWithThis(MethodBase x, object? context) {
            return x.IsStatic && context != null;
        }

        public static object? cast(object? o, Type t) {
            if (o == null) {
                if (t.IsValueType)
                    throw new Exception("cannot cast null to value type");
                return null;
            }

            if (o is MethodWrap mw) {
                return mw.methods.Select(x => (MethodInfo)x)
                        .Select(x => x.CreateDelegate(t, mw.context))
                        .First(x => x != null);
            }

            Type ot = o.GetType();

            var op_cast = ot.GetMethods(BindingFlags.Static | bindingAttrs)
                .Union(t.GetMethods(BindingFlags.Static | bindingAttrs))
                .Where(x => x.Name == "op_Implicit" || x.Name == "op_Explicit")
                .Where(x => x.GetParameters()[0].ParameterType.IsAssignableFrom(ot))
                .Where(x => t.IsAssignableFrom(x.ReturnParameter.ParameterType))
                .FirstOrDefault();
            
            if (op_cast != null) {
                return op_cast.Invoke(null, new object[] { o });
            }

            return Convert.ChangeType(o, t);
        }

        public static object? InvokeMethod(MethodBase method, object? context, object?[] args) {
            var p = method.GetParameters();
            var pargs = args;

            if (isParams(method)) {
                    pargs = new object?[p.Length];
                object?[] vargs = (object?[])p[^1].ParameterType.GetConstructors()[0]
                                                                .Invoke(new object[] { args.Length - pargs.Length + 1 });
                var varg_type = p[^1].ParameterType.GetElementType()!;

                Array.Copy(args, pargs, pargs.Length - 1);
                Array.Copy(args, pargs.Length - 1, vargs, 0, vargs.Length);
                pargs[^1] = vargs;

                for (int i = 0; i < vargs.Length; i++)
                    if (vargs[i] != null && !varg_type.IsAssignableFrom(vargs[i].GetType()))
                        vargs[i] = cast(vargs[i], varg_type);
            }

            for (int i = 0; i < pargs.Length; i++)
                if (pargs[i] != null && !p[i].ParameterType.IsAssignableFrom(pargs[i]!.GetType()))
                    pargs[i] = cast(pargs[i], p[i].ParameterType);


            if (method.IsConstructor) {
                return ((ConstructorInfo)method).Invoke(pargs);
            } else {
                return method.Invoke(context, pargs);
            }
        }

        public static MethodBase? FindMethod(MemberInfo[] members, object? context, object?[] args) {
            var canAssign = _canAssign;
            var rankMethod = _rankMethod;

            bool _canAssign(Type t, object? o) {
                Type? ct = o?.GetType();
                if (ct == null)
                    return !t.IsValueType;
                if (o is MethodWrap mw) {
                    if (!typeof(Delegate).IsAssignableFrom(t))
                        return false;

                    var invk = t.GetMethod("Invoke")!;
                    var invk_pm = invk.GetParameters();
                    var invk_ret = invk.ReturnType;

                    foreach (var mmi in mw.methods) {
                        var mi = (MethodInfo)mmi;
                        var pm = mi.GetParameters();
                        int off = isStaticWithThis(mi, mw.context) ? 1 : 0;

                        if (!invk_ret.IsAssignableFrom(mi.ReturnType))
                            continue;

                        if (pm.Length-off != invk_pm.Length)
                            continue;

                        for (int i = 0; i < invk_pm.Length; i++)
                            if (!invk_pm[i].ParameterType.IsAssignableFrom(pm[i+off].ParameterType))
                                goto next;
                        return true;

                    next:;
                    }

                    return false;
                }
                return t.IsAssignableFrom(ct);
            }

            int _rankMethod(MethodBase mi) {
                var p = mi.GetParameters();
                int rank = 0;

                var _args = isStaticWithThis(mi, context) ? args.Prepend(context).ToArray() : args;

                bool checkType(Type t, int i) {
                    if (!canAssign(t, _args[i])) {
                        try {
                            cast(_args[i], t);
                            rank++;
                        } catch {
                            return false;
                        }
                    }
                    return true;
                }

                if (isParams(mi)) {
                    var paramsType = p[^1].ParameterType.GetElementType();

                    for (int i = 0; i < p.Length - 1; i++) {
                        if (!checkType(p[i].ParameterType, i))
                            return -1;
                    }

                    for (int i = p.Length; i < _args.Length; i++) {
                        if (!checkType(paramsType, i))
                            return -1;
                    }
                } else {
                    for (int i = 0; i < p.Length; i++) {
                        if (!checkType(p[i].ParameterType, i))
                            return -1;
                    }
                }

                return rank;
            }

            bool checknargs(MethodBase x) {
                var p = x.GetParameters();
                int off = isStaticWithThis(x, context) ? 1 : 0;

                return isParams(x)
                            ? p.Length <= args.Length+off
                            : p.Length == args.Length+off;
            }

            var list = members.AsQueryable()
                .Where(x => x is MethodBase)
                .Select(x => (MethodBase)x)
                .Where(checknargs)
                .Where(x => rankMethod(x) >= 0)
                .ToList();
            list.Sort((a, b) => rankMethod(a) - rankMethod(b));

            return list.FirstOrDefault();
        }
    }
}
