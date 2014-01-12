#region Header
// **********
// AdaptUO - PowerScroll Bag
// Author:  Orbit Storm
// Version: v1.0
// Release: 6-12-2012
// **********
// Description:
// This bag will allow players to transform collected 
// PowerScrolls into an upgraded power.
// **********
// Installation:
// Drag n' drop into your customs folder.
// [Add PowerScrollBag
// **********
// Usage:
// - Drop any PowerScroll, of any power, into the bag.
// - Scrolls dropped into the bag after, must be of the same power.
// - Powers 105/110 require 7 scrolls total.
// - Powers 115/120 require 5 scrolls total.
// - If all scrolls are of the same skill, the bag transforms them into the next power
//   of the same skill. 
//   (Ex: 7 115s of Archery, will reward a 120 of Archery; 
//        just as 7 110s of Anatomy rewards a 115 of Anatomy.)
// - If all scrolls are of the same power and belong to different skills, the bags transforms
//   them into a random skill of the next power. (i.e. 5 random 115s = a random 120)
// - To reset the bag, simply remove all scrolls from the bag.
// **********
// Other Notes:
// You may need to add additional skills if your server provides scrolls for unlisted skills.
// For instance, Herding. Feel free to remove/add/reorganize skills between Crafting and
// Non-Crafting groups, as needed.
// **********
#endregion

#region References
using System;
using Server;
using System.Collections;
using System.Collections.Generic;
using Server.Multis;
using Server.Mobiles;
using Server.Network;
#endregion

namespace Server.Items
{
	public class PowerScrollBag : BaseContainer
	{
		private double currentPower = 0;
		private int currentAcceptedSkills = 1;

		private static SkillName[][] SkillTypes = new SkillName[][]
		{ 
			// NON-Crafting Skills
			new SkillName[] 
			{
				SkillName.Anatomy,
				SkillName.AnimalLore,
				SkillName.AnimalTaming,
				SkillName.Archery,
				SkillName.Bushido,
				SkillName.Chivalry,
				SkillName.Discordance,
				SkillName.EvalInt,
				SkillName.Fencing,
				SkillName.Focus,
				SkillName.Healing,
				SkillName.Lumberjacking,
				SkillName.Macing,
				SkillName.Magery,
				SkillName.MagicResist,
				SkillName.Meditation,
				SkillName.Musicianship,
				SkillName.Mysticism,
				SkillName.Necromancy,
				SkillName.Ninjitsu,
				SkillName.Parry,
				SkillName.Peacemaking,
				SkillName.Poisoning,
				SkillName.Provocation,
				SkillName.RemoveTrap,
				SkillName.Spellweaving,
				SkillName.SpiritSpeak,
				SkillName.Stealing,
				SkillName.Stealth,
				SkillName.Swords,
				SkillName.Tactics,
				SkillName.Throwing,
				SkillName.Veterinary,
				SkillName.Wrestling
			},
            
			// Craft Skills
			new SkillName[] 
			{
				SkillName.Alchemy,
				SkillName.Blacksmith,
				SkillName.Fletching,
				SkillName.Carpentry,
				SkillName.Cartography,
				SkillName.Cooking,
				SkillName.Fishing,
				SkillName.Inscribe,
				SkillName.Imbuing,
				SkillName.Tailoring,
				SkillName.Tinkering,
				SkillName.Mining,
				SkillName.Lockpicking
			}
		};


		#region Default Config
		public override int DefaultGumpID { get { return 0x3D; } }
		public override int DefaultDropSound { get { return 0x48; } }

		public override Rectangle2D Bounds
		{
			get { return new Rectangle2D(29, 34, 108, 94); }
		}

		[Constructable]
		public PowerScrollBag() : base(0xE76)
		{
			Weight = 2.0;
			Hue = 86;
			Name = "PowerScroll Bag";
		}

		public PowerScrollBag(Serial serial) : base(serial)
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int)0); // version
            
			writer.Write( currentPower );
			writer.Write( currentAcceptedSkills );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = reader.ReadInt();
            
			currentPower = reader.ReadDouble();
			currentAcceptedSkills = reader.ReadInt();
		}
		#endregion

		public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
		{
			// BaseContainer check
			if (!base.OnDragDropInto(from, item, p))
			return false;

			// Recast Item
			PowerScroll scroll = (PowerScroll)item;
			if (scroll == null)
			return false;

			int power = (int)Math.Round(scroll.Value);

			// Check for normal values | Disabled 120 PS temporarily as it drops 125 of ANY skill.
			if (!(power == 105 || power == 110 || power == 115 ))//|| power == 120))
			return false;

			// Start counting
			return this.countPowerScrolls(from, scroll.Skill, scroll.Value);
		}

		public override bool OnDragDrop(Mobile from, Item dropped)
		{
			return this.OnDragDropInto(from, dropped, new Point3D());
		}
		
		/// Count all PowerScrolls of a given type.
		private bool countPowerScrolls(Mobile from, SkillName skill, double power)
		{
			// Check all items in bag
			int count = 0;

			count = this.Items.Count;

			if (count == 0 || count == 1)
			{
				this.currentPower = power;
               
				currentAcceptedSkills = -1;
               
				if ( Array.IndexOf(SkillTypes[0], skill) > -1 )
					currentAcceptedSkills = 0;
                  
				if ( Array.IndexOf(SkillTypes[1], skill) > -1 )
					currentAcceptedSkills = 1;
               
				if ( currentAcceptedSkills == -1 )
				{
					Say("That's a very strange scroll and it cannot be transformed using this bag.");
					return false;
				}
               
				Say("The bag now accepts all "+(currentAcceptedSkills==1?"":"non-")+"crafting scrolls with a power of " + power);
			} 
			else if (this.currentPower != power )
			{
				Say("Currently, you can only drop scrolls with the power of " + power+" in this bag.");
				return false;
			}
			else if ( Array.IndexOf(SkillTypes[currentAcceptedSkills], skill) == -1) 
			{
				Say("Currently, you can only drop "+(currentAcceptedSkills==1?"":"non-")+"crafting power scrolls in this bag.");
				return false;               
			}

			// Check transformation threshold
			this.transformPowerScrolls(from, skill, power, count);
			return true;
		}

		/**
		* Transform lesser PowerScrolls into greater ones, upon reaching the threshold.
		*
		* Transformation Thresholds:
		* 105 -> 110 PS: 7x
		* 110 -> 115 PS: 7x
		* 115 -> 120 PS: 5x
		* 120 -> 125 PS: 5x -> 120s are not usable (temporary)
		*/
		private void transformPowerScrolls(Mobile from, SkillName skill, double power, int count)
		{
			int numRequired;

			// No need for doubles
			switch ((int)(Math.Round(power)))
			{
				case 105:
				case 110:
					numRequired = 7;
				break;
               
				case 115:
				case 120:
					numRequired = 5;
				break;
				default:
					numRequired = 7;
				break;
			}
            
			if (count >= numRequired)
			{
				from.SendMessage("Upgrading power scrolls with the power of " + power);
               
				if (countSameSkills(skill, power, count))
				{
					this.DropItem(new PowerScroll(skill, ((int)power) + 5));                           
					this.removePowerScrolls(from, skill, power, numRequired);
				}
				else
				{
					this.DropItem(this.randomScroll(SkillTypes[currentAcceptedSkills], ((int)power + 5)));
					this.removePowerScrollsByGroup(from, SkillTypes[currentAcceptedSkills], power, numRequired);
				}
               
				currentPower += 5;
			}
			else
			{
				Say("Add " + (numRequired - count) + " more "+(currentAcceptedSkills==1?"":"non-")+"crafting scrolls with power of " + this.currentPower + ".");
			}
		}

		// Removes all PowerScrolls of a given group
		private void removePowerScrollsByGroup(Mobile from, SkillName[] skills, double power, int number)
		{
			int count = 0;

			for (int i = (this.Items.Count - 1); i >= 0; i--)
			{
				Item item = (Item)this.Items[i];
				if (!(item is PowerScroll))
				continue;

				PowerScroll scroll = (PowerScroll)item;
				if ((Array.IndexOf(skills, scroll.Skill) > -1) && scroll.Value == power && count < number)
				{
					count++;
					item.Delete();
				}
			}

			Effects.SendLocationParticles(EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration), 0, 0, 0, 0, 0, 5060, 0);
			Effects.PlaySound(from.Location, from.Map, 0x243);

			Effects.SendMovingParticles(new Entity(Serial.Zero, new Point3D(from.X - 6, from.Y - 6, from.Z + 15), from.Map), from, 0x36D4, 7, 0, false, true, 0x497, 0, 9502, 1, 0, (EffectLayer)255, 0x100);
			Effects.SendMovingParticles(new Entity(Serial.Zero, new Point3D(from.X - 4, from.Y - 6, from.Z + 15), from.Map), from, 0x36D4, 7, 0, false, true, 0x497, 0, 9502, 1, 0, (EffectLayer)255, 0x100);
			Effects.SendMovingParticles(new Entity(Serial.Zero, new Point3D(from.X - 6, from.Y - 4, from.Z + 15), from.Map), from, 0x36D4, 7, 0, false, true, 0x497, 0, 9502, 1, 0, (EffectLayer)255, 0x100);

			Effects.SendTargetParticles(from, 0x375A, 35, 90, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);
		}
         
		// Removes all PowerScrolls of a given type
		private void removePowerScrolls(Mobile from, SkillName skill, double power, int number)
		{
			this.removePowerScrollsByGroup(from, new SkillName[] { skill }, power, number);
		}

		/*
		* Checks if all the scrolls are of the given skill/power, and if there are enough available.
		* Returns true if conditions are met, false if not.
		*/
		private bool countSameSkills(SkillName skill, double power, int needed)
		{
			int count = 0;
			foreach (Item i in this.Items)
			{
				// If this item is a PowerScroll
				if (i is PowerScroll)
				{
					// Recast Item
					PowerScroll scroll = (PowerScroll)i;

					// If this PowerScroll is the same as the one that was dropped
					if (scroll.Skill == skill && scroll.Value == power)
					{
						// Add one
						count++;
					}

					if (count >= needed)
					{
						return true;
					}
				}
			}

			return false;
		}

		/*
		* Generate a random scroll with the given power, and picking its skill from the given list.
		*/
		private PowerScroll randomScroll(SkillName[] skills, double power)
		{
			return new PowerScroll(SkillTypes[currentAcceptedSkills][Utility.Random(skills.Length)], power);
		}

		/*
		* Make the bag say the string.
		*/
		public void Say(string args)
		{
			PublicOverheadMessage(MessageType.Regular, 0x3B2, false, args);
		}
	}
}