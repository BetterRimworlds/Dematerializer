using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace Enhanced_Development.Utilities
{
    public class Utilities
    {
        public static IEnumerable<Pawn> findPawnsInColony(IntVec3 position, float radius)
        {
            //IEnumerable<Pawn> pawns = Find.ListerPawns.ColonistsAndPrisoners;
            //IEnumerable<Pawn> pawns = Find.ListerPawns.FreeColonists;
            //IEnumerable<Pawn> pawns = Find.ListerPawns.AllPawns.Where(item => item.IsColonistPlayerControlled || item.IsColonistPlayerControlled);

            IEnumerable<Pawn> pawns = Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer);

            IEnumerable<Pawn> closePawns;

            if (pawns != null)
            {
                closePawns = pawns.Where<Pawn>(t => t.Position.InHorDistOf(position, radius));
                return closePawns;
            }
            return null;
        }


        static public Thing FindItemThingsNearBuilding(Thing centerBuilding, int radius, Map map)
        {
            IEnumerable<Thing> closeThings = GenRadial.RadialDistinctThingsAround(centerBuilding.Position, map, radius, true);

            foreach (Thing tempThing in closeThings)
            {
                if (tempThing.def.category == ThingCategory.Item)
                {
                    return tempThing;
                }

            }

            return (Thing)null;
        }

    }
}

