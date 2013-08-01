using System;
using System.Collections;
using Server.Factions;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.Ninjitsu;
using Server.Spells.Seventh;
using Server.Targeting;

namespace Server.SkillHandlers
{
    public class Stealing
    {
        public static void Initialize()
        {
            SkillInfo.Table[33].Callback = new SkillUseCallback(OnUse);
        }

        public static readonly bool ClassicMode = false;
        public static readonly bool SuspendOnMurder = false;

        public static bool IsInGuild(Mobile m)
        {
            return (m is PlayerMobile && ((PlayerMobile)m).NpcGuild == NpcGuild.ThievesGuild);
        }

        public static bool IsInnocentTo(Mobile from, Mobile to)
        {
            return (Notoriety.Compute(from, (Mobile)to) == Notoriety.Innocent);
        }

        private class StealingTarget : Target
        {
            private readonly Mobile m_Thief;

            public StealingTarget(Mobile thief)
                : base(1, false, TargetFlags.None)
            {
                this.m_Thief = thief;

                this.AllowNonlocal = true;
            }

            private Item TryStealItem(Item toSteal, ref bool caught)
            {
                Item stolen = null;

                object root = toSteal.RootParent;

                StealableArtifactsSpawner.StealableInstance si = null;
                if (toSteal.Parent == null || !toSteal.Movable)
                    si = StealableArtifactsSpawner.GetStealableInstance(toSteal);

                if (!IsEmptyHanded(this.m_Thief))
                {
                    this.m_Thief.SendLocalizedMessage(1005584); // Both hands must be free to steal.
                }
                else if (root is Mobile && ((Mobile)root).Player && !IsInGuild(this.m_Thief))
                {
                    this.m_Thief.SendLocalizedMessage(1005596); // You must be in the thieves guild to steal from other players.
                }
                else if (SuspendOnMurder && root is Mobile && ((Mobile)root).Player && IsInGuild(this.m_Thief) && this.m_Thief.Kills > 0)
                {
                    this.m_Thief.SendLocalizedMessage(502706); // You are currently suspended from the thieves guild.
                }
                else if (root is BaseVendor && ((BaseVendor)root).IsInvulnerable)
                {
                    this.m_Thief.SendLocalizedMessage(1005598); // You can't steal from shopkeepers.
                }
                else if (root is PlayerVendor)
                {
                    this.m_Thief.SendLocalizedMessage(502709); // You can't steal from vendors.
                }
                else if (!this.m_Thief.CanSee(toSteal))
                {
                    this.m_Thief.SendLocalizedMessage(500237); // Target can not be seen.
                }
                else if (this.m_Thief.Backpack == null || !this.m_Thief.Backpack.CheckHold(this.m_Thief, toSteal, false, true))
                {
                    this.m_Thief.SendLocalizedMessage(1048147); // Your backpack can't hold anything else.
                }
                #region Sigils
                else if (toSteal is Sigil)
                {
                    PlayerState pl = PlayerState.Find(this.m_Thief);
                    Faction faction = (pl == null ? null : pl.Faction);

                    Sigil sig = (Sigil)toSteal;

                    if (!this.m_Thief.InRange(toSteal.GetWorldLocation(), 1))
                    {
                        this.m_Thief.SendLocalizedMessage(502703); // You must be standing next to an item to steal it.
                    }
                    else if (root != null) // not on the ground
                    {
                        this.m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                    }
                    else if (faction != null)
                    {
                        if (!this.m_Thief.CanBeginAction(typeof(IncognitoSpell)))
                        {
                            this.m_Thief.SendLocalizedMessage(1010581); //	You cannot steal the sigil when you are incognito
                        }
                        else if (DisguiseTimers.IsDisguised(this.m_Thief))
                        {
                            this.m_Thief.SendLocalizedMessage(1010583); //	You cannot steal the sigil while disguised
                        }
                        else if (!this.m_Thief.CanBeginAction(typeof(PolymorphSpell)))
                        {
                            this.m_Thief.SendLocalizedMessage(1010582); //	You cannot steal the sigil while polymorphed				
                        }
                        else if (TransformationSpellHelper.UnderTransformation(this.m_Thief))
                        {
                            this.m_Thief.SendLocalizedMessage(1061622); // You cannot steal the sigil while in that form.
                        }
                        else if (AnimalForm.UnderTransformation(this.m_Thief))
                        {
                            this.m_Thief.SendLocalizedMessage(1063222); // You cannot steal the sigil while mimicking an animal.
                        }
                        else if (pl.IsLeaving)
                        {
                            this.m_Thief.SendLocalizedMessage(1005589); // You are currently quitting a faction and cannot steal the town sigil
                        }
                        else if (sig.IsBeingCorrupted && sig.LastMonolith.Faction == faction)
                        {
                            this.m_Thief.SendLocalizedMessage(1005590); //	You cannot steal your own sigil
                        }
                        else if (sig.IsPurifying)
                        {
                            this.m_Thief.SendLocalizedMessage(1005592); // You cannot steal this sigil until it has been purified
                        }
                        else if (this.m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, 80.0, 80.0))
                        {
                            if (Sigil.ExistsOn(this.m_Thief))
                            {
                                this.m_Thief.SendLocalizedMessage(1010258); //	The sigil has gone back to its home location because you already have a sigil.
                            }
                            else if (this.m_Thief.Backpack == null || !this.m_Thief.Backpack.CheckHold(this.m_Thief, sig, false, true))
                            {
                                this.m_Thief.SendLocalizedMessage(1010259); //	The sigil has gone home because your backpack is full
                            }
                            else
                            {
                                if (sig.IsBeingCorrupted)
                                    sig.GraceStart = DateTime.Now; // begin grace period

                                this.m_Thief.SendLocalizedMessage(1010586); // YOU STOLE THE SIGIL!!!   (woah, calm down now)

                                if (sig.LastMonolith != null && sig.LastMonolith.Sigil != null)
                                {
                                    sig.LastMonolith.Sigil = null;
                                    sig.LastStolen = DateTime.Now;
                                }

                                return sig;
                            }
                        }
                        else
                        {
                            this.m_Thief.SendLocalizedMessage(1005594); //	You do not have enough skill to steal the sigil
                        }
                    }
                    else
                    {
                        this.m_Thief.SendLocalizedMessage(1005588); //	You must join a faction to do that
                    }
                }
                #endregion
                else if (si == null && (toSteal.Parent == null || !toSteal.Movable) && !ItemFlags.GetStealable(toSteal))
                {
                    this.m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if ((toSteal.LootType == LootType.Newbied || toSteal.CheckBlessed(root)) && !ItemFlags.GetStealable(toSteal))
                {
                    this.m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (Core.AOS && si == null && toSteal is Container && !ItemFlags.GetStealable(toSteal))
                {
                    this.m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (!this.m_Thief.InRange(toSteal.GetWorldLocation(), 1))
                {
                    this.m_Thief.SendLocalizedMessage(502703); // You must be standing next to an item to steal it.
                }
                else if (si != null && this.m_Thief.Skills[SkillName.Stealing].Value < 100.0)
                {
                    this.m_Thief.SendLocalizedMessage(1060025, "", 0x66D); // You're not skilled enough to attempt the theft of this item.
                }
                else if (toSteal.Parent is Mobile)
                {
                    this.m_Thief.SendLocalizedMessage(1005585); // You cannot steal items which are equiped.
                }
                else if (root == this.m_Thief)
                {
                    this.m_Thief.SendLocalizedMessage(502704); // You catch yourself red-handed.
                }
                else if (root is Mobile && ((Mobile)root).IsStaff())
                {
                    this.m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (root is Mobile && !this.m_Thief.CanBeHarmful((Mobile)root))
                {
                }
                else if (root is Corpse)
                {
                    this.m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else
                {
                    double w = toSteal.Weight + toSteal.TotalWeight;

                    if (w > 10)
                    {
                        this.m_Thief.SendMessage("That is too heavy to steal.");
                    }
                    else
                    {
                        if (toSteal.Stackable && toSteal.Amount > 1)
                        {
                            int maxAmount = toSteal.Amount;

                            if (toSteal.Weight > 0)
                                maxAmount = (int)((this.m_Thief.Skills[SkillName.Stealing].Value / 10.0) / toSteal.Weight);

                            if (maxAmount < 1)
                                maxAmount = 1;
                            else if (maxAmount > toSteal.Amount)
                                maxAmount = toSteal.Amount;

                            int amount = Utility.RandomMinMax(1, maxAmount);

                            if (amount >= toSteal.Amount)
                            {
                                int pileWeight = (int)Math.Ceiling(toSteal.Weight * toSteal.Amount);
                                pileWeight *= 10;

                                if (this.m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, pileWeight - 22.5, pileWeight + 27.5))
                                    stolen = toSteal;
                            }
                            else
                            {
                                int pileWeight = (int)Math.Ceiling(toSteal.Weight * amount);
                                pileWeight *= 10;

                                if (this.m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, pileWeight - 22.5, pileWeight + 27.5))
                                {
                                    stolen = Mobile.LiftItemDupe(toSteal, toSteal.Amount - amount);

                                    if (stolen == null)
                                        stolen = toSteal;
                                }
                            }
                        }
                        else
                        {
                            int iw = (int)Math.Ceiling(w);
                            iw *= 10;

                            if (this.m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, iw - 22.5, iw + 27.5))
                                stolen = toSteal;
                        }

                        if (stolen != null)
                        {
                            this.m_Thief.SendLocalizedMessage(502724); // You succesfully steal the item.

                            ItemFlags.SetTaken(stolen, true);
                            ItemFlags.SetStealable(stolen, false);
                            stolen.Movable = true;

                            if (si != null)
                            {
                                toSteal.Movable = true;
                                si.Item = null;
                            }
                        }
                        else
                        {
                            this.m_Thief.SendLocalizedMessage(502723); // You fail to steal the item.
                        }

                        caught = (this.m_Thief.Skills[SkillName.Stealing].Value < Utility.Random(150));
                    }
                }

                return stolen;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                from.RevealingAction();

                Item stolen = null;
                object root = null;
                bool caught = false;

                if (target is Item)
                {
                    root = ((Item)target).RootParent;
                    stolen = this.TryStealItem((Item)target, ref caught);
                }
                else if (target is Mobile)
                {
                    Container pack = ((Mobile)target).Backpack;

                    if (pack != null && pack.Items.Count > 0)
                    {
                        int randomIndex = Utility.Random(pack.Items.Count);

                        root = target;
                        stolen = this.TryStealItem(pack.Items[randomIndex], ref caught);
                    }
                }
                else
                {
                    this.m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }

                if (stolen != null)
                {
                    from.AddToBackpack(stolen);

                    StolenItem.Add(stolen, this.m_Thief, root as Mobile);
                }

                if (caught)
                {
                    if (root == null)
                    {
                        this.m_Thief.CriminalAction(false);
                    }
                    else if (root is Corpse && ((Corpse)root).IsCriminalAction(this.m_Thief))
                    {
                        this.m_Thief.CriminalAction(false);
                    }
                    else if (root is Mobile)
                    {
                        Mobile mobRoot = (Mobile)root;

                        if (!IsInGuild(mobRoot) && IsInnocentTo(this.m_Thief, mobRoot))
                            this.m_Thief.CriminalAction(false);

                        string message = String.Format("You notice {0} trying to steal from {1}.", this.m_Thief.Name, mobRoot.Name);

                        foreach (NetState ns in this.m_Thief.GetClientsInRange(8))
                        {
                            if (ns.Mobile != this.m_Thief)
                                ns.Mobile.SendMessage(message);
                        }
                    }
                }
                else if (root is Corpse && ((Corpse)root).IsCriminalAction(this.m_Thief))
                {
                    this.m_Thief.CriminalAction(false);
                }

                if (root is Mobile && ((Mobile)root).Player && this.m_Thief is PlayerMobile && IsInnocentTo(this.m_Thief, (Mobile)root) && !IsInGuild((Mobile)root))
                {
                    PlayerMobile pm = (PlayerMobile)this.m_Thief;

                    pm.PermaFlags.Add((Mobile)root);
                    pm.Delta(MobileDelta.Noto);
                }
            }
        }

        public static bool IsEmptyHanded(Mobile from)
        {
            if (from.FindItemOnLayer(Layer.OneHanded) != null)
                return false;

            if (from.FindItemOnLayer(Layer.TwoHanded) != null)
                return false;

            return true;
        }

        public static TimeSpan OnUse(Mobile m)
        {
            if (!IsEmptyHanded(m))
            {
                m.SendLocalizedMessage(1005584); // Both hands must be free to steal.
            }
            else
            {
                m.Target = new Stealing.StealingTarget(m);
                m.RevealingAction();

                m.SendLocalizedMessage(502698); // Which item do you want to steal?
            }

            return TimeSpan.FromSeconds(10.0);
        }
    }

    public class StolenItem
    {
        public static readonly TimeSpan StealTime = TimeSpan.FromMinutes(2.0);

        private readonly Item m_Stolen;
        private readonly Mobile m_Thief;
        private readonly Mobile m_Victim;
        private DateTime m_Expires;

        public Item Stolen
        {
            get
            {
                return this.m_Stolen;
            }
        }
        public Mobile Thief
        {
            get
            {
                return this.m_Thief;
            }
        }
        public Mobile Victim
        {
            get
            {
                return this.m_Victim;
            }
        }
        public DateTime Expires
        {
            get
            {
                return this.m_Expires;
            }
        }

        public bool IsExpired
        {
            get
            {
                return (DateTime.Now >= this.m_Expires);
            }
        }

        public StolenItem(Item stolen, Mobile thief, Mobile victim)
        {
            this.m_Stolen = stolen;
            this.m_Thief = thief;
            this.m_Victim = victim;

            this.m_Expires = DateTime.Now + StealTime;
        }

        private static readonly Queue m_Queue = new Queue();

        public static void Add(Item item, Mobile thief, Mobile victim)
        {
            Clean();

            m_Queue.Enqueue(new StolenItem(item, thief, victim));
        }

        public static bool IsStolen(Item item)
        {
            Mobile victim = null;

            return IsStolen(item, ref victim);
        }

        public static bool IsStolen(Item item, ref Mobile victim)
        {
            Clean();

            foreach (StolenItem si in m_Queue)
            {
                if (si.m_Stolen == item && !si.IsExpired)
                {
                    victim = si.m_Victim;
                    return true;
                }
            }

            return false;
        }

        public static void ReturnOnDeath(Mobile killed, Container corpse)
        {
            Clean();

            foreach (StolenItem si in m_Queue)
            {
                if (si.m_Stolen.RootParent == corpse && si.m_Victim != null && !si.IsExpired)
                {
                    if (si.m_Victim.AddToBackpack(si.m_Stolen))
                        si.m_Victim.SendLocalizedMessage(1010464); // the item that was stolen is returned to you.
                    else
                        si.m_Victim.SendLocalizedMessage(1010463); // the item that was stolen from you falls to the ground.

                    si.m_Expires = DateTime.Now; // such a hack
                }
            }
        }

        public static void Clean()
        {
            while (m_Queue.Count > 0)
            {
                StolenItem si = (StolenItem)m_Queue.Peek();

                if (si.IsExpired)
                    m_Queue.Dequeue();
                else
                    break;
            }
        }
    }
}