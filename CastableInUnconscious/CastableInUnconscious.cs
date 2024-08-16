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
            harmony.Patch(
                AccessTools.Method(typeof(Job), nameof(Job.CanBeginNow), null, null),
                null,
                null,
                new HarmonyMethod(typeof(CastableInUnconscious), nameof(Patch_CanBeginNow), null),
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

        static IEnumerable<CodeInstruction> Patch_CanBeginNow(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            var instructionList = instructions.ToList();
            int patchCount = 0;
            Label newLabel = generator.DefineLabel();
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Starg_S) {
                    CodeInstruction firstInst = new CodeInstruction(OpCodes.Ldarg_0);
                    firstInst.labels = instructionList[i + 1].labels;
                    instructionList[i + 1].labels = new List<Label>() { newLabel };
                    instructionList.InsertRange(i + 1, new CodeInstruction[]{
                        firstInst,
                        new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(CastableInUnconscious),nameof(HasModExt_Job))),
                        new CodeInstruction(OpCodes.Brfalse_S, newLabel),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Starg_S, instructionList[i].operand),
                    });
                    patchCount++;
                    break;
                }
            }
            return instructionList;
        }
        static bool HasModExt_Job(Job job) {
            var ability = job.ability;
            return ability!=null && ability.def.HasModExtension<ModExtension_CastableInUnconscious>();
        }
    }
    public class ModExtension_CastableInUnconscious : DefModExtension {
    }
}
