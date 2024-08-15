using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;

namespace CastableInUnconscious {
    [StaticConstructorOnStartup]
    public class CastableInUnconscious {
        static CastableInUnconscious() {
            Log.Message("[CastableInUnconscious] Now active");
            var harmony = new Harmony("kaitorisenkou.CastableInUnconscious");
            
            harmony.Patch(
                AccessTools.Method(typeof(Ability), nameof(Ability.GizmoDisabled), null, null),
                null,
                null,
                new HarmonyMethod(typeof(CastableInUnconscious), nameof(Patch_GizmoDisabled), null),
                null
                );
            
            Log.Message("[CastableInUnconscious] Harmony patch complete!");
        }

        static IEnumerable<CodeInstruction> Patch_GizmoDisabled(IEnumerable<CodeInstruction> instructions) {
            var instructionList = instructions.ToList();
            int patchCount = 0;
            MethodInfo targetInfo = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Downed));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Callvirt && (MethodInfo)instructionList[i].operand == targetInfo) {
                    instructionList.InsertRange(i + 2, new CodeInstruction[]{
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(CastableInUnconscious),nameof(HasModExt))),
                        new CodeInstruction(OpCodes.Brtrue_S, instructionList[i+1].operand)
                    });
                    patchCount++;
                    break;
                }
            }
            return instructionList;
        }
        static bool HasModExt(Ability ability) {
            return ability.def.HasModExtension<ModExtension_CastableInUnconscious>();
        }
    }
    public class ModExtension_CastableInUnconscious : DefModExtension {
    }
}
