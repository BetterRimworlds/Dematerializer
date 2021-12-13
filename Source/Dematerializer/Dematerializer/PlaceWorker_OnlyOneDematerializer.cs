﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
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
