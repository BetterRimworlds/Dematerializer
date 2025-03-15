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

using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BetterRimworlds.Dematerializer
{
    public class DematerializerBuffer : ThingOwner<Thing>, IList<Thing>
    {
        private float storedMass = 0.0f;

        Thing IList<Thing>.this[int index]
        {
            get => this.GetAt(index);
            set => throw new InvalidOperationException("ThingOwner doesn't allow setting individual elements.");
        }

        private IntVec3 Position;

        public DematerializerBuffer(IThingHolder owner, IntVec3 position, bool oneStackOnly, LookMode contentsLookMode = LookMode.Deep) :
            base(owner, oneStackOnly, contentsLookMode)
        {
            this.maxStacks = 5000;
            this.contentsLookMode = LookMode.Deep;
            this.Position = position;
        }

        public DematerializerBuffer(IThingHolder owner): base(owner)
        {
            this.maxStacks = 5000;
            this.contentsLookMode = LookMode.Deep;
        }

        public void Init()
        {
            this.calculateStoredMass();
            Log.Warning("Total stored mass: " + this.storedMass + " kg");
            this.SetRequiredDematerializerPower();
        }

        
        public float findThingMass(Thing thing)
        {
            return thing.GetStatValue(StatDefOf.Mass) * thing.stackCount;
        }

        private void calculateStoredMass()
        {
            foreach (var thing in this.InnerListForReading)
            {
                float mass = this.findThingMass(thing);

                // Log.Message("Thing (" + thing.def.defName + ") = " + mass + " kg");
                this.storedMass += mass;
            }
        }
        
        public float GetStoredMass()
        {
            return this.storedMass;
        }

        public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
        {
            this.storedMass += this.findThingMass(item);
            Log.Warning("Item Mass: " + this.findThingMass(item) + " kg");
            Log.Warning("Total Storaged Mass: " + this.storedMass + " kg");
            this.SetRequiredDematerializerPower();

            // Increase the maxStacks size for every Pawn, as they don't affect the dispersion area.
            // if (item is Pawn)
            // {
            //     ++this.maxStacks;
            // }

            if (item is Pawn pawn)
            {
                ++this.maxStacks;
            }
            else
            {
                if (this.InnerListForReading.Count >= this.maxStacks)
                {
                    return false;
                }
            }

            // Clear its existing Holder (the actual Stargate).
            item.holdingOwner = null;
            if (!base.TryAdd(item, canMergeWithExistingStacks))
            {
                return false;
            }

            item.DeSpawn();

            return true;
        }

        public int getMaxStacks()
        {
            return this.maxStacks;
        }
        
        private bool SetRequiredDematerializerPower()
        {
            var stargate = (Building_Dematerializer)this.owner;

            float requiredWatts = this.storedMass * 2;
            if (requiredWatts > 0)
            {
                return stargate.UpdateRequiredPower(1000 + requiredWatts);
            }

            return true;
        }

        public void DestroyLeastMassive()
        {
            this.InnerListForReading.Sort((x, y) => 
                this.findThingMass(y).CompareTo(this.findThingMass(x)));
            var mostMassive = this.InnerListForReading.Pop();
            this.storedMass -= this.findThingMass(mostMassive);
            
            Messages.Message("Due to lack of power, the Dematerializer lost " + mostMassive.Label + " x" + mostMassive.stackCount, MessageTypeDefOf.NegativeEvent);
            this.SetRequiredDematerializerPower();

            if (this.storedMass < 0)
            {
                this.storedMass = 0;
            }
        }
    }
}
