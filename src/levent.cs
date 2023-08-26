using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace lua51interp.Lua.libs.ldotnetlib {
    internal class levent {
        public class EventWrap {
            public EventInfo ei;
            public object? ctx;

            EventWrap(EventInfo ei, object? ctx) {
                this.ei = ei;
                this.ctx = ctx;
            }

            public static TUserdata New(EventInfo ei, object? ctx) {
                return TUserdata.New(new EventWrap(ei, ctx));
            }
        }

        public static void Open(LuaState L) {
            TTable ev = TTable.New();
            L.l_gt.RawHSet("event", ev);

            ev.RawHSet("add", CClosure.New(add, "levent.add"));
            ev.RawHSet("remove", CClosure.New(remove, "levent.remove"));
        }

        public static int remove(LuaState L) {
            (EventWrap evw, MethodWrap mw) = get_args(L);

            var efi = (Delegate)evw.ei.DeclaringType!
                            .GetField(evw.ei.Name, BindingFlags.NonPublic | BindingFlags.GetField | (evw.ctx == null ? BindingFlags.Static : BindingFlags.Instance))!
                            .GetValue(evw.ctx)!;

            foreach (var d in efi.GetInvocationList()) {
                if (d.Target != mw.context)
                    continue;

                int mtdtk;
                try {
                    mtdtk = d.Method.MetadataToken;
                } catch (InvalidOperationException e) {
                    // quick check for lua methods, unique context per method is guaranteed
                    if (d.Target is lmethod.luamethodctx) {
                        evw.ei.RemoveEventHandler(evw.ctx, d);
                    } else {
                        // todo: implement atleast some partial comparison when MetadataToken is not available
                        throw e;
                    }
                    continue;
                }

                if (mw.methods.Any(x => { try { return mtdtk == x.MetadataToken; } catch { return false; } }))
                    evw.ei.RemoveEventHandler(evw.ctx, d);
            }

            return 0;
        }

        public static int add(LuaState L) {
            (EventWrap evw, MethodWrap mw) = get_args(L);

            Delegate? mb = null;
            
            foreach (var l in mw.methods) {
                try {
                    mb = ((MethodInfo)l).CreateDelegate(evw.ei.EventHandlerType, mw.context);
                } catch { }
            }

            if (mb == null)
                L.luaG_runerror("no method overload matches (todo): todo");

            evw.ei.AddEventHandler(evw.ctx, mb);

            return 0;
        }

        public static (EventWrap, MethodWrap) get_args(LuaState L) {
            L.luaL_nargcheck(2);
            object? ud = L.luaL_checktype(1, LUA_TUSERDATA).As<TUserdata>().ud;
            object? listn = L.luaL_checktype(2, LUA_TUSERDATA).As<TUserdata>().ud;

            if (ud is not EventWrap)
                L.luaG_runerror("argument 1 expected event");

            if (listn is not MethodWrap)
                L.luaG_runerror("argument 2 expected method");

            return ((EventWrap)ud, (MethodWrap)listn);
        }
    }
}
