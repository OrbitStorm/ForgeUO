using System;
using System.Collections.Generic;
using Server.Network;

namespace Server.Misc
{
    public static partial class Assistants
    {
        public class Settings
        {
            public static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(30.0);// How long to wait for a handshake response before showing warning and disconnecting
            public static readonly TimeSpan DisconnectDelay = TimeSpan.FromSeconds(15.0);// How long to show warning message before they are disconnected
            public const bool Enabled = false;// Enable assistant negotiator?
            public const bool KickOnFailure = true;// When true, this will cause anyone who does not negotiate (include those not running allowed assistants at all) to be disconnected from the server
            public const string WarningMessage = "The server was unable to negotiate features with your assistant. You must download and run an updated version of <A HREF=\"http://www.runuo.com/products/assistuo\">AssistUO</A> or <A HREF=\"http://www.runuo.com/products/razor\">Razor</A>.<BR><BR>Make sure you've checked the option <B>Negotiate features with server</B>, once you have this box checked you may log in and play normally.<BR><BR>You will be disconnected shortly.";
            private static Features m_DisallowedFeatures = Features.None;

            [Flags]
            public enum Features : ulong
            {
                None = 0,

                FilterWeather = 1 << 0,  // Weather Filter
                FilterLight = 1 << 1,  // Light Filter
                SmartTarget = 1 << 2,  // Smart Last Target
                RangedTarget = 1 << 3,  // Range Check Last Target
                AutoOpenDoors = 1 << 4,  // Automatically Open Doors
                DequipOnCast = 1 << 5,  // Unequip Weapon on spell cast
                AutoPotionEquip = 1 << 6,  // Un/Re-equip weapon on potion use
                PoisonedChecks = 1 << 7,  // Block heal If poisoned/Macro If Poisoned condition/Heal or Cure self
                LoopedMacros = 1 << 8,  // Disallow Looping macros, For loops, and macros that call other macros
                UseOnceAgent = 1 << 9,  // The use once agent
                RestockAgent = 1 << 10, // The restock agent
                SellAgent = 1 << 11, // The sell agent
                BuyAgent = 1 << 12, // The buy agent
                PotionHotkeys = 1 << 13, // All potion hotkeys
                RandomTargets = 1 << 14, // All random target hotkeys (not target next, last target, target self)
                ClosestTargets = 1 << 15, // All closest target hotkeys
                OverheadHealth = 1 << 16, // Health and Mana/Stam messages shown over player's heads
                
                // AssistUO Only
                AutolootAgent = 1 << 17, // The autoloot agent
                BoneCutterAgent = 1 << 18, // The bone cutter agent
                JScriptMacros = 1 << 19, // Javascript macro engine
                AutoRemount = 1 << 20, // Auto remount after dismount

                All = 0xFFFFFFFFFFFFFFFF  // Every feature possible
            }
            public static Features DisallowedFeatures
            {
                get
                {
                    return m_DisallowedFeatures;
                }
            }
            public static void Configure()
            {
                //DisallowFeature( Features.FilterLight );
            }

            public static void DisallowFeature(Features feature)
            {
                SetDisallowed(feature, true);
            }

            public static void AllowFeature(Features feature)
            {
                SetDisallowed(feature, false);
            }

            public static void SetDisallowed(Features feature, bool value)
            {
                if (value)
                    m_DisallowedFeatures |= feature;
                else
                    m_DisallowedFeatures &= ~feature;
            }
        }

        public class Negotiator
        {
            private static Dictionary<Mobile, Timer> m_Dictionary = new Dictionary<Mobile, Timer>();
            private static TimerStateCallback OnHandshakeTimeout_Callback = new TimerStateCallback(OnHandshakeTimeout);
            private static TimerStateCallback OnForceDisconnect_Callback = new TimerStateCallback(OnForceDisconnect);
            public static void Initialize()
            {
                if (Settings.Enabled)
                {
                    EventSink.Login += new LoginEventHandler(EventSink_Login);
                    ProtocolExtensions.Register(0xFF, true, new OnPacketReceive(OnHandshakeResponse));
                }
            }

            private static void EventSink_Login(LoginEventArgs e)
            {
                Mobile m = e.Mobile;

                if (m != null && m.NetState != null && m.NetState.Running)
                {
                    Timer t;

                    m.Send(new BeginHandshake());

                    if (m_Dictionary.ContainsKey(m))
                    {
                        t = m_Dictionary[m] as Timer;

                        if (t != null && t.Running)
                            t.Stop();
                    }

                    m_Dictionary[m] = t = Timer.DelayCall(Settings.HandshakeTimeout, OnHandshakeTimeout_Callback, m);
                    t.Start();
                }
            }

            private static void OnHandshakeResponse(NetState state, PacketReader pvSrc)
            {
                pvSrc.Trace(state);

                if (state == null || state.Mobile == null || !state.Running)
                    return;

                Mobile m = state.Mobile;
                Timer t = null;

                if (m_Dictionary.ContainsKey(m))
                {
                    t = m_Dictionary[m] as Timer;

                    if (t != null)
                        t.Stop();

                    m_Dictionary.Remove(m);
                }
            }

            private static void OnHandshakeTimeout(object state)
            {
                Timer t = null;
                Mobile m = state as Mobile;

                if (m == null)
                    return;

                m_Dictionary.Remove(m);

                if (!Settings.KickOnFailure)
                {
                    Console.WriteLine("Player '{0}' failed to negotiate features.", m);
                }
                else if (m.NetState != null && m.NetState.Running)
                {
                    m.SendGump(new Gumps.WarningGump(1060635, 30720, Settings.WarningMessage, 0xFFC000, 420, 250, null, null));

                    if (m.AccessLevel <= AccessLevel.Player)
                    {
                        m_Dictionary[m] = t = Timer.DelayCall(Settings.DisconnectDelay, OnForceDisconnect_Callback, m);
                        t.Start();
                    }
                }
            }

            private static void OnForceDisconnect(object state)
            {
                if (state is Mobile)
                {
                    Mobile m = (Mobile)state;

                    if (m.NetState != null && m.NetState.Running)
                        m.NetState.Dispose();

                    m_Dictionary.Remove(m);

                    Console.WriteLine("Player {0} kicked (Failed assistant handshake)", m);
                }
            }

            private sealed class BeginHandshake : ProtocolExtension
            {
                public BeginHandshake()
                    : base(0xFE, 8)
                {
                    this.m_Stream.Write((uint)((ulong)Settings.DisallowedFeatures >> 32));
                    this.m_Stream.Write((uint)((ulong)Settings.DisallowedFeatures & 0xFFFFFFFF));
                }
            }
        }
    }
}