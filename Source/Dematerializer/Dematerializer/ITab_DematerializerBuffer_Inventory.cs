using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterRimworlds.Dematerializer
{
    public class ITab_DematerializerBuffer : ITab_ContentsBase
    {
        public override IList<Thing> container
        {
            get
            {
                var stargate = base.SelThing as Building_Dematerializer;

                return stargate.GetDirectlyHeldThings();
            }
        }

        public ITab_DematerializerBuffer()
        {
            labelKey = "TabCasketContents";
            containedItemsKey = "ContainedItems";
            canRemoveThings = false;
        }
    }
}
