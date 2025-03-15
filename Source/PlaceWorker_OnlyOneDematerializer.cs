/*
 * This file is part of Dematerializer, a Better Rimworlds Project.
 *
 * Copyright © 2024 Theodore R. Smith
 * Author: Theodore R. Smith <hopeseekr@gmail.com>
 *   GPG Fingerprint: D8EA 6E4D 5952 159D 7759  2BB4 EEB6 CE72 F441 EC41
 *   https://github.com/BetterRimworlds/Dematerializer
 *
 * This file is licensed under the Creative Commons No-Derivations v4.0 License.
 * Most rights are reserved.
 */

using System.Collections.Generic;
using Verse;

namespace BetterRimworlds.Dematerializer
{
    class PlaceWorker_OnlyOneDematerializer : PlaceWorker_OnlyOneBuilding
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            List<Thing> blueprints = map.listerThings.ThingsOfDef(checkingDef.blueprintDef);
            List<Thing> frames = map.listerThings.ThingsOfDef(checkingDef.frameDef);
            if (
                ((blueprints != null) && (blueprints.Count > 0))
               || ((frames != null) && (frames.Count > 0))
               || map.listerBuildings.ColonistsHaveBuilding(ThingDef.Named(checkingDef.defName))
               || map.listerBuildings.ColonistsHaveBuilding(ThingDef.Named("Dematerializer"))
               )
            {
                return "You can only build one Dematerializer per map.";
            }

            return true;
        }
    }
}
