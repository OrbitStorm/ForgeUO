using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Targets
{
    public class AIControlMobileTarget : Target
    {
        private readonly List<BaseAI> m_List;
        private readonly OrderType m_Order;
        public AIControlMobileTarget(BaseAI ai, OrderType order)
            : base(-1, false, (order == OrderType.Attack ? TargetFlags.Harmful : TargetFlags.None))
        {
            this.m_List = new List<BaseAI>();
            this.m_Order = order;

            this.AddAI(ai);
        }

        public OrderType Order
        {
            get
            {
                return this.m_Order;
            }
        }
        public void AddAI(BaseAI ai)
        {
            if (!this.m_List.Contains(ai))
                this.m_List.Add(ai);
        }

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is Mobile)
            {
                Mobile m = (Mobile)o;
                for (int i = 0; i < this.m_List.Count; ++i)
                    this.m_List[i].EndPickTarget(from, m, this.m_Order);
            }
        }
    }
}