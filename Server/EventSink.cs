using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Server.Accounting;
using Server.Commands;
using Server.Guilds;
using Server.Network;

namespace Server
{
    public delegate void CharacterCreatedEventHandler(CharacterCreatedEventArgs e);
    public delegate void OpenDoorMacroEventHandler(OpenDoorMacroEventArgs e);
    public delegate void SpeechEventHandler(SpeechEventArgs e);
    public delegate void LoginEventHandler(LoginEventArgs e);
    public delegate void ServerListEventHandler(ServerListEventArgs e);
    public delegate void MovementEventHandler(MovementEventArgs e);
    public delegate void HungerChangedEventHandler(HungerChangedEventArgs e);
    public delegate void CrashedEventHandler(CrashedEventArgs e);
    public delegate void ShutdownEventHandler(ShutdownEventArgs e);
    public delegate void HelpRequestEventHandler(HelpRequestEventArgs e);
    public delegate void DisarmRequestEventHandler(DisarmRequestEventArgs e);
    public delegate void StunRequestEventHandler(StunRequestEventArgs e);
    public delegate void OpenSpellbookRequestEventHandler(OpenSpellbookRequestEventArgs e);
    public delegate void CastSpellRequestEventHandler(CastSpellRequestEventArgs e);
    public delegate void AnimateRequestEventHandler(AnimateRequestEventArgs e);
    public delegate void LogoutEventHandler(LogoutEventArgs e);
    public delegate void SocketConnectEventHandler(SocketConnectEventArgs e);
    public delegate void ConnectedEventHandler(ConnectedEventArgs e);
    public delegate void DisconnectedEventHandler(DisconnectedEventArgs e);
    public delegate void RenameRequestEventHandler(RenameRequestEventArgs e);
    public delegate void PlayerDeathEventHandler(PlayerDeathEventArgs e);
    public delegate void VirtueGumpRequestEventHandler(VirtueGumpRequestEventArgs e);
    public delegate void VirtueItemRequestEventHandler(VirtueItemRequestEventArgs e);
    public delegate void VirtueMacroRequestEventHandler(VirtueMacroRequestEventArgs e);
    public delegate void ChatRequestEventHandler(ChatRequestEventArgs e);
    public delegate void AccountLoginEventHandler(AccountLoginEventArgs e);
    public delegate void PaperdollRequestEventHandler(PaperdollRequestEventArgs e);
    public delegate void ProfileRequestEventHandler(ProfileRequestEventArgs e);
    public delegate void ChangeProfileRequestEventHandler(ChangeProfileRequestEventArgs e);
    public delegate void AggressiveActionEventHandler(AggressiveActionEventArgs e);
    public delegate void GameLoginEventHandler(GameLoginEventArgs e);
    public delegate void DeleteRequestEventHandler(DeleteRequestEventArgs e);
    public delegate void WorldLoadEventHandler();
    public delegate void WorldSaveEventHandler(WorldSaveEventArgs e);
    public delegate void SetAbilityEventHandler(SetAbilityEventArgs e);
    public delegate void FastWalkEventHandler(FastWalkEventArgs e);
    public delegate void ServerStartedEventHandler();
    public delegate BaseGuild CreateGuildHandler(CreateGuildEventArgs e);
    public delegate void GuildGumpRequestHandler(GuildGumpRequestArgs e);
    public delegate void QuestGumpRequestHandler(QuestGumpRequestArgs e);
    public delegate void ClientVersionReceivedHandler(ClientVersionReceivedArgs e);
    public delegate void OnKilledByEventHandler(OnKilledByEventArgs e);
    public delegate void OnItemUseEventHandler(OnItemUseEventArgs e);
    public delegate void OnEnterRegionEventHandler(OnEnterRegionEventArgs e);
    public delegate void OnConsumeEventHandler(OnConsumeEventArgs e);

    public struct SkillNameValue
    {
        private readonly SkillName m_Name;
        private readonly int m_Value;
        public SkillNameValue(SkillName name, int value)
        {
            this.m_Name = name;
            this.m_Value = value;
        }

        public SkillName Name
        {
            get
            {
                return this.m_Name;
            }
        }
        public int Value
        {
            get
            {
                return this.m_Value;
            }
        }
    }

    public static class EventSink
    {
        public static event CharacterCreatedEventHandler CharacterCreated;
        public static event OpenDoorMacroEventHandler OpenDoorMacroUsed;
        public static event SpeechEventHandler Speech;
        public static event LoginEventHandler Login;
        public static event ServerListEventHandler ServerList;
        public static event MovementEventHandler Movement;
        public static event HungerChangedEventHandler HungerChanged;
        public static event CrashedEventHandler Crashed;
        public static event ShutdownEventHandler Shutdown;
        public static event HelpRequestEventHandler HelpRequest;
        public static event DisarmRequestEventHandler DisarmRequest;
        public static event StunRequestEventHandler StunRequest;
        public static event OpenSpellbookRequestEventHandler OpenSpellbookRequest;
        public static event CastSpellRequestEventHandler CastSpellRequest;
        public static event AnimateRequestEventHandler AnimateRequest;
        public static event LogoutEventHandler Logout;
        public static event SocketConnectEventHandler SocketConnect;
        public static event ConnectedEventHandler Connected;
        public static event DisconnectedEventHandler Disconnected;
        public static event RenameRequestEventHandler RenameRequest;
        public static event PlayerDeathEventHandler PlayerDeath;
        public static event VirtueGumpRequestEventHandler VirtueGumpRequest;
        public static event VirtueItemRequestEventHandler VirtueItemRequest;
        public static event VirtueMacroRequestEventHandler VirtueMacroRequest;
        public static event ChatRequestEventHandler ChatRequest;
        public static event AccountLoginEventHandler AccountLogin;
        public static event PaperdollRequestEventHandler PaperdollRequest;
        public static event ProfileRequestEventHandler ProfileRequest;
        public static event ChangeProfileRequestEventHandler ChangeProfileRequest;
        public static event AggressiveActionEventHandler AggressiveAction;
        public static event CommandEventHandler Command;
        public static event GameLoginEventHandler GameLogin;
        public static event DeleteRequestEventHandler DeleteRequest;
        public static event WorldLoadEventHandler WorldLoad;
        public static event WorldSaveEventHandler WorldSave;
        public static event SetAbilityEventHandler SetAbility;
        public static event FastWalkEventHandler FastWalk;
        public static event CreateGuildHandler CreateGuild;
        public static event ServerStartedEventHandler ServerStarted;
        public static event GuildGumpRequestHandler GuildGumpRequest;
        public static event QuestGumpRequestHandler QuestGumpRequest;
        public static event ClientVersionReceivedHandler ClientVersionReceived;
        public static event OnKilledByEventHandler OnKilledBy;
        public static event OnItemUseEventHandler OnItemUse;
        public static event OnEnterRegionEventHandler OnEnterRegion;
        public static event OnConsumeEventHandler OnConsume;

        public static void InvokeClientVersionReceived(ClientVersionReceivedArgs e)
        {
            if (ClientVersionReceived != null)
                ClientVersionReceived(e);
        }

        public static void InvokeServerStarted()
        {
            if (ServerStarted != null)
                ServerStarted();
        }

        public static BaseGuild InvokeCreateGuild(CreateGuildEventArgs e)
        {
            if (CreateGuild != null)
                return CreateGuild(e);
            else
                return null;
        }

        public static void InvokeSetAbility(SetAbilityEventArgs e)
        {
            if (SetAbility != null)
                SetAbility(e);
        }

        public static void InvokeGuildGumpRequest(GuildGumpRequestArgs e)
        {
            if (GuildGumpRequest != null)
                GuildGumpRequest(e);
        }

        public static void InvokeQuestGumpRequest(QuestGumpRequestArgs e)
        {
            if (QuestGumpRequest != null)
                QuestGumpRequest(e);
        }

        public static void InvokeFastWalk(FastWalkEventArgs e)
        {
            if (FastWalk != null)
                FastWalk(e);
        }

        public static void InvokeDeleteRequest(DeleteRequestEventArgs e)
        {
            if (DeleteRequest != null)
                DeleteRequest(e);
        }

        public static void InvokeGameLogin(GameLoginEventArgs e)
        {
            if (GameLogin != null)
                GameLogin(e);
        }

        public static void InvokeCommand(CommandEventArgs e)
        {
            if (Command != null)
                Command(e);
        }

        public static void InvokeAggressiveAction(AggressiveActionEventArgs e)
        {
            if (AggressiveAction != null)
                AggressiveAction(e);
        }

        public static void InvokeProfileRequest(ProfileRequestEventArgs e)
        {
            if (ProfileRequest != null)
                ProfileRequest(e);
        }

        public static void InvokeChangeProfileRequest(ChangeProfileRequestEventArgs e)
        {
            if (ChangeProfileRequest != null)
                ChangeProfileRequest(e);
        }

        public static void InvokePaperdollRequest(PaperdollRequestEventArgs e)
        {
            if (PaperdollRequest != null)
                PaperdollRequest(e);
        }

        public static void InvokeAccountLogin(AccountLoginEventArgs e)
        {
            if (AccountLogin != null)
                AccountLogin(e);
        }

        public static void InvokeChatRequest(ChatRequestEventArgs e)
        {
            if (ChatRequest != null)
                ChatRequest(e);
        }

        public static void InvokeVirtueItemRequest(VirtueItemRequestEventArgs e)
        {
            if (VirtueItemRequest != null)
                VirtueItemRequest(e);
        }

        public static void InvokeVirtueGumpRequest(VirtueGumpRequestEventArgs e)
        {
            if (VirtueGumpRequest != null)
                VirtueGumpRequest(e);
        }

        public static void InvokeVirtueMacroRequest(VirtueMacroRequestEventArgs e)
        {
            if (VirtueMacroRequest != null)
                VirtueMacroRequest(e);
        }

        public static void InvokePlayerDeath(PlayerDeathEventArgs e)
        {
            if (PlayerDeath != null)
                PlayerDeath(e);
        }

        public static void InvokeRenameRequest(RenameRequestEventArgs e)
        {
            if (RenameRequest != null)
                RenameRequest(e);
        }

        public static void InvokeLogout(LogoutEventArgs e)
        {
            if (Logout != null)
                Logout(e);
        }

        public static void InvokeSocketConnect(SocketConnectEventArgs e)
        {
            if (SocketConnect != null)
                SocketConnect(e);
        }

        public static void InvokeConnected(ConnectedEventArgs e)
        {
            if (Connected != null)
                Connected(e);
        }

        public static void InvokeDisconnected(DisconnectedEventArgs e)
        {
            if (Disconnected != null)
                Disconnected(e);
        }

        public static void InvokeAnimateRequest(AnimateRequestEventArgs e)
        {
            if (AnimateRequest != null)
                AnimateRequest(e);
        }

        public static void InvokeCastSpellRequest(CastSpellRequestEventArgs e)
        {
            if (CastSpellRequest != null)
                CastSpellRequest(e);
        }

        public static void InvokeOpenSpellbookRequest(OpenSpellbookRequestEventArgs e)
        {
            if (OpenSpellbookRequest != null)
                OpenSpellbookRequest(e);
        }

        public static void InvokeDisarmRequest(DisarmRequestEventArgs e)
        {
            if (DisarmRequest != null)
                DisarmRequest(e);
        }

        public static void InvokeStunRequest(StunRequestEventArgs e)
        {
            if (StunRequest != null)
                StunRequest(e);
        }

        public static void InvokeHelpRequest(HelpRequestEventArgs e)
        {
            if (HelpRequest != null)
                HelpRequest(e);
        }

        public static void InvokeShutdown(ShutdownEventArgs e)
        {
            if (Shutdown != null)
                Shutdown(e);
        }

        public static void InvokeCrashed(CrashedEventArgs e)
        {
            if (Crashed != null)
                Crashed(e);
        }

        public static void InvokeHungerChanged(HungerChangedEventArgs e)
        {
            if (HungerChanged != null)
                HungerChanged(e);
        }

        public static void InvokeMovement(MovementEventArgs e)
        {
            if (Movement != null)
                Movement(e);
        }

        public static void InvokeServerList(ServerListEventArgs e)
        {
            if (ServerList != null)
                ServerList(e);
        }

        public static void InvokeLogin(LoginEventArgs e)
        {
            if (Login != null)
                Login(e);
        }

        public static void InvokeSpeech(SpeechEventArgs e)
        {
            if (Speech != null)
                Speech(e);
        }

        public static void InvokeCharacterCreated(CharacterCreatedEventArgs e)
        {
            if (CharacterCreated != null)
                CharacterCreated(e);
        }

        public static void InvokeOpenDoorMacroUsed(OpenDoorMacroEventArgs e)
        {
            if (OpenDoorMacroUsed != null)
                OpenDoorMacroUsed(e);
        }

        public static void InvokeWorldLoad()
        {
            if (WorldLoad != null)
                WorldLoad();
        }

        public static void InvokeWorldSave(WorldSaveEventArgs e)
        {
            if (WorldSave != null)
                WorldSave(e);
        }

        public static void InvokeOnKilledBy(OnKilledByEventArgs e)
        {
            if (OnKilledBy != null)
                OnKilledBy(e);
        }

        public static void InvokeOnItemUse(OnItemUseEventArgs e)
        {
            if (OnItemUse != null)
                OnItemUse(e);
        }

        public static void InvokeOnEnterRegion(OnEnterRegionEventArgs e)
        {
            if (OnEnterRegion != null)
                OnEnterRegion(e);
        }

        public static void InvokeOnConsume(OnConsumeEventArgs e)
        {
            if (OnConsume != null)
                OnConsume(e);
        }

        public static void Reset()
        {
            CharacterCreated = null;
            OpenDoorMacroUsed = null;
            Speech = null;
            Login = null;
            ServerList = null;
            Movement = null;
            HungerChanged = null;
            Crashed = null;
            Shutdown = null;
            HelpRequest = null;
            DisarmRequest = null;
            StunRequest = null;
            OpenSpellbookRequest = null;
            CastSpellRequest = null;
            AnimateRequest = null;
            Logout = null;
            SocketConnect = null;
            Connected = null;
            Disconnected = null;
            RenameRequest = null;
            PlayerDeath = null;
            VirtueGumpRequest = null;
            VirtueItemRequest = null;
            VirtueMacroRequest = null;
            ChatRequest = null;
            AccountLogin = null;
            PaperdollRequest = null;
            ProfileRequest = null;
            ChangeProfileRequest = null;
            AggressiveAction = null;
            Command = null;
            GameLogin = null;
            DeleteRequest = null;
            WorldLoad = null;
            WorldSave = null;
            SetAbility = null;
            GuildGumpRequest = null;
            QuestGumpRequest = null;
        }
    }

    public class ClientVersionReceivedArgs : EventArgs
    {
        private readonly NetState m_State;
        private readonly ClientVersion m_Version;
        public ClientVersionReceivedArgs(NetState state, ClientVersion cv)
        {
            this.m_State = state;
            this.m_Version = cv;
        }

        public NetState State
        {
            get
            {
                return this.m_State;
            }
        }
        public ClientVersion Version
        {
            get
            {
                return this.m_Version;
            }
        }
    }

    public class CreateGuildEventArgs : EventArgs
    {
        private int m_Id;
        public CreateGuildEventArgs(int id)
        {
            this.m_Id = id;
        }

        public int Id
        {
            get
            {
                return this.m_Id;
            }
            set
            {
                this.m_Id = value;
            }
        }
    }

    public class GuildGumpRequestArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        public GuildGumpRequestArgs(Mobile mobile)
        {
            this.m_Mobile = mobile;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
    }

    public class QuestGumpRequestArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        public QuestGumpRequestArgs(Mobile mobile)
        {
            this.m_Mobile = mobile;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
    }

    public class SetAbilityEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        private readonly int m_Index;
        public SetAbilityEventArgs(Mobile mobile, int index)
        {
            this.m_Mobile = mobile;
            this.m_Index = index;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
        public int Index
        {
            get
            {
                return this.m_Index;
            }
        }
    }

    public class DeleteRequestEventArgs : EventArgs
    {
        private readonly NetState m_State;
        private readonly int m_Index;
        public DeleteRequestEventArgs(NetState state, int index)
        {
            this.m_State = state;
            this.m_Index = index;
        }

        public NetState State
        {
            get
            {
                return this.m_State;
            }
        }
        public int Index
        {
            get
            {
                return this.m_Index;
            }
        }
    }

    public class GameLoginEventArgs : EventArgs
    {
        private readonly NetState m_State;
        private readonly string m_Username;
        private readonly string m_Password;
        private bool m_Accepted;
        private CityInfo[] m_CityInfo;
        public GameLoginEventArgs(NetState state, string un, string pw)
        {
            this.m_State = state;
            this.m_Username = un;
            this.m_Password = pw;
        }

        public NetState State
        {
            get
            {
                return this.m_State;
            }
        }
        public string Username
        {
            get
            {
                return this.m_Username;
            }
        }
        public string Password
        {
            get
            {
                return this.m_Password;
            }
        }
        public bool Accepted
        {
            get
            {
                return this.m_Accepted;
            }
            set
            {
                this.m_Accepted = value;
            }
        }
        public CityInfo[] CityInfo
        {
            get
            {
                return this.m_CityInfo;
            }
            set
            {
                this.m_CityInfo = value;
            }
        }
    }

    public class AggressiveActionEventArgs : EventArgs
    {
        private static readonly Queue<AggressiveActionEventArgs> m_Pool = new Queue<AggressiveActionEventArgs>();
        private Mobile m_Aggressed;
        private Mobile m_Aggressor;
        private bool m_Criminal;
        private AggressiveActionEventArgs(Mobile aggressed, Mobile aggressor, bool criminal)
        {
            this.m_Aggressed = aggressed;
            this.m_Aggressor = aggressor;
            this.m_Criminal = criminal;
        }

        public Mobile Aggressed
        {
            get
            {
                return this.m_Aggressed;
            }
        }
        public Mobile Aggressor
        {
            get
            {
                return this.m_Aggressor;
            }
        }
        public bool Criminal
        {
            get
            {
                return this.m_Criminal;
            }
        }
        public static AggressiveActionEventArgs Create(Mobile aggressed, Mobile aggressor, bool criminal)
        {
            AggressiveActionEventArgs args;

            if (m_Pool.Count > 0)
            {
                args = m_Pool.Dequeue();

                args.m_Aggressed = aggressed;
                args.m_Aggressor = aggressor;
                args.m_Criminal = criminal;
            }
            else
            {
                args = new AggressiveActionEventArgs(aggressed, aggressor, criminal);
            }

            return args;
        }

        public void Free()
        {
            m_Pool.Enqueue(this);
        }
    }

    public class ProfileRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Beholder;
        private readonly Mobile m_Beheld;
        public ProfileRequestEventArgs(Mobile beholder, Mobile beheld)
        {
            this.m_Beholder = beholder;
            this.m_Beheld = beheld;
        }

        public Mobile Beholder
        {
            get
            {
                return this.m_Beholder;
            }
        }
        public Mobile Beheld
        {
            get
            {
                return this.m_Beheld;
            }
        }
    }

    public class ChangeProfileRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Beholder;
        private readonly Mobile m_Beheld;
        private readonly string m_Text;
        public ChangeProfileRequestEventArgs(Mobile beholder, Mobile beheld, string text)
        {
            this.m_Beholder = beholder;
            this.m_Beheld = beheld;
            this.m_Text = text;
        }

        public Mobile Beholder
        {
            get
            {
                return this.m_Beholder;
            }
        }
        public Mobile Beheld
        {
            get
            {
                return this.m_Beheld;
            }
        }
        public string Text
        {
            get
            {
                return this.m_Text;
            }
        }
    }

    public class PaperdollRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Beholder;
        private readonly Mobile m_Beheld;
        public PaperdollRequestEventArgs(Mobile beholder, Mobile beheld)
        {
            this.m_Beholder = beholder;
            this.m_Beheld = beheld;
        }

        public Mobile Beholder
        {
            get
            {
                return this.m_Beholder;
            }
        }
        public Mobile Beheld
        {
            get
            {
                return this.m_Beheld;
            }
        }
    }

    public class AccountLoginEventArgs : EventArgs
    {
        private readonly NetState m_State;
        private readonly string m_Username;
        private readonly string m_Password;
        private bool m_Accepted;
        private ALRReason m_RejectReason;
        public AccountLoginEventArgs(NetState state, string username, string password)
        {
            this.m_State = state;
            this.m_Username = username;
            this.m_Password = password;
        }

        public NetState State
        {
            get
            {
                return this.m_State;
            }
        }
        public string Username
        {
            get
            {
                return this.m_Username;
            }
        }
        public string Password
        {
            get
            {
                return this.m_Password;
            }
        }
        public bool Accepted
        {
            get
            {
                return this.m_Accepted;
            }
            set
            {
                this.m_Accepted = value;
            }
        }
        public ALRReason RejectReason
        {
            get
            {
                return this.m_RejectReason;
            }
            set
            {
                this.m_RejectReason = value;
            }
        }
    }

    public class VirtueItemRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Beholder;
        private readonly Mobile m_Beheld;
        private readonly int m_GumpID;
        public VirtueItemRequestEventArgs(Mobile beholder, Mobile beheld, int gumpID)
        {
            this.m_Beholder = beholder;
            this.m_Beheld = beheld;
            this.m_GumpID = gumpID;
        }

        public Mobile Beholder
        {
            get
            {
                return this.m_Beholder;
            }
        }
        public Mobile Beheld
        {
            get
            {
                return this.m_Beheld;
            }
        }
        public int GumpID
        {
            get
            {
                return this.m_GumpID;
            }
        }
    }

    public class VirtueGumpRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Beholder;
        private readonly Mobile m_Beheld;
        public VirtueGumpRequestEventArgs(Mobile beholder, Mobile beheld)
        {
            this.m_Beholder = beholder;
            this.m_Beheld = beheld;
        }

        public Mobile Beholder
        {
            get
            {
                return this.m_Beholder;
            }
        }
        public Mobile Beheld
        {
            get
            {
                return this.m_Beheld;
            }
        }
    }

    public class VirtueMacroRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        private readonly int m_VirtueID;
        public VirtueMacroRequestEventArgs(Mobile mobile, int virtueID)
        {
            this.m_Mobile = mobile;
            this.m_VirtueID = virtueID;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
        public int VirtueID
        {
            get
            {
                return this.m_VirtueID;
            }
        }
    }

    public class ChatRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        public ChatRequestEventArgs(Mobile mobile)
        {
            this.m_Mobile = mobile;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
    }

    public class PlayerDeathEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        public PlayerDeathEventArgs(Mobile mobile)
        {
            this.m_Mobile = mobile;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
    }

    public class RenameRequestEventArgs : EventArgs
    {
        private readonly Mobile m_From;
        private readonly Mobile m_Target;
        private readonly string m_Name;
        public RenameRequestEventArgs(Mobile from, Mobile target, string name)
        {
            this.m_From = from;
            this.m_Target = target;
            this.m_Name = name;
        }

        public Mobile From
        {
            get
            {
                return this.m_From;
            }
        }
        public Mobile Target
        {
            get
            {
                return this.m_Target;
            }
        }
        public string Name
        {
            get
            {
                return this.m_Name;
            }
        }
    }

    public class LogoutEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        public LogoutEventArgs(Mobile m)
        {
            this.m_Mobile = m;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
    }

    public class SocketConnectEventArgs : EventArgs
    {
        private readonly Socket m_Socket;
        private bool m_AllowConnection;
        public SocketConnectEventArgs(Socket s)
        {
            this.m_Socket = s;
            this.m_AllowConnection = true;
        }

        public Socket Socket
        {
            get
            {
                return this.m_Socket;
            }
        }
        public bool AllowConnection
        {
            get
            {
                return this.m_AllowConnection;
            }
            set
            {
                this.m_AllowConnection = value;
            }
        }
    }

    public class ConnectedEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        public ConnectedEventArgs(Mobile m)
        {
            this.m_Mobile = m;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
    }

    public class DisconnectedEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        public DisconnectedEventArgs(Mobile m)
        {
            this.m_Mobile = m;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
    }

    public class AnimateRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        private readonly string m_Action;
        public AnimateRequestEventArgs(Mobile m, string action)
        {
            this.m_Mobile = m;
            this.m_Action = action;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
        public string Action
        {
            get
            {
                return this.m_Action;
            }
        }
    }

    public class CastSpellRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        private readonly Item m_Spellbook;
        private readonly int m_SpellID;
        public CastSpellRequestEventArgs(Mobile m, int spellID, Item book)
        {
            this.m_Mobile = m;
            this.m_Spellbook = book;
            this.m_SpellID = spellID;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
        public Item Spellbook
        {
            get
            {
                return this.m_Spellbook;
            }
        }
        public int SpellID
        {
            get
            {
                return this.m_SpellID;
            }
        }
    }

    public class OpenSpellbookRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        private readonly int m_Type;
        public OpenSpellbookRequestEventArgs(Mobile m, int type)
        {
            this.m_Mobile = m;
            this.m_Type = type;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
        public int Type
        {
            get
            {
                return this.m_Type;
            }
        }
    }

    public class StunRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        public StunRequestEventArgs(Mobile m)
        {
            this.m_Mobile = m;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
    }

    public class DisarmRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        public DisarmRequestEventArgs(Mobile m)
        {
            this.m_Mobile = m;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
    }

    public class HelpRequestEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        public HelpRequestEventArgs(Mobile m)
        {
            this.m_Mobile = m;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
    }

    public class ShutdownEventArgs : EventArgs
    {
        public ShutdownEventArgs()
        {
        }
    }

    public class CrashedEventArgs : EventArgs
    {
        private readonly Exception m_Exception;
        private bool m_Close;
        public CrashedEventArgs(Exception e)
        {
            this.m_Exception = e;
        }

        public Exception Exception
        {
            get
            {
                return this.m_Exception;
            }
        }
        public bool Close
        {
            get
            {
                return this.m_Close;
            }
            set
            {
                this.m_Close = value;
            }
        }
    }

    public class HungerChangedEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        private readonly int m_OldValue;
        public HungerChangedEventArgs(Mobile mobile, int oldValue)
        {
            this.m_Mobile = mobile;
            this.m_OldValue = oldValue;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
        public int OldValue
        {
            get
            {
                return this.m_OldValue;
            }
        }
    }

    public class MovementEventArgs : EventArgs
    {
        private static readonly Queue<MovementEventArgs> m_Pool = new Queue<MovementEventArgs>();
        private Mobile m_Mobile;
        private Direction m_Direction;
        private bool m_Blocked;
        public MovementEventArgs(Mobile mobile, Direction dir)
        {
            this.m_Mobile = mobile;
            this.m_Direction = dir;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
        public Direction Direction
        {
            get
            {
                return this.m_Direction;
            }
        }
        public bool Blocked
        {
            get
            {
                return this.m_Blocked;
            }
            set
            {
                this.m_Blocked = value;
            }
        }
        public static MovementEventArgs Create(Mobile mobile, Direction dir)
        {
            MovementEventArgs args;

            if (m_Pool.Count > 0)
            {
                args = m_Pool.Dequeue();

                args.m_Mobile = mobile;
                args.m_Direction = dir;
                args.m_Blocked = false;
            }
            else
            {
                args = new MovementEventArgs(mobile, dir);
            }

            return args;
        }

        public void Free()
        {
            m_Pool.Enqueue(this);
        }
    }

    public class ServerListEventArgs : EventArgs
    {
        private readonly NetState m_State;
        private readonly IAccount m_Account;
        private readonly List<ServerInfo> m_Servers;
        private bool m_Rejected;
        public ServerListEventArgs(NetState state, IAccount account)
        {
            this.m_State = state;
            this.m_Account = account;
            this.m_Servers = new List<ServerInfo>();
        }

        public NetState State
        {
            get
            {
                return this.m_State;
            }
        }
        public IAccount Account
        {
            get
            {
                return this.m_Account;
            }
        }
        public bool Rejected
        {
            get
            {
                return this.m_Rejected;
            }
            set
            {
                this.m_Rejected = value;
            }
        }
        public List<ServerInfo> Servers
        {
            get
            {
                return this.m_Servers;
            }
        }
        public void AddServer(string name, IPEndPoint address)
        {
            this.AddServer(name, 0, TimeZone.CurrentTimeZone, address);
        }

        public void AddServer(string name, int fullPercent, TimeZone tz, IPEndPoint address)
        {
            this.m_Servers.Add(new ServerInfo(name, fullPercent, tz, address));
        }
    }

    public class CharacterCreatedEventArgs : EventArgs
    {
        private readonly NetState m_State;
        private readonly IAccount m_Account;
        private readonly CityInfo m_City;
        private readonly SkillNameValue[] m_Skills;
        private readonly int m_ShirtHue;
        private readonly int m_PantsHue;
        private readonly int m_HairID;
        private readonly int m_HairHue;
        private readonly int m_BeardID;
        private readonly int m_BeardHue;
        private readonly string m_Name;
        private readonly bool m_Female;
        private readonly int m_Hue;
        private readonly int m_Str;
        private readonly int m_Dex;
        private readonly int m_Int;
        private readonly Race m_Race;
        private int m_Profession;
        private Mobile m_Mobile;
        public CharacterCreatedEventArgs(NetState state, IAccount a, string name, bool female, int hue, int str, int dex, int intel, CityInfo city, SkillNameValue[] skills, int shirtHue, int pantsHue, int hairID, int hairHue, int beardID, int beardHue, int profession, Race race)
        {
            this.m_State = state;
            this.m_Account = a;
            this.m_Name = name;
            this.m_Female = female;
            this.m_Hue = hue;
            this.m_Str = str;
            this.m_Dex = dex;
            this.m_Int = intel;
            this.m_City = city;
            this.m_Skills = skills;
            this.m_ShirtHue = shirtHue;
            this.m_PantsHue = pantsHue;
            this.m_HairID = hairID;
            this.m_HairHue = hairHue;
            this.m_BeardID = beardID;
            this.m_BeardHue = beardHue;
            this.m_Profession = profession;
            this.m_Race = race;
        }

        public NetState State
        {
            get
            {
                return this.m_State;
            }
        }
        public IAccount Account
        {
            get
            {
                return this.m_Account;
            }
        }
        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
            set
            {
                this.m_Mobile = value;
            }
        }
        public string Name
        {
            get
            {
                return this.m_Name;
            }
        }
        public bool Female
        {
            get
            {
                return this.m_Female;
            }
        }
        public int Hue
        {
            get
            {
                return this.m_Hue;
            }
        }
        public int Str
        {
            get
            {
                return this.m_Str;
            }
        }
        public int Dex
        {
            get
            {
                return this.m_Dex;
            }
        }
        public int Int
        {
            get
            {
                return this.m_Int;
            }
        }
        public CityInfo City
        {
            get
            {
                return this.m_City;
            }
        }
        public SkillNameValue[] Skills
        {
            get
            {
                return this.m_Skills;
            }
        }
        public int ShirtHue
        {
            get
            {
                return this.m_ShirtHue;
            }
        }
        public int PantsHue
        {
            get
            {
                return this.m_PantsHue;
            }
        }
        public int HairID
        {
            get
            {
                return this.m_HairID;
            }
        }
        public int HairHue
        {
            get
            {
                return this.m_HairHue;
            }
        }
        public int BeardID
        {
            get
            {
                return this.m_BeardID;
            }
        }
        public int BeardHue
        {
            get
            {
                return this.m_BeardHue;
            }
        }
        public int Profession
        {
            get
            {
                return this.m_Profession;
            }
            set
            {
                this.m_Profession = value;
            }
        }
        public Race Race
        {
            get
            {
                return this.m_Race;
            }
        }
    }

    public class OpenDoorMacroEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        public OpenDoorMacroEventArgs(Mobile mobile)
        {
            this.m_Mobile = mobile;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
    }

    public class SpeechEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        private readonly MessageType m_Type;
        private readonly int m_Hue;
        private readonly int[] m_Keywords;
        private string m_Speech;
        private bool m_Handled;
        private bool m_Blocked;
        public SpeechEventArgs(Mobile mobile, string speech, MessageType type, int hue, int[] keywords)
        {
            this.m_Mobile = mobile;
            this.m_Speech = speech;
            this.m_Type = type;
            this.m_Hue = hue;
            this.m_Keywords = keywords;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
        public string Speech
        {
            get
            {
                return this.m_Speech;
            }
            set
            {
                this.m_Speech = value;
            }
        }
        public MessageType Type
        {
            get
            {
                return this.m_Type;
            }
        }
        public int Hue
        {
            get
            {
                return this.m_Hue;
            }
        }
        public int[] Keywords
        {
            get
            {
                return this.m_Keywords;
            }
        }
        public bool Handled
        {
            get
            {
                return this.m_Handled;
            }
            set
            {
                this.m_Handled = value;
            }
        }
        public bool Blocked
        {
            get
            {
                return this.m_Blocked;
            }
            set
            {
                this.m_Blocked = value;
            }
        }
        public bool HasKeyword(int keyword)
        {
            for (int i = 0; i < this.m_Keywords.Length; ++i)
                if (this.m_Keywords[i] == keyword)
                    return true;

            return false;
        }
    }

    public class LoginEventArgs : EventArgs
    {
        private readonly Mobile m_Mobile;
        public LoginEventArgs(Mobile mobile)
        {
            this.m_Mobile = mobile;
        }

        public Mobile Mobile
        {
            get
            {
                return this.m_Mobile;
            }
        }
    }

    public class WorldSaveEventArgs : EventArgs
    {
        private readonly bool m_Msg;
        public WorldSaveEventArgs(bool msg)
        {
            this.m_Msg = msg;
        }

        public bool Message
        {
            get
            {
                return this.m_Msg;
            }
        }
    }

    public class FastWalkEventArgs
    {
        private readonly NetState m_State;
        private bool m_Blocked;
        public FastWalkEventArgs(NetState state)
        {
            this.m_State = state;
            this.m_Blocked = false;
        }

        public NetState NetState
        {
            get
            {
                return this.m_State;
            }
        }
        public bool Blocked
        {
            get
            {
                return this.m_Blocked;
            }
            set
            {
                this.m_Blocked = value;
            }
        }
    }

    public class OnKilledByEventArgs : EventArgs
    {
        private Mobile m_Killed;
        private Mobile m_KilledBy;

        public OnKilledByEventArgs(Mobile killed, Mobile killedBy)
        {
            this.m_Killed = killed;
            this.m_KilledBy = killedBy;
        }

        public Mobile Killed
        {
            get
            {
                return this.m_Killed;
            }
        }
        public Mobile KilledBy
        {
            get
            {
                return this.m_KilledBy;
            }
        }
    }

    public class OnItemUseEventArgs : EventArgs
    {
        private Mobile m_From;
        private Item m_Item;

        public OnItemUseEventArgs(Mobile from, Item item)
        {
            this.m_From = from;
            this.m_Item = item;
        }

        public Mobile From
        {
            get
            {
                return this.m_From;
            }
        }
        public Item Item
        {
            get
            {
                return this.m_Item;
            }
        }
    }

    public class OnEnterRegionEventArgs : EventArgs
    {
        private Mobile m_From;
        private Region m_Region;

        public OnEnterRegionEventArgs(Mobile from, Region region)
        {
            this.m_From = from;
            this.m_Region = region;
        }

        public Mobile From
        {
            get
            {
                return this.m_From;
            }
        }
        public Region Region
        {
            get
            {
                return this.m_Region;
            }
        }
    }

    public class OnConsumeEventArgs : EventArgs
    {
        private Mobile m_Consumer;
        private Item m_Consumed;
        private int m_Quantity;

        public OnConsumeEventArgs(Mobile consumer, Item consumed) : this(consumer, consumed, 1)
        {
        }

        public OnConsumeEventArgs(Mobile consumer, Item consumed, int quantity)
        {
            m_Consumer = consumer;
            m_Consumed = consumed;
            m_Quantity = quantity;
        }

        public Mobile Consumer
        {
            get
            {
                return this.m_Consumer;
            }
        }

        public Item Consumed
        {
            get
            {
                return m_Consumed;
            }
        }

        public int Quantity
        {
            get
            {
                return m_Quantity;
            }
        }
    }
}