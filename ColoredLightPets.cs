using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace ColoredLightPets
{
    public class ColoredLightPets : Mod
    {
        public static HashSet<int> IsLightPetProj;
        public const int VanillaBuffs = 206;

        private static FieldInfo ShaderColor = typeof(ArmorShaderData).GetField("_uColor", BindingFlags.Instance | BindingFlags.NonPublic);
        internal static Projectile CurrentProjectile = null;

        public override void Load()
        {
            IL.Terraria.Projectile.ProjLight += Projectile_ProjLight;
            On.Terraria.Lighting.AddLight_int_int_float_float_float += Lighting_AddLight;

            IsLightPetProj = new HashSet<int>();

            // Vanilla light pets
            IsLightPetProj.Add(18);   // 19 
            IsLightPetProj.Add(500);  // 155
            IsLightPetProj.Add(72);   // 27 
            IsLightPetProj.Add(86);   // 101
            IsLightPetProj.Add(87);   // 102
            IsLightPetProj.Add(211);  // 57 
            IsLightPetProj.Add(650);  // 190
            IsLightPetProj.Add(492);  // 152
            IsLightPetProj.Add(702);  // 201
        }

        private void Lighting_AddLight(On.Terraria.Lighting.orig_AddLight_int_int_float_float_float orig, int i, int j, float R, float G, float B)
        {
            if (CurrentProjectile != null) ModifyLight(CurrentProjectile, ref R, ref G, ref B);
            orig(i, j, R, G, B);
        }

        public override void Unload()
        {
            IL.Terraria.Projectile.ProjLight -= Projectile_ProjLight;
            On.Terraria.Lighting.AddLight_int_int_float_float_float -= Lighting_AddLight;
        }

        public override void PostSetupContent()
        {
            Dictionary<string, int> scanResult = new Dictionary<string, int>();

            Logger.Info("Scanning mods for light pets...");

            for (int i = VanillaBuffs; i < BuffLoader.BuffCount; i++)
            {
                if (!Main.lightPet[i]) continue;
                ModBuff buff = BuffLoader.GetBuff(i);
                Type buffType = buff.GetType();

                if (ScanType(buffType))
                {
                    string name = buffType.Assembly.GetName().Name;
                    if (scanResult.ContainsKey(name)) scanResult[name]++;
                    else scanResult.Add(name, 1);
                }
            }

            foreach (KeyValuePair<string, int> mod in scanResult)
                Logger.InfoFormat("Found {0} light pets in {1}", mod.Value, mod.Key);
        }

        private bool ScanType(Type buffType)
        {
            MethodInfo update = buffType.GetMethods().FirstOrDefault(m => m.Name == "Update" && m.DeclaringType == buffType);
            if (update == null) return false;

            using (DynamicMethodDefinition def = new DynamicMethodDefinition(update))
            {
                foreach (Instruction instr in def.Definition.Body.Instructions)
                {
                    if (instr.OpCode.Code == Mono.Cecil.Cil.Code.Call
                        && instr.Operand is GenericInstanceMethod method
                        && method.Name == "ProjectileType"
                        )
                    {
                        TypeReference projectile = method.GenericArguments[0];
                        Assembly asm = buffType.Assembly;
                        Type projType = asm.GetType(projectile.FullName);

                        MethodInfo getInstance = typeof(ModContent).GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static);
                        ModProjectile proj = (ModProjectile)getInstance.MakeGenericMethod(projType).Invoke(null, new object[] { });
                        IsLightPetProj.Add(proj.projectile.type);

                        return true;
                    }
                }
            }
            return false;
        }

        private void Projectile_ProjLight(ILContext il)
        {
            int r = -1, g = -1, b = -1;

            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(
                x => x.MatchLdloc(out r),
                x => x.MatchLdloc(out g),
                x => x.MatchLdloc(out b),
                x => x.MatchCall<Lighting>("AddLight")
                )) return;

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, r);
            c.Emit(OpCodes.Ldloca, g);
            c.Emit(OpCodes.Ldloca, b);
            c.Emit<ColoredLightPets>(OpCodes.Call, "ModifyLight");
        }

        private static void ModifyLight(Projectile proj, ref float r, ref float g, ref float b)
        {
            if (!IsLightPetProj.Contains(proj.type)) return;

            ArmorShaderData shader = GameShaders.Armor.GetSecondaryShader(Main.player[proj.owner].cLight, Main.player[proj.owner]);

            if (shader == null) return;

            Vector3 uColor = (Vector3)ShaderColor.GetValue(shader);

            float l = (r + g + b) / 3;

            r = uColor.X * l;
            g = uColor.Y * l;
            b = uColor.Z * l;
        }


    }
}