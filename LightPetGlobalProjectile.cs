using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace ColoredLightPets
{
    internal class LightPetGlobalProjectile : GlobalProjectile
    {

        public override bool PreAI(Projectile projectile)
        {
            if (ColoredLightPets.IsLightPetProj.Contains(projectile.type))
                ColoredLightPets.CurrentProjectile = projectile;
            return true;

            Terraria.ID.ItemID.InfernalWispDye
        }

        public override void PostAI(Projectile projectile)
        {
            if (ColoredLightPets.CurrentProjectile == projectile)
                ColoredLightPets.CurrentProjectile = null;
        }
    }
}
