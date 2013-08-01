using System;
using Server.Items;

namespace Server.Mobiles
{
    public class ArcherGuard : BaseGuard
    {
        private Timer m_AttackTimer, m_IdleTimer;
        private Mobile m_Focus;
        [Constructable]
        public ArcherGuard()
            : this(null)
        {
        }

        public ArcherGuard(Mobile target)
            : base(target)
        {
            this.InitStats(100, 125, 25);
            this.Title = "the guard";

            this.SpeechHue = Utility.RandomDyedHue();

            this.Hue = Utility.RandomSkinHue();

            if (this.Female = Utility.RandomBool())
            {
                this.Body = 0x191;
                this.Name = NameList.RandomName("female");
            }
            else
            {
                this.Body = 0x190;
                this.Name = NameList.RandomName("male");
            }

            new Horse().Rider = this;

            this.AddItem(new StuddedChest());
            this.AddItem(new StuddedArms());
            this.AddItem(new StuddedGloves());
            this.AddItem(new StuddedGorget());
            this.AddItem(new StuddedLegs());
            this.AddItem(new Boots());
            this.AddItem(new SkullCap());

            Bow bow = new Bow();

            bow.Movable = false;
            bow.Crafter = this;
            bow.Quality = WeaponQuality.Exceptional;

            this.AddItem(bow);

            Container pack = new Backpack();

            pack.Movable = false;

            Arrow arrows = new Arrow(250);

            arrows.LootType = LootType.Newbied;

            pack.DropItem(arrows);
            pack.DropItem(new Gold(10, 25));

            this.AddItem(pack);

            this.Skills[SkillName.Anatomy].Base = 120.0;
            this.Skills[SkillName.Tactics].Base = 120.0;
            this.Skills[SkillName.Archery].Base = 120.0;
            this.Skills[SkillName.MagicResist].Base = 120.0;
            this.Skills[SkillName.DetectHidden].Base = 100.0;

            this.NextCombatTime = DateTime.Now + TimeSpan.FromSeconds(0.5);
            this.Focus = target;
        }

        public ArcherGuard(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override Mobile Focus
        {
            get
            {
                return this.m_Focus;
            }
            set
            {
                if (this.Deleted)
                    return;

                Mobile oldFocus = this.m_Focus;

                if (oldFocus != value)
                {
                    this.m_Focus = value;

                    if (value != null)
                        this.AggressiveAction(value);

                    this.Combatant = value;

                    if (oldFocus != null && !oldFocus.Alive)
                        this.Say("Thou hast suffered thy punishment, scoundrel.");

                    if (value != null)
                        this.Say(500131); // Thou wilt regret thine actions, swine!

                    if (this.m_AttackTimer != null)
                    {
                        this.m_AttackTimer.Stop();
                        this.m_AttackTimer = null;
                    }

                    if (this.m_IdleTimer != null)
                    {
                        this.m_IdleTimer.Stop();
                        this.m_IdleTimer = null;
                    }

                    if (this.m_Focus != null)
                    {
                        this.m_AttackTimer = new AttackTimer(this);
                        this.m_AttackTimer.Start();
                        ((AttackTimer)this.m_AttackTimer).DoOnTick();
                    }
                    else
                    {
                        this.m_IdleTimer = new IdleTimer(this);
                        this.m_IdleTimer.Start();
                    }
                }
                else if (this.m_Focus == null && this.m_IdleTimer == null)
                {
                    this.m_IdleTimer = new IdleTimer(this);
                    this.m_IdleTimer.Start();
                }
            }
        }
        public override bool OnBeforeDeath()
        {
            if (this.m_Focus != null && this.m_Focus.Alive)
                new AvengeTimer(this.m_Focus).Start(); // If a guard dies, three more guards will spawn

            return base.OnBeforeDeath();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(this.m_Focus);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch ( version )
            {
                case 0:
                    {
                        this.m_Focus = reader.ReadMobile();

                        if (this.m_Focus != null)
                        {
                            this.m_AttackTimer = new AttackTimer(this);
                            this.m_AttackTimer.Start();
                        }
                        else
                        {
                            this.m_IdleTimer = new IdleTimer(this);
                            this.m_IdleTimer.Start();
                        }

                        break;
                    }
            }
        }

        public override void OnAfterDelete()
        {
            if (this.m_AttackTimer != null)
            {
                this.m_AttackTimer.Stop();
                this.m_AttackTimer = null;
            }

            if (this.m_IdleTimer != null)
            {
                this.m_IdleTimer.Stop();
                this.m_IdleTimer = null;
            }

            base.OnAfterDelete();
        }

        private class AvengeTimer : Timer
        {
            private readonly Mobile m_Focus;
            public AvengeTimer(Mobile focus)
                : base(TimeSpan.FromSeconds(2.5), TimeSpan.FromSeconds(1.0), 3)// After 2.5 seconds, one guard will spawn every 1.0 second, three times
            {
                this.m_Focus = focus;
            }

            protected override void OnTick()
            {
                BaseGuard.Spawn(this.m_Focus, this.m_Focus, 1, true);
            }
        }

        private class AttackTimer : Timer
        {
            private readonly ArcherGuard m_Owner;
            //	private bool m_Shooting;
            public AttackTimer(ArcherGuard owner)
                : base(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(0.1))
            {
                this.m_Owner = owner;
            }

            public void DoOnTick()
            {
                this.OnTick();
            }

            protected override void OnTick()
            {
                if (this.m_Owner.Deleted)
                {
                    this.Stop();
                    return;
                }

                this.m_Owner.Criminal = false;
                this.m_Owner.Kills = 0;
                this.m_Owner.Stam = this.m_Owner.StamMax;

                Mobile target = this.m_Owner.Focus;

                if (target != null && (target.Deleted || !target.Alive || !this.m_Owner.CanBeHarmful(target)))	
                {
                    this.m_Owner.Focus = null;
                    this.Stop();
                    return;
                }
                else if (this.m_Owner.Weapon is Fists)
                {
                    this.m_Owner.Kill();
                    this.Stop();
                    return;
                }

                if (target != null && this.m_Owner.Combatant != target)
                    this.m_Owner.Combatant = target;

                if (target == null)
                {
                    this.Stop();
                }
                else
                { // <instakill>
                    this.TeleportTo(target);
                    target.BoltEffect(0);

                    if (target is BaseCreature)
                        ((BaseCreature)target).NoKillAwards = true;

                    target.Damage(target.HitsMax, this.m_Owner);
                    target.Kill(); // just in case, maybe Damage is overriden on some shard

                    if (target.Corpse != null && !target.Player)
                        target.Corpse.Delete();

                    this.m_Owner.Focus = null;
                    this.Stop();
                }// </instakill>
                /*else if ( !m_Owner.InRange( target, 20 ) )
                {
                m_Shooting = false;
                m_Owner.Focus = null;
                }
                else if ( !m_Owner.InLOS( target ) )
                {
                m_Shooting = false;
                TeleportTo( target );
                }
                else if ( !m_Owner.CanSee( target ) )
                {
                m_Shooting = false;
                if ( !m_Owner.InRange( target, 2 ) )
                {
                if ( !m_Owner.Move( m_Owner.GetDirectionTo( target ) | Direction.Running ) && OutOfMaxDistance( target ) )
                TeleportTo( target );
                }
                else
                {
                if ( !m_Owner.UseSkill( SkillName.DetectHidden ) && Utility.Random( 50 ) == 0 )
                m_Owner.Say( "Reveal!" );
                }
                }
                else
                {
                if ( m_Shooting && (TimeToSpare() || OutOfMaxDistance( target )) )
                m_Shooting = false;
                else if ( !m_Shooting && InMinDistance( target ) )
                m_Shooting = true;
                if ( !m_Shooting )
                {
                if ( m_Owner.InRange( target, 1 ) )
                {
                if ( !m_Owner.Move( (Direction)(m_Owner.GetDirectionTo( target ) - 4) | Direction.Running ) && OutOfMaxDistance( target ) ) // Too close, move away
                TeleportTo( target );
                }
                else if ( !m_Owner.InRange( target, 2 ) )
                {
                if ( !m_Owner.Move( m_Owner.GetDirectionTo( target ) | Direction.Running ) && OutOfMaxDistance( target ) )
                TeleportTo( target );
                }
                }
                }*/
            }

            private bool TimeToSpare()
            {
                return (this.m_Owner.NextCombatTime - DateTime.Now) > TimeSpan.FromSeconds(1.0);
            }

            private bool OutOfMaxDistance(Mobile target)
            {
                return !this.m_Owner.InRange(target, this.m_Owner.Weapon.MaxRange);
            }

            private bool InMinDistance(Mobile target)
            {
                return this.m_Owner.InRange(target, 4);
            }

            private void TeleportTo(Mobile target)
            {
                Point3D from = this.m_Owner.Location;
                Point3D to = target.Location;

                this.m_Owner.Location = to;

                Effects.SendLocationParticles(EffectItem.Create(from, this.m_Owner.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                Effects.SendLocationParticles(EffectItem.Create(to, this.m_Owner.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);

                this.m_Owner.PlaySound(0x1FE);
            }
        }

        private class IdleTimer : Timer
        {
            private readonly ArcherGuard m_Owner;
            private int m_Stage;
            public IdleTimer(ArcherGuard owner)
                : base(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.5))
            {
                this.m_Owner = owner;
            }

            protected override void OnTick()
            {
                if (this.m_Owner.Deleted)
                {
                    this.Stop();
                    return;
                }

                if ((this.m_Stage++ % 4) == 0 || !this.m_Owner.Move(this.m_Owner.Direction))
                    this.m_Owner.Direction = (Direction)Utility.Random(8);

                if (this.m_Stage > 16)
                {
                    Effects.SendLocationParticles(EffectItem.Create(this.m_Owner.Location, this.m_Owner.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                    this.m_Owner.PlaySound(0x1FE);

                    this.m_Owner.Delete();
                }
            }
        }
    }
}