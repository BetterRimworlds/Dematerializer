﻿using System.Collections.Generic;
using Verse;

namespace BetterRimworlds.Dematerializer
{
    public class DematerializerBuffer : ThingOwner
    {
        List<Thing> bufferedThingsList = new List<Thing>();

        public DematerializerBuffer(IThingHolder owner): base(owner)
        {
            this.maxStacks = 999999;
            this.contentsLookMode = LookMode.Deep;
        }
        
        public override int Count
        {
            get
            {
                return bufferedThingsList.Count;
            }
        }

        public override int TryAdd(Thing item, int count, bool canMergeWithExistingStacks = true)
        {
            this.bufferedThingsList.Add(item);

            return count;
        }

        public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
        {
            this.bufferedThingsList.Add(item);

            if (item is Pawn pawn)
            {
                pawn.DeSpawn();
            }
            else
            {
                item.DeSpawn();
            }

            return true;
        }

        public override int IndexOf(Thing item)
        {
            return this.bufferedThingsList.IndexOf(item);
        }

        public override bool Remove(Thing item)
        {
            return this.bufferedThingsList.Remove(item);
        }

        protected override Thing GetAt(int index)
        {
            return this.bufferedThingsList[index];
        }
    }
}