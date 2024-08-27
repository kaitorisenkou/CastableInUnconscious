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
            var inner_PATGetGizmos =
                typeof(Pawn_AbilityTracker)
                .GetNestedTypes(AccessTools.all)
                .FirstOrDefault((Type t) => t.Name.Contains("<GetGizmos>"));
            harmony.Patch(
                AccessTools.Method(inner_PATGetGizmos, "MoveNext", null, null),
                null,
                null,
                new HarmonyMethod(typeof(CastableInUnconscious), nameof(Patch_PATGetGizmos), null),
                null
                );

            Log.Message("[CastableInUnconscious] Harmony patch complete!");
        }

        //-----------------------------------------------//
        //                 ...                           //
        // + if (this.pawn.Downed || HasModExt(this)){   //
        //   	reason = "CommandDisabledUnconsciou...   //
        //   	return true;                             //
        //   }                                           //
        //                 ...                           //
        //-----------------------------------------------//
        static IEnumerable<CodeInstruction> Patch_GizmoDisabled(IEnumerable<CodeInstruction> instructions) {
            var instructionList = instructions.ToList();
            int patchCount = 0;
            MethodInfo targetInfo = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Downed));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Callvirt && (MethodInfo)instructionList[i].operand == targetInfo) {
                    instructionList.InsertRange(i + 2, new CodeInstruction[]{
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(CastableInUnconscious),nameof(GetModExt_Unconscious))),
                        new CodeInstruction(OpCodes.Brtrue_S, instructionList[i+1].operand)
                    });
                    patchCount++;
                    break;
                }
            }
            if (patchCount < 1) {
                Log.Error("[CiU] Patch_GizmoDisabled seems failed!");
            }
            return instructionList;
        }

        //----------------------------------------------------------------------//
        //   public bool CanBeginNow(Pawn pawn, bool whileLyingDown = false){   //
        // 	    if (pawn.Downed) {                                              //
        // 		    whileLyingDown = true;                                      //
        // 	    }                                                               //
        // +    if (HasModExt_Job(this)) {                                      //
        // +	whileLyingDown = false;                                         //
        // +    }                                                               //
        // 	    return !whileLyingDown || this.GetCachedDriver(pawn).CanBe...   //
        //   }                                                                  //
        //----------------------------------------------------------------------//
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
                        new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(CastableInUnconscious),nameof(GetModExt_Unconscious_Job))),
                        new CodeInstruction(OpCodes.Brfalse_S, newLabel),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Starg_S, instructionList[i].operand),
                    });
                    patchCount++;
                    break;
                }
            }
            if (patchCount < 1) {
                Log.Error("[CiU] Patch_CanBeginNow seems failed!");
            }
            return instructionList;
        }

        static IEnumerable<CodeInstruction> Patch_PATGetGizmos(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {

            var instructionList = instructions.ToList();
            int patchCount = 0;
            MethodInfo targetInfo = AccessTools.PropertyGetter(typeof(DebugSettings), nameof(DebugSettings.ShowDevGizmos));
            var innerType = typeof(Pawn_AbilityTracker).GetNestedTypes(AccessTools.all).First(t => t.Name.Contains("GetGizmos"));
            FieldInfo a_field = innerType.GetFields(AccessTools.all).First(t => t.Name.Contains("<a>"));
            for (int i = 0; i < instructionList.Count; i++) {
                if (instructionList[i].opcode == OpCodes.Call && (MethodInfo)instructionList[i].operand == targetInfo) {
                    instructionList.InsertRange(i, new CodeInstruction[]{
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, a_field),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CastableInUnconscious),nameof(GetModExt_Mentalbreak))),
                        instructionList[i-1]
                    });
                    patchCount++;
                    break;
                }
            }
            if (patchCount < 1) {
                Log.Error("[CiU] Patch_PATGetGizmos seems failed!");
            }
            return instructionList;
        }

        static bool GetModExt_Unconscious(Ability ability) {
            var ext= ability.def.GetModExtension<ModExtension_CastableInUnconscious>();
            return ext!=null && ext.castableInUnconscious;
        }
        static bool GetModExt_Mentalbreak(Ability ability) {
            var ext = ability.def.GetModExtension<ModExtension_CastableInUnconscious>();
            return ext != null && ext.castableInMentalbreak;
        }
        static bool GetModExt_Unconscious_Job(Job job) {
            var ability = job.ability;
            return ability != null && GetModExt_Unconscious(ability);
        }
    }
    public class ModExtension_CastableInUnconscious : DefModExtension {
        [DefaultValue(true)]
        public bool castableInUnconscious = true;
        [DefaultValue(true)]
        public bool castableInMentalbreak = true;
    }
}
