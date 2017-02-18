#region Header
// **********
// [ForgeUO] - Corgul Altar
// Part of High Seas content. Players must sacrifice a treasure map
// and a world map to earn a "Corgul Map" (not included).
// The Corgul Map is supposed to teleport the user and any nearby
// party members to Cursed Island. This altar has a timer.
// More info on Corgul the Soulbinder:
// http://www.uoguide.com/Corgul_the_Soulbinder
// **********
#endregion

#region References
using System;
using Server;
using Server.Items;
using System.Collections;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.ContextMenus;
using Server.Prompts;
using Server.Targeting;
using Server.Network;
#endregion

public class CorgulAltar : Item
{
    private static readonly TimeSpan UseDelay = TimeSpan.FromHours(24.0);
    private DateTime m_LastUse;
    
    [CommandProperty( AccessLevel.GameMaster )]
    public DateTime LastUse
    {
        get
        {
            return this.m_LastUse;
        }
        set
        {
            this.m_LastUse = value;
        }
    }

    private Dictionary<Mobile, CorgulContext> m_MapTable;
 
    public Dictionary<Mobile, CorgulContext> MapTable
    {
        get{ return m_MapTable; }
        private set
        {
            m_MapTable = value;
        }
    }
 
    [Constructable]
    public CorgulAltar() : base(0x35EF)
    {
        Name = "Sacrificial Altar";
        Hue = 2075;
        Movable = false;
 
        MapTable = new Dictionary<Mobile, CorgulContext>();
    }
 
    public CorgulAltar(Serial s) : base(s) {}
 
    public override void OnDoubleClick(Mobile from)
    {
        if(DateTime.Now > (this.m_LastUse + UseDelay))
        {
            if ( !MapTable.ContainsKey(from) )
            {
                MapTable.Add(from, new CorgulContext(from));
            }
            else
            {
                MapTable[from].SacrificedWMap = false;
                MapTable[from].SacrificedTMap = false;
            }
        }
        
        if(!MapTable.ContainsKey(from) || !MapTable[from].SacrificedTMap)
        {
            MapTable[from] = new CorgulContext(from);
         
            from.SendMessage("Select a treasure map..");
            from.BeginTarget(2, false, TargetFlags.None, new TargetCallback( OnTargetTMap ));
        }
        else if(!MapTable[from].RewardReceived())
        {
            from.SendMessage("You've already sacrificed a treasure map. You must now sacrifice a world map..");
            from.BeginTarget(2, false, TargetFlags.None, new TargetCallback( OnTargetWMap ));
        }
        else
        {
            from.SendMessage("You have already received your reward from this altar.");
            from.SendMessage("Please wait at least one hour before attempting another sacrifice.");
        }
    }
 
    private void OnTargetTMap(Mobile from, object targeted)
    {
        if(targeted is TreasureMap)
        {
            MapTable[from].SacrificedTMap = true;
         
            ((TreasureMap)targeted).Delete();
         
            from.SendMessage("Your offering has been accepted. The price of blood will be taken when you sacrifice your world map.");
            from.BeginTarget(2, false, TargetFlags.None, new TargetCallback( OnTargetWMap ));
        }
        else
        {
            from.SendMessage("That is not a treasure map!");
            from.BeginTarget(2, false, TargetFlags.None, new TargetCallback( OnTargetTMap ));
        }
    }
 
    private void OnTargetWMap(Mobile from, object targeted)
    {
        if(targeted is WorldMap)
        {
            MapTable[from].SacrificedWMap = true;
         
            ((WorldMap)targeted).Delete();
         
            from.SendMessage("Your final offering has been accepted and the blood cost has been collected.");
            from.Hits = 1;
         
            from.AddToBackpack(new CorgulMap());
            
            this.m_LastUse = DateTime.Now;
        }
        else
        {
            from.SendMessage("That is not a world map!");
            from.BeginTarget(2, false, TargetFlags.None, new TargetCallback( OnTargetWMap ));
        }
    }

    public override void Serialize(GenericWriter writer)
    {
        base.Serialize(writer);
     
        writer.Write((int)0);
     
        writer.Write((int)MapTable.Count);
     
        foreach(KeyValuePair<Mobile, CorgulContext> kvp in MapTable)
        {
            writer.WriteMobile<Mobile>(kvp.Key);
            kvp.Value.Serialize(writer);
        }
    }
 
    public override void Deserialize(GenericReader reader)
    {
        base.Deserialize(reader);
     
        int version = reader.ReadInt();
     
        switch(version)
        {
            case 0:
                int tableCount = reader.ReadInt();
             
                MapTable = new Dictionary<Mobile, CorgulContext>(tableCount);
             
                for(int i = 0; i < tableCount; i++)
                {
                    MapTable[reader.ReadMobile<Mobile>()] = new CorgulContext(reader);
                }
             
                break;
        }
    }
 
    //private class CorgulContext
    public sealed class CorgulContext
    {
        private Mobile m_Mobile;
        private bool m_TMap;
        private bool m_WMap;
     
        public Mobile Mobile
        {
            get{ return m_Mobile; }
            set{ m_Mobile = value; }
        }
     
        public bool SacrificedTMap
        {
            get{ return m_TMap; }
            set{ m_TMap = value; }
        }
     
        public bool SacrificedWMap
        {
            get{ return m_WMap; }
            set{ m_WMap = value; }
        }
     
        public CorgulContext(Mobile from)
        {
            m_Mobile = from;
        }
     
        public bool RewardReceived()
        {
            return (SacrificedTMap && SacrificedWMap);
        }
     
        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0);
         
            writer.WriteMobile<Mobile>(Mobile);
            writer.Write(SacrificedTMap);
            writer.Write(SacrificedWMap);
        }
     
        public CorgulContext(GenericReader reader)
        {
            int version = reader.ReadInt();
         
            switch(version)
            {
                case 0:
                    Mobile = reader.ReadMobile<Mobile>();
                    SacrificedTMap = reader.ReadBool();
                    SacrificedWMap = reader.ReadBool();
                 
                    break;
            }
        }
    }
}
